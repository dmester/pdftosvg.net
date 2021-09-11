// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.DocumentModel
{
    internal class PdfOnDemandStream : PdfStream
    {
        private readonly InputFile file;

        public PdfOnDemandStream(PdfDictionary owner, InputFile file, long offset) : base(owner)
        {
            this.file = file;
            this.Offset = offset;
        }

        public long Offset { get; }
        public long Length => owner.GetValueOrDefault(Names.Length, 0);

        public override Stream Open(CancellationToken cancellationToken)
        {
            return file.CreateSliceReader(Offset, Length, cancellationToken);
        }

#if HAVE_ASYNC
        public override async Task<Stream> OpenAsync(CancellationToken cancellationToken)
        {
            var reader = await file.CreateSliceReaderAsync(Offset, Length, (int)Math.Min(8 * 1024, Length), cancellationToken).ConfigureAwait(false);
            var success = false;

            try
            {
                await reader.FillBufferAsync().ConfigureAwait(false);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    reader.Dispose();
                }
            }

            return reader;
        }
#endif
    }
}
