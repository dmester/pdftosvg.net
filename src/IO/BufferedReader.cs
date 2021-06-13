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
    [DebuggerDisplay("{DebugView,nq}")]
    internal abstract class BufferedReader : Stream
    {
        protected readonly byte[] buffer;

        protected int bufferLength;
        protected int readCursor;
        protected long estimatedStreamPosition;

        public const char EndOfStreamMarker = char.MaxValue;

        protected BufferedReader(byte[] buffer)
        {
            this.buffer = buffer;
        }

        public override long Position
        {
            get => estimatedStreamPosition - bufferLength + readCursor;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        private string DebugView
        {
            get
            {
                var start = Math.Max(readCursor - 18, 0);
                var end = Math.Min(start + 60, bufferLength);

                var sb = new StringBuilder();
                sb.Append("Reader | \"");

                for (var i = start; i < end; i++)
                {
                    if (i == readCursor)
                    {
                        sb.Append(" \u2192");
                    }

                    var ch = (char)buffer[i];
                    sb.Append(ch >= ' ' && ch < '~' ? ch : '_');
                }

                sb.Append('"');

                return sb.ToString();
            }
        }

        public abstract Task FillBufferAsync();

        public abstract void FillBuffer();

        protected void ShiftBuffer()
        {
            if (readCursor < bufferLength)
            {
                for (var i = readCursor; i < bufferLength; i++)
                {
                    buffer[i - readCursor] = buffer[i];
                }

                bufferLength -= readCursor;
            }
            else
            {
                bufferLength = 0;
            }

            readCursor = 0;
        }

        protected abstract void SeekCore(long position);

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = offset;
                    break;

                case SeekOrigin.Current:
                    newPosition = Position + offset;
                    break;

                case SeekOrigin.End:
                    newPosition = Length + offset;
                    break;

                default:
                    throw new ArgumentException(nameof(origin));
            }

            if (newPosition < 0)
            {
                newPosition = 0;
            }

            if (newPosition > Length)
            {
                newPosition = Length;
            }

            long bufferStartPosition = estimatedStreamPosition - bufferLength;

            if (newPosition < bufferStartPosition ||
                newPosition > bufferStartPosition + bufferLength)
            {
                // Seek stream first, to ensure it is seekable
                SeekCore(newPosition);
                estimatedStreamPosition = newPosition;
                readCursor = 0;
                bufferLength = 0;
            }
            else
            {
                // Seek inside buffer
                readCursor = (int)(newPosition - bufferStartPosition);
            }

            return newPosition;
        }

        public void Skip() => Skip(1);

        public void Skip(int count)
        {
            if (readCursor + count <= bufferLength)
            {
                readCursor += count;
            }
            else
            {
                Seek(1, SeekOrigin.Current);
            }
        }

        protected abstract int ReadUnbuffered(byte[] buffer, int offset, int count);

        protected abstract Task<int> ReadUnbufferedAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

        private void ReadFromBuffer(byte[] buffer, ref int read, ref int offset, ref int count)
        {
            if (count > 0 && readCursor < bufferLength)
            {
                var copyByteCount = Math.Min(count, bufferLength - readCursor);

                Buffer.BlockCopy(this.buffer, readCursor, buffer, offset, copyByteCount);

                read += copyByteCount;
                readCursor += copyByteCount;
                offset += copyByteCount;
                count -= copyByteCount;
            }
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var read = 0;

            ReadFromBuffer(buffer, ref read, ref offset, ref count);

            if (count > 0)
            {
                if (count < this.buffer.Length)
                {
                    await FillBufferAsync().ConfigureAwait(false);
                    ReadFromBuffer(buffer, ref read, ref offset, ref count);
                }
                else
                {
                    var readFromStream = await ReadUnbufferedAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                    if (readFromStream > 0)
                    {
                        read += readFromStream;
                        estimatedStreamPosition += readFromStream;
                        readCursor = 0;
                        bufferLength = 0;
                    }
                }
            }

            return read;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = 0;

            ReadFromBuffer(buffer, ref read, ref offset, ref count);

            if (count > 0)
            {
                if (count < this.buffer.Length)
                {
                    FillBuffer();
                    ReadFromBuffer(buffer, ref read, ref offset, ref count);
                }
                else
                {
                    var readFromStream = ReadUnbuffered(buffer, offset, count);
                    if (readFromStream > 0)
                    {
                        read += readFromStream;
                        estimatedStreamPosition += readFromStream;
                        readCursor = 0;
                        bufferLength = 0;
                    }
                }
            }

            return read;
        }

        public char ReadChar()
        {
            if (readCursor >= bufferLength)
            {
                FillBuffer();
            }

            if (readCursor >= bufferLength)
            {
                return EndOfStreamMarker;
            }

            return unchecked((char)buffer[readCursor++]);
        }

        public char PeekChar() => PeekChar(1);

        public char PeekChar(int offset)
        {
            if (offset < 1 || offset > buffer.Length / 2)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (readCursor + offset - 1 >= bufferLength)
            {
                FillBuffer();
            }

            if (readCursor + offset - 1 >= bufferLength)
            {
                return EndOfStreamMarker;
            }

            return unchecked((char)buffer[readCursor + offset - 1]);
        }

        public override int ReadByte()
        {
            var ch = ReadChar();
            return ch == EndOfStreamMarker ? -1 : ch;
        }

        public int PeekByte() => PeekByte(1);

        public int PeekByte(int offset)
        {
            var ch = PeekChar(offset);
            return ch == EndOfStreamMarker ? -1 : unchecked((int)ch);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void Flush() { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void SetLength(long value) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
