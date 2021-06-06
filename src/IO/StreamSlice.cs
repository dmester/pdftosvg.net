// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg
{
    // TODO test
    internal class StreamSlice : Stream
    {
        private Stream? stream;
        private long offset;
        private long length;
        private long cursor;

        public StreamSlice(Stream stream, long length)
        {
            this.stream = stream;
            this.length = length;

            try
            {
                this.offset = stream.Position;
            }
            catch
            {
                this.offset = -1;
            }
        }

        public override bool CanRead => stream != null && stream.CanRead;

        public override bool CanSeek => stream != null && stream.CanSeek;

        public override bool CanWrite => false;

        public override long Length => length;

        public override long Position
        {
            get => cursor;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override void Flush()
        {
            if (stream == null) throw new ObjectDisposedException(nameof(StreamSlice));

            stream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (stream == null) throw new ObjectDisposedException(nameof(StreamSlice));

            return stream.FlushAsync(cancellationToken);
        }

        private int LimitCount(int count)
        {
            if (count > length - cursor)
            {
                count = (int)(length - cursor);
            }
            return count;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (stream == null) throw new ObjectDisposedException(nameof(StreamSlice));

            count = LimitCount(count);

            if (count > 0)
            {
                var read = stream.Read(buffer, offset, count);
                this.cursor += read;
                return read;
            }

            return 0;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (stream == null) throw new ObjectDisposedException(nameof(StreamSlice));

            return stream.BeginRead(buffer, offset, LimitCount(count), callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (stream == null) throw new ObjectDisposedException(nameof(StreamSlice));

            var read = stream.EndRead(asyncResult);
            cursor += read;
            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (stream == null) throw new ObjectDisposedException(nameof(StreamSlice));

            var read = await stream.ReadAsync(buffer, offset, LimitCount(count), cancellationToken);
            cursor += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (stream == null) throw new ObjectDisposedException(nameof(StreamSlice));
            if (this.offset < 0) throw new NotSupportedException();

            long newPosition;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = offset;
                    break;
                case SeekOrigin.Current:
                    newPosition = cursor + offset;
                    break;
                case SeekOrigin.End:
                    newPosition = length + offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin));
            }

            if (newPosition < 0)
            {
                newPosition = 0;
            }
            if (newPosition > length)
            {
                newPosition = length;
            }

            stream.Position = this.offset + newPosition;
            cursor = newPosition;
            return newPosition;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }
    }
}
