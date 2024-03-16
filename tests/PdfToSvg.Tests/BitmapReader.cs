// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests
{
    /// <summary>
    /// Minimal reader for reading monochrome 32-bit bitmaps. 32-bit to avoid handling packed bits and row padding.
    /// Reference: https://en.wikipedia.org/wiki/BMP_file_format
    /// </summary>
    internal class BitmapReader
    {
        private readonly byte[] data;
        private int offset;

        public BitmapReader(byte[] data)
        {
            this.data = data;

            offset = ReadInt32(data, 10);

            Width = ReadInt32(data, 18);
            Height = ReadInt32(data, 22);

            var bitsPerPixel = ReadInt16(data, 28);
            if (bitsPerPixel != 32)
            {
                throw new NotSupportedException("Only supports 32 bit bmps");
            }
        }

        public int Width { get; }
        public int Height { get; }

        private static int ReadInt16(byte[] data, int offset)
        {
            return
                (data[offset + 0] << 0) |
                (data[offset + 1] << 8);
        }

        private static int ReadInt32(byte[] data, int offset)
        {
            return
                (data[offset + 0] << 0) |
                (data[offset + 1] << 8) |
                (data[offset + 2] << 16) |
                (data[offset + 3] << 24);
        }

        public byte[] ReadRgba32()
        {
            const int BytesPerPixel = 4;

            const int BmpAlpha = 3;
            const int BmpRed = 2;
            const int BmpGreen = 1;
            const int BmpBlue = 0;

            const int OutputAlpha = 3;
            const int OutputRed = 0;
            const int OutputGreen = 1;
            const int OutputBlue = 2;

            var result = new byte[Height * Width * BytesPerPixel];

            var outputCursor = 0;

            for (var y = 0; y < Height; y++)
            {
                var inputCursor = offset + (Height - y - 1) * Width * BytesPerPixel;

                for (var x = 0; x < Width; x++)
                {
                    result[outputCursor + OutputRed] = data[inputCursor + BmpRed];
                    result[outputCursor + OutputGreen] = data[inputCursor + BmpGreen];
                    result[outputCursor + OutputBlue] = data[inputCursor + BmpBlue];
                    result[outputCursor + OutputAlpha] = data[inputCursor + BmpAlpha];

                    outputCursor += BytesPerPixel;
                    inputCursor += BytesPerPixel;
                }
            }

            return result;
        }
    }
}
