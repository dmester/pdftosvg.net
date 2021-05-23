using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.DocumentModel
{
    [DebuggerTypeProxy(typeof(PdfStreamDebugProxy))]
    internal class PdfMemoryStream : PdfStream
    {
        private readonly byte[] content;
        private readonly int length;
        
        public PdfMemoryStream(PdfDictionary owner, byte[] content, int length) : base(owner)
        {
            this.content = content ?? throw new ArgumentNullException(nameof(content));
            this.length = length;
        }

        public override Stream Open()
        {
            return new MemoryStream(content, 0, length, false);
        }

        public override Task<Stream> OpenAsync()
        {
            return Task.FromResult(Open());
        }
    }
}
