// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Common
{
    internal static class PageRangeExtensions
    {
        public static IEnumerable<int> Pages(this IEnumerable<PageRange> ranges, int totalPageCount)
        {
            var pages = new bool[totalPageCount];

            foreach (var range in ranges)
            {
                var from = (range.From < 1 ? 1 : range.From) - 1;
                var to = (range.To < 0 ? totalPageCount : range.To) - 1;

                for (var page = from; page <= to && page < pages.Length; page++)
                {
                    pages[page] = true;
                }
            }

            for (var page = 0; page < pages.Length; page++)
            {
                if (pages[page])
                {
                    yield return page + 1;
                }
            }
        }
    }
}
