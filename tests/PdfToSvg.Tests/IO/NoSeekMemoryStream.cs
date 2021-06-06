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
    /// <summary>
    /// A readonly non-seekable <see cref="MemoryStream"/> variant for testing purposes.
    /// </summary>
    internal class NoSeekMemoryStream : Stream
    {
        private readonly byte[] data;
        private readonly int offset;
        private readonly int length;
        private int position;
        private bool disposed;

        public NoSeekMemoryStream(byte[] data)
        {
            this.data = data;
            this.offset = 0;
            this.length = data.Length;
        }

        public NoSeekMemoryStream(byte[] data, int offset, int length)
        {
            this.data = data;
            this.offset = offset;
            this.length = length;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (disposed) throw new ObjectDisposedException(nameof(NoSeekMemoryStream));

            if (position < length)
            {
                var copy = Math.Min(count, length - position);
                Buffer.BlockCopy(data, this.offset + position, buffer, offset, copy);
                position += copy;
                return copy;
            }

            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
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
            disposed = true;
        }
    }
}
