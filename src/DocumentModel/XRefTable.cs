using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.DocumentModel
{
    internal class XRefTable : Collection<XRef>
    {
        public PdfDictionary Trailer { get; set; } = new PdfDictionary();
    }
}
