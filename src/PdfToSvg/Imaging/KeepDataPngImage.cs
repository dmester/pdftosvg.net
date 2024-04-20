// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Filters;
using PdfToSvg.Imaging.Png;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Imaging
{
    internal class KeepDataPngImage : Image
    {
        private readonly PdfDictionary imageDictionary;
        private readonly PdfStream imageDictionaryStream;
        private readonly ColorSpace colorSpace;

        public KeepDataPngImage(PdfDictionary imageDictionary, ColorSpace colorSpace)
            : base(imageDictionary, "image/png", ".png")
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

            // See supported color types and bit depths in PNG:
            // https://www.w3.org/TR/PNG/#table111

            if (colorSpace is IndexedColorSpace)
            {
                return
                    bitsPerComponent == 1 ||
                    bitsPerComponent == 2 ||
                    bitsPerComponent == 4 ||
                    bitsPerComponent == 8;
            }

            // A decode array requires unpacking and scaling each pixel except for indexed images,
            // where the decoding can be done entirely on the color table.
            if (ImageHelper.HasCustomDecodeArray(imageDictionary, colorSpace))
            {
                return false;
            }

            // Images with a color key mask must be rebuilt.
            if (imageDictionary.TryGetValue(Names.Mask, out var mask) && mask is object[])
            {
                return false;
            }

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

            return false;
        }

        public override byte[] GetContent(CancellationToken cancellationToken)
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
                bytesPerRow = 1 + MathUtils.BitsToBytes(width * bitsPerComponent);
            }
            else if (colorSpace is IndexedColorSpace indexedColorSpace)
            {
                const int PaletteBytesPerColor = 3;

                colorType = PngColorType.IndexedColour;
                bytesPerRow = 1 + MathUtils.BitsToBytes(width * bitsPerComponent);

                var paletteColorCount = 1 << bitsPerComponent;
                palette = new byte[paletteColorCount * PaletteBytesPerColor];

                var decodeArray = ImageHelper.GetDecodeArray(imageDictionary, colorSpace);

                var indexedColorValue = new float[1];
                for (var i = 0; i < indexedColorSpace.ColorCount && i < paletteColorCount; i++)
                {
                    indexedColorValue[0] = i;
                    decodeArray.Decode(indexedColorValue, 0, 1);
                    indexedColorSpace.ToRgb8(indexedColorValue, 0, palette, i * PaletteBytesPerColor, 1);
                }
            }
            else
            {
                colorType = PngColorType.Truecolour;
                bytesPerRow = 1 + 3 * width * bitsPerComponent / 8;
            }

            cancellationToken.ThrowIfCancellationRequested();

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
                using var decodedStream = imageDictionaryStream.OpenDecoded(cancellationToken);

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

                    pngDataStream.Write(row);

                    if ((y & 0x3f) == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
            }

            pngWriter.WriteImageEnd();

            return pngStream.ToArray();
        }

#if HAVE_ASYNC
        public override Task<byte[]> GetContentAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(GetContent(cancellationToken));
        }
#endif

        public override int GetHashCode() =>
            960926671 ^
            RuntimeHelpers.GetHashCode(imageDictionaryStream) ^
            colorSpace.GetHashCode();

        public override bool Equals(object obj) =>
            obj is KeepDataPngImage pngImage &&
            ReferenceEquals(pngImage.imageDictionaryStream, imageDictionaryStream) &&
            pngImage.colorSpace.Equals(colorSpace);
    }
}
