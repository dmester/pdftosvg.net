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
        private readonly long startPosition;

        public PdfOnDemandStream(PdfDictionary owner, InputFile file, long startPosition) : base(owner)
        {
            this.file = file;
            this.startPosition = startPosition;
        }

        public override Stream Open()
        {
            if (owner.TryGetInteger(Names.Length, out var streamLength))
            {
                return file.CreateExclusiveSliceReader(startPosition, streamLength);
            }
            else
            {
                // TODO extract exception
                throw new Exception("No length.");
            }
        }

        public override async Task<Stream> OpenAsync()
        {
            if (owner.TryGetInteger(Names.Length, out var streamLength))
            {
                var reader = await file.CreateExclusiveSliceReaderAsync(startPosition, streamLength, Math.Min(8 * 1024, streamLength));
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
            else
            {
                // TODO extract exception
                throw new Exception("No length.");
            }
        }
    }
}
