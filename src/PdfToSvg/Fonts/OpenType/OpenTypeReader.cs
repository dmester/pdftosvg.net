// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType
{
    [DebuggerDisplay("OpenTypeReader (read {Position} of {Length} bytes)")]
    internal class OpenTypeReader
    {
        private static readonly DateTime Epoch = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly byte[] buffer;
        private readonly int startIndex, endIndex;
        private int cursor;

        public OpenTypeReader(byte[] buffer, int offset, int count)
        {
            this.buffer = buffer;
            this.cursor = offset;
            this.startIndex = offset;
            this.endIndex = offset + count;
        }

        public int Position
        {
            get => cursor - startIndex;
            set
            {
                cursor = startIndex + value;

                if (cursor < 0)
                {
                    cursor = 0;
                }
                else if (cursor > endIndex)
                {
                    cursor = endIndex;
                }
            }
        }

        public int Length => endIndex - startIndex;

        public OpenTypeReader Slice(int offset, int count)
        {
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || startIndex + offset + count > endIndex) throw new ArgumentOutOfRangeException(nameof(count));

            return new OpenTypeReader(buffer, this.startIndex + offset, count);
        }

        public byte[] ReadBytes(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (cursor + count > endIndex) throw new EndOfStreamException();

            var result = new byte[count];
            Buffer.BlockCopy(buffer, cursor, result, 0, count);

            cursor += count;

            return result;
        }

        public string ReadAscii(int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (cursor + length > endIndex) throw new EndOfStreamException();

            var result = Encoding.ASCII.GetString(buffer, cursor, length);

            cursor += length;

            return result;
        }

        public byte ReadUInt8()
        {
            if (cursor + 1 > endIndex) throw new EndOfStreamException();
            cursor++;
            return buffer[cursor - 1];
        }

        public sbyte ReadInt8() => (sbyte)ReadUInt8();

        public short ReadInt16()
        {
            if (cursor + 2 > endIndex) throw new EndOfStreamException();

            cursor += 2;

            return (short)(
                (buffer[cursor - 2] << 8) |
                (buffer[cursor - 1])
                );
        }

        public ushort ReadUInt16() => (ushort)ReadInt16();

        public int ReadInt32()
        {
            if (cursor + 4 > endIndex) throw new EndOfStreamException();

            cursor += 4;

            return
                (buffer[cursor - 4] << 24) |
                (buffer[cursor - 3] << 16) |
                (buffer[cursor - 2] << 8) |
                (buffer[cursor - 1]);
        }

        public uint ReadUInt32() => (uint)ReadInt32();

        public decimal ReadFixed() => ReadInt32() * (1M / (1 << 16));

        public DateTime ReadDateTime()
        {
            if (cursor + 8 > endIndex) throw new EndOfStreamException();

            cursor += 8;

            var seconds =
                ((long)buffer[cursor - 8] << 56) |
                ((long)buffer[cursor - 7] << 48) |
                ((long)buffer[cursor - 6] << 40) |
                ((long)buffer[cursor - 5] << 32) |
                ((long)buffer[cursor - 4] << 24) |
                ((long)buffer[cursor - 3] << 16) |
                ((long)buffer[cursor - 2] << 8) |
                ((long)buffer[cursor - 1]);

            return Epoch.AddSeconds(seconds);
        }
    }
}
