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

            foreach (var filter in filters)
            {
                result = filter.Decode(result);
            }

            return result;
        }
    }
}
