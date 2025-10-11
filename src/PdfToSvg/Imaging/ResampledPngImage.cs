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
using System.Runtime.CompilerServices;
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

        public ResampledPngImage(PdfDictionary imageDictionary, ColorSpace colorSpace)
            : base(imageDictionary, "image/png", ".png")
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

            this.bitsPerComponent = ImageHelper.GetBitsPerComponent(imageDictionary);

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
            var componentBuffer = new float[colorSpace.ComponentsPerSample * width];

            var decodeArray = ImageHelper.GetDecodeArray(imageDictionary, colorSpace);
            var pngRgbRow = new byte[1 + width * ColorsRgb];

            for (var y = 0; y < height; y++)
            {
                componentReader.Read(componentBuffer, 0, componentBuffer.Length);
                componentReader.SkipPartialByte();

                decodeArray.Decode(componentBuffer, 0, componentBuffer.Length);

                colorSpace.ToRgb8(componentBuffer, 0, pngRgbRow, 1, width);

                outputStream.Write(pngRgbRow);

                if ((y & 0x3f) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        private void WriteColorKeyMaskedImage(BitReader componentReader, Stream outputStream, CancellationToken cancellationToken)
        {
            var componentBuffer = new float[colorSpace.ComponentsPerSample * width];

            var decodeArray = ImageHelper.GetDecodeArray(imageDictionary, colorSpace);

            var pngRgbRow = new byte[1 + width * ColorsRgba];

            var transparent = new bool[width];

            for (var y = 0; y < height; y++)
            {
                componentReader.Read(componentBuffer, 0, componentBuffer.Length);
                componentReader.SkipPartialByte();

                for (var x = 0; x < width; x++)
                {
                    transparent[x] = true;

                    for (var componentIndex = 0; componentIndex < colorSpace.ComponentsPerSample; componentIndex++)
                    {
                        var component = (int)componentBuffer[x * colorSpace.ComponentsPerSample + componentIndex];

                        if (component < colorKey[componentIndex * 2] ||
                            component > colorKey[componentIndex * 2 + 1])
                        {
                            transparent[x] = false;
                            break;
                        }
                    }
                }

                decodeArray.Decode(componentBuffer, 0, componentBuffer.Length);

                colorSpace.ToRgba8(componentBuffer, 0, pngRgbRow, 1, width);

                for (var x = 0; x < width; x++)
                {
                    var pixelOffset = 1 + x * ColorsRgba;

                    if (transparent[x])
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

                outputStream.Write(pngRgbRow);

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

        public override int GetHashCode() =>
            838425911 ^
            RuntimeHelpers.GetHashCode(imageDictionaryStream) ^
            colorSpace.GetHashCode();

        public override bool Equals(object? obj) =>
            obj is ResampledPngImage pngImage &&
            ReferenceEquals(pngImage.imageDictionaryStream, imageDictionaryStream) &&
            pngImage.colorSpace.Equals(colorSpace);
    }
}
