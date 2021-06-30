// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
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
        private readonly PdfDictionary imageDictionary;
        private readonly PdfStream imageDictionaryStream;
        private readonly ColorSpace colorSpace;

        public ResampledPngImage(PdfDictionary imageDictionary, ColorSpace colorSpace) : base("image/png")
        {
            if (imageDictionary.Stream == null)
            {
                throw new ArgumentException("There was no data stream attached to the image dictionary.", nameof(imageDictionary));
            }

            this.imageDictionary = imageDictionary;
            this.imageDictionaryStream = imageDictionary.Stream;
            this.colorSpace = colorSpace;
        }

        public override byte[] GetContent(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var imageDataStream = imageDictionaryStream.OpenDecoded(cancellationToken);

            var bitsPerComponent = imageDictionary.GetValueOrDefault(Names.BitsPerComponent, 8);
            var width = imageDictionary.GetValueOrDefault(Names.Width, 0);
            var height = imageDictionary.GetValueOrDefault(Names.Height, 0);

            var decodeArray = ImageHelper.GetDecodeArray(imageDictionary, colorSpace);

            var pngStream = new MemoryStream();
            var pngWriter = new PngEncoder(pngStream);

            pngWriter.WriteSignature();

            pngWriter.WriteImageHeader(
                width,
                height,
                PngColorType.Truecolour,
                8);

            pngWriter.WriteImageGamma();

            using (var pngDataStream = pngWriter.GetImageDataStream())
            {
                var componentBuffer = new float[colorSpace.ComponentsPerSample * 800];
                var componentCursor = componentBuffer.Length;

                using var componentReader = new BitReader(imageDataStream, bitsPerComponent,
                    bufferSize: componentBuffer.Length * bitsPerComponent / 8);

                var pngRgbRow = new byte[1 + width * 3];

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
                            (pngRgbRow.Length - pngRgbRowCursor) / 3,
                            (componentBuffer.Length - componentCursor) / colorSpace.ComponentsPerSample
                            );

                        colorSpace.ToRgb8(componentBuffer, componentCursor, pngRgbRow, pngRgbRowCursor, samplesThisIteration);

                        componentCursor += samplesThisIteration * colorSpace.ComponentsPerSample;
                        pngRgbRowCursor += samplesThisIteration * 3;
                    }
                    while (pngRgbRowCursor < pngRgbRow.Length);

                    pngDataStream.Write(pngRgbRow, 0, pngRgbRow.Length);

                    if ((y & 0x3f) == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
            }

            pngWriter.WriteImageEnd();

            return pngStream.ToArray();
        }

        public override Task<byte[]> GetContentAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(GetContent(cancellationToken));
        }
    }
}
