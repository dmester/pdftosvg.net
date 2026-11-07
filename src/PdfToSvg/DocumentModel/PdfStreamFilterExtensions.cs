// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.DocumentModel
{
    internal static class PdfStreamFilterExtensions
    {
        private const int MaxFilterCount = 10;

        public static Stream Decode(this IList<PdfStreamFilter> filters, Stream encodedStream, bool skipLast = false)
        {
            var result = encodedStream;
            var disposeResult = true;

            try
            {
                if (filters.Count > MaxFilterCount)
                {
                    throw new PdfException("Max filter count exceeded. Filters found: " + filters.Count);
                }

                var count = filters.Count - (skipLast ? 1 : 0);
                for (var i = 0; i < count; i++)
                {
                    result = filters[i].Decode(result);
                }

                disposeResult = false;
            }
            finally
            {
                if (disposeResult)
                {
                    result.Dispose();
                }
            }

            return result;
        }
    }
}
