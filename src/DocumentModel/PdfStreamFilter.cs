using PdfToSvg.Filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.DocumentModel
{
    internal class PdfStreamFilter
    {
        public PdfStreamFilter(Filter filter, PdfDictionary? decodeParms)
        {
            Filter = filter;
            DecodeParms = decodeParms;
        }

        public Filter Filter { get; }

        public PdfDictionary? DecodeParms { get; }

        public Stream Decode(Stream encodedStream)
        {
            return Filter.Decode(encodedStream, DecodeParms);
        }
    }
}
