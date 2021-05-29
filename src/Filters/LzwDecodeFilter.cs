using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Filters
{
    internal class LzwDecodeFilter : Filter
    {
        public override Stream Decode(Stream stream, PdfDictionary? decodeParms)
        {
            // PDF spec 1.7, table 8, page 36
            var earlyChange = decodeParms.GetValueOrDefault(Names.EarlyChange, defaultValue: 1) == 1;

            var lzwStream = new LzwDecodeStream(stream, earlyChange);
            return PredictorStream.Create(lzwStream, decodeParms);
        }
    }
}
