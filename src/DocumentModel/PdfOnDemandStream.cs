// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public override Stream Open()
        {
            return file.CreateExclusiveSliceReader(Offset, Length);
        }

        public override async Task<Stream> OpenAsync()
        {
            var reader = await file.CreateExclusiveSliceReaderAsync(Offset, Length, (int)Math.Min(8 * 1024, Length));
            try
            {
                await reader.FillBufferAsync();
                return reader;
            }
            catch
            {
                reader.Dispose();
                throw;
            }
        }
    }
}
