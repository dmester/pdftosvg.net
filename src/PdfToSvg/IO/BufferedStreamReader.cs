// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

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

            if (stream.CanSeek)
            {
                estimatedStreamPosition = stream.Position - offset;
            }
            else
            {
                estimatedStreamPosition = 0;
            }
        }

        public BufferedStreamReader(Stream stream, Action? customDisposer = null, int bufferSize = 4096)
            : base(new byte[bufferSize])
        {
            this.customDisposer = customDisposer;
            this.stream = stream;
            this.offset = 0;
            this.length = -1;

            if (stream.CanSeek)
            {
                estimatedStreamPosition = stream.Position;
            }
            else
            {
                estimatedStreamPosition = 0;
            }
        }

        public Stream BaseStream => stream ?? throw new ObjectDisposedException(nameof(BufferedStreamReader));

        public override long Length
        {
            get
            {
                if (stream == null)
                {
                    return 0;
                }

                if (length >= 0)
                {
                    return length;
                }

                return stream.Length - offset;
            }
        }

        private int LimitReadByteCount(int desiredCount)
        {
            if (length >= 0 && desiredCount > length - estimatedStreamPosition)
            {
                return (int)(length - estimatedStreamPosition);
            }

            return desiredCount;
        }

#if HAVE_ASYNC
        public override async Task FillBufferAsync()
        {
            if (bufferLength == 0 || readCursor > bufferLength / 2)
            {
                ShiftBuffer();

                if (stream != null)
                {
                    var bytesToRead = LimitReadByteCount(buffer.Length - bufferLength);
                    if (bytesToRead > 0)
                    {
                        var bytesRead = await stream.ReadAsync(buffer, bufferLength, bytesToRead).ConfigureAwait(false);

                        bufferLength += bytesRead;
                        estimatedStreamPosition += bytesRead;
                    }
                }
            }
        }
#endif

        public override void FillBuffer()
        {
            ShiftBuffer();

            if (stream != null)
            {
                var bytesToRead = LimitReadByteCount(buffer.Length - bufferLength);
                if (bytesToRead > 0)
                {
                    var bytesRead = stream.Read(buffer, bufferLength, bytesToRead);

                    bufferLength += bytesRead;
                    estimatedStreamPosition += bytesRead;
                }
            }
        }

        protected override int ReadUnbuffered(byte[] buffer, int offset, int count)
        {
            if (stream == null) throw new ObjectDisposedException(nameof(BufferedStreamReader));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

            count = LimitReadByteCount(count);

            return count > 0 ? stream.Read(buffer, offset, count) : 0;
        }

#if HAVE_ASYNC
        protected override Task<int> ReadUnbufferedAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (stream == null) throw new ObjectDisposedException(nameof(BufferedStreamReader));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

            count = LimitReadByteCount(count);

            return count > 0 ?
                stream.ReadAsync(buffer, offset, count, cancellationToken) :
                Task.FromResult(0);
        }
#endif

        protected override void SeekCore(long position)
        {
            if (stream == null) throw new ObjectDisposedException(nameof(BufferedStreamReader));

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
                }

                stream = null;
            }
        }
    }
}
