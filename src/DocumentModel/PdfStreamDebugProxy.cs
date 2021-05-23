using PdfToSvg.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.DocumentModel
{
    internal class PdfStreamDebugProxy
    {
        private readonly PdfStream streamInfo;
        private string content;

        public PdfStreamDebugProxy(PdfStream streamInfo)
        {
            this.streamInfo = streamInfo;
        }

        public IReadOnlyList<PdfStreamFilter> Filters => streamInfo.Filters;

        public string Content
        {
            get
            {
                if (content == null)
                {
                    using (var stream = streamInfo.OpenDecoded())
                    {
                        var encoding = new PdfDocEncoding();
                        var buffer = new byte[8 * 1024];
                        var read = stream.Read(buffer, 0, buffer.Length);

                        content = encoding.GetString(buffer, 0, read);
                    }
                }

                return content;
            }
        }
    }
}
