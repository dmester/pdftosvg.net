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
        public BufferedMemoryReader(byte[] data) : base(data)
        {
            bufferLength = data.Length;
            estimatedStreamPosition = data.Length;
        }

        public override long Length => buffer.Length;

        protected override void SeekCore(long position) { }

        protected override int ReadUnbuffered(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        protected override Task<int> ReadUnbufferedAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public override Task FillBufferAsync()
        {
            return Task.FromResult(true);
        }

        public override void FillBuffer()
        {
        }
    }
}
