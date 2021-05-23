using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Filters
{
    internal abstract class DecodeStream : Stream
    {
        protected byte[] buffer;

        protected int bufferCursor;
        protected int bufferLength;

        protected bool endOfStream;
        protected long position;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => position; set => throw new NotSupportedException(); }

        protected abstract void FillBuffer();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = 0;

            while (read < count && (!endOfStream || bufferCursor < bufferLength))
            {
                if (bufferCursor >= bufferLength)
                {
                    FillBuffer();
                }

                var readThisIteration = Math.Min(bufferLength - bufferCursor, count - read);
                if (readThisIteration > 0)
                {
                    Buffer.BlockCopy(this.buffer, bufferCursor, buffer, offset + read, readThisIteration);

                    read += readThisIteration;
                    bufferCursor += readThisIteration;
                }
            }

            position += read;

            return read;
        }

        public override void Flush()
        {
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
    }
}
