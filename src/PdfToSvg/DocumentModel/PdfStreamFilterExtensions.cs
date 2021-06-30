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
        public static Stream Decode(this IEnumerable<PdfStreamFilter> filters, Stream encodedStream)
        {
            var result = encodedStream;
            var disposeResult = true;

            try
            {
                foreach (var filter in filters)
                {
                    result = filter.Decode(result);
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
