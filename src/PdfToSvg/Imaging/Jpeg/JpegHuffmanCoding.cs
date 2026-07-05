// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpeg
{
    internal static class JpegHuffmanCoding
    {
        public static int GetSsss(int value)
        {
            // ITU T.81 F.1.2.1
            // ssss = number of significant bits in |value| (0 when value == 0).

#if NET5_0_OR_GREATER
            if (value == 0)
            {
                return 0;
            }

            var magnitude = (uint)(value < 0 ? -value : value);
            return System.Numerics.BitOperations.Log2(magnitude) + 1;
#else
            if (value == 0)
            {
                return 0;
            }

            if (value >= -1 && value <= 1)
            {
                return 1;
            }

            if (value >= -3 && value <= 3)
            {
                return 2;
            }

            if (value >= -7 && value <= 7)
            {
                return 3;
            }

            if (value >= -15 && value <= 15)
            {
                return 4;
            }

            if (value >= -31 && value <= 31)
            {
                return 5;
            }

            if (value >= -63 && value <= 63)
            {
                return 6;
            }

            if (value >= -127 && value <= 127)
            {
                return 7;
            }

            if (value >= -255 && value <= 255)
            {
                return 8;
            }

            if (value >= -511 && value <= 511)
            {
                return 9;
            }

            if (value >= -1023 && value <= 1023)
            {
                return 10;
            }

            return 11;
#endif
        }

    }
}
