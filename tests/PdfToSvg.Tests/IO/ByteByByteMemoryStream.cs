// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.IO
{
    internal class ByteByByteMemoryStream : MemoryStream
    {
        public ByteByByteMemoryStream(params byte[] buffer) : base(buffer)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return base.Read(buffer, offset, Math.Min(count, 1));
        }
    }
}
