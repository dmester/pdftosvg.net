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

namespace PdfToSvg.IO
{
    [DebuggerDisplay("{DebugView,nq}")]
    internal class BufferedMemoryReader : BufferedReader
    {
        private readonly long length;

        public BufferedMemoryReader(byte[] data) : base(data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            bufferLength = data.Length;
            estimatedStreamPosition = data.Length;
            length = data.Length;
        }

        public BufferedMemoryReader(byte[] data, int offset, int count) : base(data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || offset + count > data.Length) throw new ArgumentOutOfRangeException(nameof(count));

            bufferLength = offset + count;
            readCursor = offset;
            estimatedStreamPosition = count;
            length = count;
        }

        public override long Length => length;

        protected override void SeekCore(long position) { }

        protected override int ReadUnbuffered(byte[] buffer, int offset, int count)
        {
            return 0;
        }

#if HAVE_ASYNC
        protected override Task<int> ReadUnbufferedAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public override Task FillBufferAsync()
        {
            return Task.FromResult(true);
        }
#endif

        public override void FillBuffer()
        {
        }
    }
}
