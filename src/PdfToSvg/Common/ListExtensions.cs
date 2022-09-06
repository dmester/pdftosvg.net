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
        public static void Sort<T, TKey>(this List<T> list, Func<T, TKey> sortSelector)
        {
            var comparer = Comparer<TKey>.Default;
            list.Sort((a, b) => comparer.Compare(sortSelector(a), sortSelector(b)));
        }

        public static void Sort<T, TKey1, TKey2>(this List<T> list, Func<T, TKey1> sortSelector1, Func<T, TKey2> sortSelector2)
        {
            var comparer1 = Comparer<TKey1>.Default;
            var comparer2 = Comparer<TKey2>.Default;

            list.Sort((a, b) =>
            {
                var result = comparer1.Compare(sortSelector1(a), sortSelector1(b));

                if (result == 0)
                {
                    result = comparer2.Compare(sortSelector2(a), sortSelector2(b));
                }

                return result;
            });
        }

        public static void Sort<T, TKey1, TKey2, TKey3>(this List<T> list, Func<T, TKey1> sortSelector1, Func<T, TKey2> sortSelector2, Func<T, TKey3> sortSelector3)
        {
            var comparer1 = Comparer<TKey1>.Default;
            var comparer2 = Comparer<TKey2>.Default;
            var comparer3 = Comparer<TKey3>.Default;

            list.Sort((a, b) =>
            {
                var result = comparer1.Compare(sortSelector1(a), sortSelector1(b));

                if (result == 0)
                {
                    result = comparer2.Compare(sortSelector2(a), sortSelector2(b));

                    if (result == 0)
                    {
                        result = comparer3.Compare(sortSelector3(a), sortSelector3(b));
                    }
                }

                return result;
            });
        }

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
