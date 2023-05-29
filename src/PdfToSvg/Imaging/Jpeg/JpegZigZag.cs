// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpeg
{
    internal static class JpegZigZag
    {
        private static readonly int[] order = new int[]
        {
            00, 01, 05, 06, 14, 15, 27, 28,
            02, 04, 07, 13, 16, 26, 29, 42,
            03, 08, 12, 17, 25, 30, 41, 43,
            09, 11, 18, 24, 31, 40, 44, 53,
            10, 19, 23, 32, 39, 45, 52, 54,
            20, 22, 33, 38, 46, 51, 55, 60,
            21, 34, 37, 47, 50, 56, 59, 61,
            35, 36, 48, 49, 57, 58, 62, 63,
        };

        private static readonly int[] reverseOrder = new int[64];

        static JpegZigZag()
        {
            for (var i = 0; i < order.Length; i++)
            {
                reverseOrder[order[i]] = i;
            }
        }

        public static void ZigZag<T>(T[] input, T[] output)
        {
            if (input.Length < order.Length) throw new ArgumentException("Too small input", nameof(input));
            if (output.Length < order.Length) throw new ArgumentException("Too small output", nameof(output));

            for (var i = 0; i < order.Length; i++)
            {
                output[order[i]] = input[i];
            }
        }

        public static void ReverseZigZag<T>(T[] input, T[] output)
        {
            if (input.Length < reverseOrder.Length) throw new ArgumentException("Too small input", nameof(input));
            if (output.Length < reverseOrder.Length) throw new ArgumentException("Too small output", nameof(output));

            for (var i = 0; i < reverseOrder.Length; i++)
            {
                output[reverseOrder[i]] = input[i];
            }
        }
    }
}
