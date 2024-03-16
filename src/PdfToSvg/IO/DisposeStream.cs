// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.IO
{
    internal class DisposeStream : Stream
    {
        private Stream? baseStream;
        private Action? disposer;

        public DisposeStream(Stream baseStream, Action disposer)
        {
            this.baseStream = baseStream;
            this.disposer = disposer;
        }

        public override bool CanRead
        {
            get
            {
                if (baseStream == null) throw new ObjectDisposedException(nameof(DisposeStream));

                return baseStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                if (baseStream == null) throw new ObjectDisposedException(nameof(DisposeStream));

                return baseStream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                if (baseStream == null) throw new ObjectDisposedException(nameof(DisposeStream));

                return baseStream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                if (baseStream == null) throw new ObjectDisposedException(nameof(DisposeStream));

                return baseStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                if (baseStream == null) throw new ObjectDisposedException(nameof(DisposeStream));

                return baseStream.Position;
            }
            set
            {
                if (baseStream == null) throw new ObjectDisposedException(nameof(DisposeStream));

                baseStream.Position = value;
            }
        }

        public override void Flush()
        {
            if (baseStream == null) throw new ObjectDisposedException(nameof(DisposeStream));

            baseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (baseStream == null) throw new ObjectDisposedException(nameof(DisposeStream));

            return baseStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (baseStream == null) throw new ObjectDisposedException(nameof(DisposeStream));

            return baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            if (baseStream == null) throw new ObjectDisposedException(nameof(DisposeStream));

            baseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (baseStream == null) throw new ObjectDisposedException(nameof(DisposeStream));

            baseStream.Write(buffer, offset, count);
        }

#if HAVE_STREAM_BEGINEND
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            if (baseStream == null) throw new ObjectDisposedException(nameof(DisposeStream));

            return baseStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (baseStream == null) throw new ObjectDisposedException(nameof(DisposeStream));

            return baseStream.EndRead(asyncResult);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            if (baseStream == null) throw new ObjectDisposedException(nameof(DisposeStream));

            return baseStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (baseStream == null) throw new ObjectDisposedException(nameof(DisposeStream));

            baseStream.EndWrite(asyncResult);
        }
#endif

#if HAVE_ASYNC
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (baseStream == null) throw new ObjectDisposedException(nameof(DisposeStream));

            return baseStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (baseStream == null) throw new ObjectDisposedException(nameof(DisposeStream));

            return baseStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            if (baseStream == null) throw new ObjectDisposedException(nameof(DisposeStream));

            return baseStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }
#endif

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (baseStream != null)
                {
                    baseStream.Dispose();
                    baseStream = null;
                }

                if (disposer != null)
                {
                    disposer();
                    disposer = null;
                }
            }
        }
    }
}
