using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Filters
{
    internal class IdentityFilter : Filter
    {
        public override Stream Decode(Stream stream, PdfDictionary? decodeParms)
        {
            return stream;
        }
    }
}
