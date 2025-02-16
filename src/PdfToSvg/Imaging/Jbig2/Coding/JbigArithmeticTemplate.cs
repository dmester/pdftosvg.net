// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jbig2.Coding
{
    internal struct JbigArithmeticTemplate
    {
        // Visiting all template pixels on every computation of the context used for arithmetic decoding becomes quickly a bottleneck.
        // This struct will optimize the context computation to only visit new template pixels on each computation.
        // 
        // Template 0 for example has 16 template pixels:
        //
        //         x x x x x
        //       x x x x x x x
        //     x x x x o
        //
        // This struct will on computation shift consecutive pixels to the left and then only visit new pixels, marked with x below:
        //
        //         . . . . x
        //       . . . . . . x
        //     . . . x o
        //
        // This brings the number of pixels to visit down from 16 to 3.

        public readonly int PartialUpdateMask;

        public int Pixels => fullUpdatePixels.Length;

        private readonly Pixel[] fullUpdatePixels;
        private readonly Pixel[] partialUpdatePixels;

        private struct Pixel
        {
            public int X;
            public int Y;
            public int Bit;
        }

        public JbigArithmeticTemplate(int[] coordinates) : this(coordinates, bitOffset: 0)
        {
        }

        public JbigArithmeticTemplate(int[] coordinates, int bitOffset)
        {
            var partial = new List<Pixel>(coordinates.Length);
            var full = new List<Pixel>(coordinates.Length);

            const int XOffset = 0;
            const int YOffset = 1;
            const int NextXOffset = 2;
            const int NextYOffset = 3;

            var contextBit = 1 << (coordinates.Length / 2 + bitOffset);
            PartialUpdateMask |= contextBit;

            for (var i = 0; i < coordinates.Length; i += 2)
            {
                contextBit >>= 1;

                var x = coordinates[i + XOffset];
                var y = coordinates[i + YOffset];

                var pixel = new Pixel
                {
                    X = x,
                    Y = y,
                    Bit = contextBit,
                };
                full.Add(pixel);

                var updateInPartialUpdate =
                    i + NextXOffset >= coordinates.Length || // Last pixel must always be updated
                    y != coordinates[i + NextYOffset] ||     // Next pixel is not on the same row
                    x + 1 != coordinates[i + NextXOffset];   // Next pixel is not adjacent

                if (updateInPartialUpdate)
                {
                    partial.Add(pixel);
                    PartialUpdateMask |= contextBit;
                }
            }

            // Truncate
            PartialUpdateMask = ~PartialUpdateMask;

            this.partialUpdatePixels = partial.ToArray();
            this.fullUpdatePixels = full.ToArray();
        }

        public void FullUpdate(JbigBitmap bitmap, int currentX, int currentY, ref int context)
        {
            Update(fullUpdatePixels, bitmap, currentX, currentY, ref context);
        }

        public void PartialUpdate(JbigBitmap bitmap, int currentX, int currentY, ref int context)
        {
            Update(partialUpdatePixels, bitmap, currentX, currentY, ref context);
        }

        private static void Update(Pixel[] pixels, JbigBitmap bitmap, int currentX, int currentY, ref int context)
        {
            for (var i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];

                var x = currentX + pixel.X;
                if (x < 0 || x >= bitmap.Width)
                {
                    continue;
                }

                var y = currentY + pixel.Y;
                if (y < 0 || y >= bitmap.Height)
                {
                    continue;
                }

                if (bitmap[x, y])
                {
                    context |= pixel.Bit;
                }
            }
        }
    }
}
