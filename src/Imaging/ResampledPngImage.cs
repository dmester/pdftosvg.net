using PdfToSvg.ColorSpaces;
using PdfToSvg.DocumentModel;
using PdfToSvg.Imaging.Png;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        public override byte[] GetContent()
        {
            using var imageDataStream = imageDictionaryStream.OpenDecoded();

            var bitsPerComponent = imageDictionary.GetValueOrDefault(Names.BitsPerComponent, 8);
            var width = imageDictionary.GetValueOrDefault(Names.Width, 0);
            var height = imageDictionary.GetValueOrDefault(Names.Height, 0);

            DecodeArray decodeArray;

            if (imageDictionary.TryGetArray<double>(Names.Decode, out var decodeValues))
            {
                decodeArray = new DecodeArray(bitsPerComponent, decodeValues);
            }
            else
            {
                decodeArray = colorSpace.GetDefaultDecodeArray(bitsPerComponent);
            }

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

                var componentReader = ComponentReader.Create(imageDataStream, bitsPerComponent, componentBuffer.Length);

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
                }
            }

            pngWriter.WriteImageEnd();

            return pngStream.ToArray();
        }

        public override Task<byte[]> GetContentAsync()
        {
            return Task.FromResult(GetContent());
        }
    }
}
