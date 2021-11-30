// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Common
{
    internal static class ListExtensions
    {
        public static void RollEnd<T>(this List<T> list, int windowSize, int shiftAmount)
        {
            if (windowSize <= 0)
            {
                return;
            }

            if (list.Count < windowSize)
            {
                throw new ArgumentOutOfRangeException(nameof(windowSize));
            }

            // Normalize shift amount to (1-n, 0]
            shiftAmount = shiftAmount % windowSize;
            if (shiftAmount > 0) shiftAmount -= windowSize;

            if (shiftAmount != 0)
            {
                var startIndex = list.Count - windowSize;

                for (var i = 0; i > shiftAmount; i--)
                {
                    list.Add(list[startIndex - i]);
                }

                list.RemoveRange(startIndex, -shiftAmount);
            }
        }

    }
}
