// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.DocumentModel;
using PdfToSvg.Filters;
using PdfToSvg.Imaging.Png;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Imaging
{
    internal class KeepDataPngImage : Image
    {
        private readonly PdfDictionary imageDictionary;
        private readonly PdfStream imageDictionaryStream;
        private readonly ColorSpace colorSpace;

        public KeepDataPngImage(PdfDictionary imageDictionary, ColorSpace colorSpace) : base("image/png")
        {
            if (imageDictionary.Stream == null)
            {
                throw new ArgumentException("There was no data stream attached to the image dictionary.", nameof(imageDictionary));
            }

            this.imageDictionary = imageDictionary;
            this.imageDictionaryStream = imageDictionary.Stream;
            this.colorSpace = colorSpace;
        }

        public static bool IsSupported(PdfDictionary imageDictionary, ColorSpace colorSpace)
        {
            var lastFilter = imageDictionary.Stream?.Filters.LastOrDefault();
            if (lastFilter == null || lastFilter.Filter != Filter.FlateDecode)
            {
                return false;
            }

            var decodeParms = lastFilter.DecodeParms;
            var bitsPerComponent = decodeParms == null ? 8 : decodeParms.GetValueOrDefault(Names.BitsPerComponent, 8);

            // A decode array requires unpacking and scaling each pixel
            if (imageDictionary.ContainsKey(Names.Decode))
            {
                return false;
            }

            // See supported color types and bit depths in PNG:
            // https://www.w3.org/TR/PNG/#table111

            if (colorSpace is DeviceRgbColorSpace)
            {
                return
                    bitsPerComponent == 8 ||
                    bitsPerComponent == 16;
            }
            else if (colorSpace is DeviceGrayColorSpace)
            {
                return
                    bitsPerComponent == 1 ||
                    bitsPerComponent == 2 ||
                    bitsPerComponent == 4 ||
                    bitsPerComponent == 8 ||
                    bitsPerComponent == 16;
            }
            else if (colorSpace is IndexedColorSpace)
            {
                return
                    bitsPerComponent == 1 ||
                    bitsPerComponent == 2 ||
                    bitsPerComponent == 4 ||
                    bitsPerComponent == 8;
            }

            return false;
        }

        public override byte[] GetContent()
        {
            var bitsPerComponent = imageDictionary.GetValueOrDefault(Names.BitsPerComponent, 8);
            var width = imageDictionary.GetValueOrDefault(Names.Width, 0);
            var height = imageDictionary.GetValueOrDefault(Names.Height, 0);
            
            int bytesPerRow;
            byte[]? palette = null;
            PngColorType colorType;
            
            if (colorSpace is DeviceGrayColorSpace)
            {
                colorType = PngColorType.Greyscale;
                bytesPerRow = 1 + (width * bitsPerComponent + 7) / 8;
            }
            else if (colorSpace is IndexedColorSpace indexedColorSpace)
            {
                const int PaletteBytesPerColor = 3;

                colorType = PngColorType.IndexedColour;
                bytesPerRow = 1 + (width * bitsPerComponent + 7) / 8;

                var paletteColorCount = 1 << bitsPerComponent;
                palette = new byte[paletteColorCount * PaletteBytesPerColor];

                var indexedColorValue = new float[1];
                for (var i = 0; i < indexedColorSpace.ColorCount && i < paletteColorCount; i++)
                {
                    indexedColorValue[0] = i;
                    indexedColorSpace.ToRgb8(indexedColorValue, 0, palette, i * PaletteBytesPerColor, 1);
                }
            }
            else
            {
                colorType = PngColorType.Truecolour;
                bytesPerRow = 1 + 3 * width * bitsPerComponent / 8;
            }

            var pngStream = new MemoryStream();
            var pngWriter = new PngEncoder(pngStream);

            pngWriter.WriteSignature();
            pngWriter.WriteImageHeader(width, height, colorType, bitsPerComponent);
            pngWriter.WriteImageGamma();

            if (palette != null)
            {
                pngWriter.WritePalette(palette);
            }

            using (var pngDataStream = pngWriter.GetImageDataStream())
            {
                using var decodedStream = imageDictionaryStream.OpenDecoded();

                var row = new byte[bytesPerRow];

                for (var y = 0; y < height; y++)
                {
                    var rowCursor = 1;
                    int read;

                    do
                    {
                        read = decodedStream.Read(row, rowCursor, row.Length - rowCursor);
                        rowCursor += read;
                    }
                    while (read > 0 && rowCursor < row.Length);

                    pngDataStream.Write(row, 0, row.Length);
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
