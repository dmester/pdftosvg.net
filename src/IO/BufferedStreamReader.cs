using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.IO
{
    internal class BufferedStreamReader : BufferedReader
    {
        private Stream? stream;
        private Action? customDisposer;

        private readonly long offset;
        private readonly long length;

        public BufferedStreamReader(Stream stream, long offset, long length, Action? customDisposer = null, int bufferSize = 4096)
            : base(new byte[bufferSize])
        {
            this.customDisposer = customDisposer;
            this.stream = stream;
            this.offset = offset;
            this.length = length;
        }

        public BufferedStreamReader(Stream stream, Action customDisposer = null, int bufferSize = 4096)
            : base(new byte[bufferSize])
        {
            this.customDisposer = customDisposer;
            this.stream = stream;
            this.offset = 0;
            this.length = -1;
        }

        public Stream BaseStream => stream;

        public override long Length => length < 0 ? stream.Length : length;

        private int LimitReadByteCount(int desiredCount)
        {
            if (length >= 0 && desiredCount > length - estimatedStreamPosition)
            {
                return (int)(length - estimatedStreamPosition);
            }

            return desiredCount;
        }

        public override async Task FillBufferAsync()
        {
            if (bufferLength == 0 || readCursor > bufferLength / 2)
            {
                ShiftBuffer();

                if (stream != null)
                {
                    var bytesToRead = LimitReadByteCount(buffer.Length - bufferLength);
                    var bytesRead = await stream.ReadAsync(buffer, bufferLength, bytesToRead);

                    bufferLength += bytesRead;
                    estimatedStreamPosition += bytesRead;
                }
            }
        }

        public override void FillBuffer()
        {
            ShiftBuffer();

            if (stream != null)
            {
                var bytesToRead = LimitReadByteCount(buffer.Length - bufferLength);
                var bytesRead = stream.Read(buffer, bufferLength, bytesToRead);

                bufferLength += bytesRead;
                estimatedStreamPosition += bytesRead;
            }
        }

        protected override int ReadUnbuffered(byte[] buffer, int offset, int count)
        {
            return stream.Read(buffer, offset, LimitReadByteCount(count));
        }

        protected override Task<int> ReadUnbufferedAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return stream.ReadAsync(buffer, offset, LimitReadByteCount(count), cancellationToken);
        }

        protected override void SeekCore(long position)
        {
            stream.Seek(position + offset, SeekOrigin.Begin);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (customDisposer != null)
                {
                    customDisposer();
                    customDisposer = null;
                }
                else if (stream != null)
                {
                    stream.Dispose();
                    stream = null;
                }
            }
        }
    }
}
