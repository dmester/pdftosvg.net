// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.DocumentModel
{
#if DEBUG
    [DebuggerTypeProxy(typeof(PdfStreamDebugProxy))]
#endif
    internal class PdfMemoryStream : PdfStream
    {
        private readonly byte[] content;
        private readonly int length;

        public PdfMemoryStream(PdfDictionary owner, byte[] content, int length) : base(owner)
        {
            this.content = content ?? throw new ArgumentNullException(nameof(content));
            this.length = length;
        }

        public override Stream Open(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new MemoryStream(content, 0, length, false);
        }

#if HAVE_ASYNC
        public override Task<Stream> OpenAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Open(cancellationToken));
        }
#endif
    }
}
