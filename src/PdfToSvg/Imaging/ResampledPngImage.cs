// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Imaging.Png;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Imaging
{
    internal class ResampledPngImage : Image
    {
        private const int SampleBufferSize = 800;

        private const int ColorsRgb = 3;
        private const int ColorsRgba = 4;

        private const int OffsetRed = 0;
        private const int OffsetGreen = 1;
        private const int OffsetBlue = 2;
        private const int OffsetAlpha = 3;

        private readonly PdfDictionary imageDictionary;
        private readonly PdfStream imageDictionaryStream;
        private readonly ColorSpace colorSpace;
        private readonly int bitsPerComponent;
        private readonly int width;
        private readonly int height;
        private readonly int[] colorKey;

        public ResampledPngImage(PdfDictionary imageDictionary, ColorSpace colorSpace) : base("image/png")
        {
            if (imageDictionary.Stream == null)
            {
                throw new ArgumentException("There was no data stream attached to the image dictionary.", nameof(imageDictionary));
            }

            this.imageDictionary = imageDictionary;
            this.imageDictionaryStream = imageDictionary.Stream;
            this.colorSpace = colorSpace;

            this.width = imageDictionary.GetValueOrDefault(Names.Width, 0);
            this.height = imageDictionary.GetValueOrDefault(Names.Height, 0);

            if (imageDictionary.GetValueOrDefault(Names.ImageMask, false) == true)
            {
                bitsPerComponent = 1;
            }
            else
            {
                bitsPerComponent = imageDictionary.GetValueOrDefault(Names.BitsPerComponent, 8);
            }

            var potentialColorKey = imageDictionary.GetArrayOrNull<int>(Names.Mask);
            if (potentialColorKey != null && potentialColorKey.Length >= colorSpace.ComponentsPerSample * 2)
            {
                colorKey = potentialColorKey;
            }
            else
            {
                colorKey = ArrayUtils.Empty<int>();
            }
        }

        public override byte[] GetContent(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var imageDataStream = imageDictionaryStream.OpenDecoded(cancellationToken);

            var pngStream = new MemoryStream();
            var pngWriter = new PngEncoder(pngStream);

            pngWriter.WriteSignature();

            pngWriter.WriteImageHeader(
                width,
                height,
                colorKey.Length == 0 ? PngColorType.Truecolour : PngColorType.TruecolourWithAlpha,
                8);

            pngWriter.WriteImageGamma();

            using (var pngDataStream = pngWriter.GetImageDataStream())
            {
                var componentBuffer = new float[colorSpace.ComponentsPerSample * SampleBufferSize];
                var componentCursor = componentBuffer.Length;

                using var componentReader = new BitReader(imageDataStream, bitsPerComponent,
                    bufferSize: componentBuffer.Length * bitsPerComponent / 8);

                if (colorKey.Length > 0)
                {
                    WriteColorKeyMaskedImage(componentReader, pngDataStream, cancellationToken);
                }
                else
                {
                    WriteUnmaskedImage(componentReader, pngDataStream, cancellationToken);
                }
            }

            pngWriter.WriteImageEnd();

            return pngStream.ToArray();
        }

        private void WriteUnmaskedImage(BitReader componentReader, Stream outputStream, CancellationToken cancellationToken)
        {
            var componentBuffer = new float[colorSpace.ComponentsPerSample * SampleBufferSize];
            var componentCursor = componentBuffer.Length;

            var decodeArray = ImageHelper.GetDecodeArray(imageDictionary, colorSpace);

            var pngRgbRow = new byte[1 + width * ColorsRgb];

            for (var y = 0; y < height; y++)
            {
                var pngRgbRowCursor = 1;

                do
                {
                    if (componentCursor >= componentBuffer.Length)
                    {
                        componentReader.Read(componentBuffer, 0, componentBuffer.Length);
                        decodeArray.Decode(componentBuffer, 0, componentBuffer.Length);
                        componentCursor = 0;
                    }

                    var samplesThisIteration = Math.Min(
                        (pngRgbRow.Length - pngRgbRowCursor) / ColorsRgb,
                        (componentBuffer.Length - componentCursor) / colorSpace.ComponentsPerSample
                        );

                    colorSpace.ToRgb8(componentBuffer, componentCursor, pngRgbRow, pngRgbRowCursor, samplesThisIteration);

                    componentCursor += samplesThisIteration * colorSpace.ComponentsPerSample;
                    pngRgbRowCursor += samplesThisIteration * ColorsRgb;
                }
                while (pngRgbRowCursor < pngRgbRow.Length);

                outputStream.Write(pngRgbRow, 0, pngRgbRow.Length);

                if ((y & 0x3f) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        private void WriteColorKeyMaskedImage(BitReader componentReader, Stream outputStream, CancellationToken cancellationToken)
        {
            var componentBuffer = new float[colorSpace.ComponentsPerSample * SampleBufferSize];
            var componentCursor = componentBuffer.Length;

            var decodeArray = ImageHelper.GetDecodeArray(imageDictionary, colorSpace);

            var pngRgbRow = new byte[1 + width * ColorsRgba];

            var transparent = new bool[SampleBufferSize];
            var transparentCursor = 0;

            for (var y = 0; y < height; y++)
            {
                var pngRgbRowCursor = 1;

                do
                {
                    if (componentCursor >= componentBuffer.Length)
                    {
                        componentReader.Read(componentBuffer, 0, componentBuffer.Length);

                        for (var sample = 0; sample < SampleBufferSize; sample++)
                        {
                            transparent[sample] = true;

                            for (var componentIndex = 0; componentIndex < colorSpace.ComponentsPerSample; componentIndex++)
                            {
                                var component = (int)componentBuffer[sample * colorSpace.ComponentsPerSample + componentIndex];

                                if (component < colorKey[componentIndex * 2] ||
                                    component > colorKey[componentIndex * 2 + 1])
                                {
                                    transparent[sample] = false;
                                    break;
                                }
                            }
                        }

                        decodeArray.Decode(componentBuffer, 0, componentBuffer.Length);
                        componentCursor = 0;
                        transparentCursor = 0;
                    }

                    var samplesThisIteration = Math.Min(
                        (pngRgbRow.Length - pngRgbRowCursor) / 4,
                        (componentBuffer.Length - componentCursor) / colorSpace.ComponentsPerSample
                        );

                    colorSpace.ToRgba8(componentBuffer, componentCursor, pngRgbRow, pngRgbRowCursor, samplesThisIteration);

                    for (var i = 0; i < samplesThisIteration; i++)
                    {
                        var pixelOffset = pngRgbRowCursor + i * ColorsRgba;

                        if (transparent[transparentCursor + i])
                        {
                            // Zero all components to improve compression
                            pngRgbRow[pixelOffset + OffsetRed] = 0;
                            pngRgbRow[pixelOffset + OffsetGreen] = 0;
                            pngRgbRow[pixelOffset + OffsetBlue] = 0;
                            pngRgbRow[pixelOffset + OffsetAlpha] = 0;
                        }
                        else
                        {
                            pngRgbRow[pixelOffset + OffsetAlpha] = 255;
                        }
                    }

                    transparentCursor += samplesThisIteration;
                    componentCursor += samplesThisIteration * colorSpace.ComponentsPerSample;
                    pngRgbRowCursor += samplesThisIteration * ColorsRgba;
                }
                while (pngRgbRowCursor < pngRgbRow.Length);

                outputStream.Write(pngRgbRow, 0, pngRgbRow.Length);

                if ((y & 0x3f) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

#if HAVE_ASYNC
        public override Task<byte[]> GetContentAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(GetContent(cancellationToken));
        }
#endif
    }
}
