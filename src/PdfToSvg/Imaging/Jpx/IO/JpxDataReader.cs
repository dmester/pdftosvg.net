// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.IO;

namespace PdfToSvg.Imaging.Jpx.IO
{
    internal sealed class JpxDataReader
    {
        private readonly byte[] buffer;
        private readonly int offset;
        private readonly int length;
        private int cursor;

        public static JpxDataReader Empty { get; } = new JpxDataReader(ArrayUtils.Empty<byte>(), 0, 0);

        public JpxDataReader(ArraySegment<byte> data)
        {
            this.buffer = data.Array ?? throw new ArgumentException("Array cannot be null.", nameof(data));
            this.offset = data.Offset;
            this.length = data.Count;
        }

        public JpxDataReader(byte[] buffer, int offset, int count)
        {
            this.buffer = buffer;
            this.offset = offset;
            this.length = count;
        }

        public JpxDataReader(byte[] buffer)
        {
            this.buffer = buffer;
            this.length = buffer.Length;
        }

        #region Properties

        public int Cursor
        {
            get => cursor;
            set => cursor = value;
        }

        public int Length => length;

        public bool EndOfStream => cursor >= length;

        public ArraySegment<byte> Data => new ArraySegment<byte>(buffer, offset, length);

        public ArraySegment<byte> RemainingData => new ArraySegment<byte>(buffer, offset + cursor, length - cursor);

        #endregion

        #region Reader methods

        public byte ReadByte()
        {
            if (cursor >= length)
            {
                throw new EndOfStreamException();
            }

            return buffer[offset + cursor++];
        }

        public ushort ReadUInt16()
        {
            if (cursor + 2 > length)
            {
                throw new EndOfStreamException();
            }

            var startIndex = offset + cursor;
            var result = (ushort)((
                buffer[startIndex + 0] << 8) |
                buffer[startIndex + 1]
                );
            cursor += 2;
            return result;
        }

        public uint ReadUInt32()
        {
            if (cursor + 4 > length)
            {
                throw new EndOfStreamException();
            }

            var startIndex = offset + cursor;
            var result =
                ((uint)buffer[startIndex + 0] << 24) |
                ((uint)buffer[startIndex + 1] << 16) |
                ((uint)buffer[startIndex + 2] << 8) |
                ((uint)buffer[startIndex + 3]);
            cursor += 4;
            return result;
        }

        public ulong ReadUInt64()
        {
            if (cursor + 8 > length)
            {
                throw new EndOfStreamException();
            }

            var startIndex = offset + cursor;
            var result =
                ((ulong)buffer[startIndex + 0] << 56) |
                ((ulong)buffer[startIndex + 1] << 48) |
                ((ulong)buffer[startIndex + 2] << 40) |
                ((ulong)buffer[startIndex + 3] << 32) |
                ((ulong)buffer[startIndex + 4] << 24) |
                ((ulong)buffer[startIndex + 5] << 16) |
                ((ulong)buffer[startIndex + 6] << 8) |
                ((ulong)buffer[startIndex + 7]);
            cursor += 8;
            return result;
        }

        public short ReadInt16() => unchecked((short)ReadUInt16());
        public int ReadInt32() => unchecked((int)ReadUInt32());
        public long ReadInt64() => unchecked((long)ReadUInt64());

        public ArraySegment<byte> ReadBytes(int byteCount)
        {
            if (byteCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            }
            if (byteCount > length - cursor)
            {
                throw new EndOfStreamException();
            }

            var segment = new ArraySegment<byte>(buffer, offset + cursor, byteCount);
            cursor += byteCount;
            return segment;
        }

        public void SkipBytes(int byteCount)
        {
            if (byteCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            }
            if (byteCount > length - cursor)
            {
                throw new EndOfStreamException();
            }

            cursor += byteCount;
        }

        #endregion

        #region Reader slicing

        public JpxDataReader Slice(int byteCount)
        {
            if (byteCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            }
            if (byteCount > length - cursor)
            {
                throw new EndOfStreamException();
            }

            var sliced = new JpxDataReader(buffer, offset + cursor, byteCount);
            cursor += byteCount;
            return sliced;
        }

        #endregion
    }
}
