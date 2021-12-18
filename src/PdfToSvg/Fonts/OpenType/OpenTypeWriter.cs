// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType
{
    internal struct OpenTypeWriterField32
    {
        private readonly OpenTypeWriter writer;
        private readonly int position;

        public OpenTypeWriterField32(OpenTypeWriter writer, int position)
        {
            this.writer = writer;
            this.position = position;
        }

        public void WriteUInt32(uint value)
        {
            if (writer == null) throw new InvalidOperationException("Cannot set value of uninitialized field.");

            var origPos = writer.Position;
            writer.Position = position;
            writer.WriteUInt32(value);
            writer.Position = origPos;
        }

        public void WriteInt32(int value) => WriteUInt32((uint)value);
    }

    internal struct OpenTypeWriterField16
    {
        private readonly OpenTypeWriter writer;
        private readonly int position;

        public OpenTypeWriterField16(OpenTypeWriter writer, int position)
        {
            this.writer = writer;
            this.position = position;
        }

        public void WriteInt16(short value)
        {
            if (writer == null) throw new InvalidOperationException("Cannot set value of uninitialized field.");

            var origPos = writer.Position;
            writer.Position = position;
            writer.WriteInt16(value);
            writer.Position = origPos;
        }

        public void WriteUInt16(ushort value) => WriteInt16((short)value);
    }

    internal class OpenTypeWriter : MemoryWriter
    {
        private static readonly DateTime Epoch = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public OpenTypeWriter() { }
        public OpenTypeWriter(int capacity) : base(capacity) { }

        public uint Checksum(int startIndex, int endIndex)
        {
            if (startIndex < 0 || startIndex > length) throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (endIndex < 0 || endIndex < startIndex || endIndex > length) throw new ArgumentOutOfRangeException(nameof(endIndex));

            var result = 0;
            var shift = 24;

            for (var i = startIndex; i < endIndex; i++)
            {
                result = result + (buffer[i] << shift);

                shift -= 8;

                if (shift < 0)
                {
                    shift = 24;
                }
            }

            return (uint)result;
        }

        public void WritePaddedBytes(byte[] value, int count, byte paddedByte = 0)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            EnsureCapacity(cursor + count);

            if (value.Length >= count)
            {
                Buffer.BlockCopy(value, 0, buffer, cursor, count);
                cursor += count;
            }
            else
            {
                Buffer.BlockCopy(value, 0, buffer, cursor, value.Length);

                cursor += value.Length;

                for (var i = value.Length; i < count; i++)
                {
                    buffer[cursor++] = paddedByte;
                }
            }

            if (cursor > length)
            {
                length = cursor;
            }
        }

        public void WriteBytes(byte[] value)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            EnsureCapacity(cursor + value.Length);

            Buffer.BlockCopy(value, 0, buffer, cursor, value.Length);

            cursor += value.Length;

            if (cursor > length)
            {
                length = cursor;
            }
        }

        public void WriteField16(out OpenTypeWriterField16 field)
        {
            field = new OpenTypeWriterField16(this, cursor);

            cursor += 2;

            if (cursor > length)
            {
                length = cursor;
            }
        }

        public void WriteField32(out OpenTypeWriterField32 field)
        {
            field = new OpenTypeWriterField32(this, cursor);

            cursor += 4;

            if (cursor > length)
            {
                length = cursor;
            }
        }

        public void WriteBytes(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

            EnsureCapacity(cursor + count);

            Buffer.BlockCopy(buffer, offset, this.buffer, cursor, count);

            cursor += count;

            if (cursor > length)
            {
                length = cursor;
            }
        }

        public void WriteAscii(string value)
        {
            WriteBytes(Encoding.ASCII.GetBytes(value));
        }

        public void WritePaddedAscii(string value, int length, char paddingChar = ' ')
        {
            if (value == null)
            {
                value = "";
            }

            if (value.Length > length)
            {
                value = value.Substring(0, length);
            }
            else if (value.Length < length)
            {
                value = value.PadRight(length, paddingChar);
            }

            WriteAscii(value);
        }

        public void WriteInt16(short value)
        {
            EnsureCapacity(cursor + 2);

            buffer[cursor + 0] = (byte)(value >> 8);
            buffer[cursor + 1] = (byte)(value);

            cursor += 2;

            if (cursor > length)
            {
                length = cursor;
            }
        }

        public void WriteUInt16(ushort value) => WriteInt16((short)value);

        public void WriteUInt32(uint value)
        {
            EnsureCapacity(cursor + 4);

            buffer[cursor + 0] = (byte)(value >> 24);
            buffer[cursor + 1] = (byte)(value >> 16);
            buffer[cursor + 2] = (byte)(value >> 8);
            buffer[cursor + 3] = (byte)(value);

            cursor += 4;

            if (cursor > length)
            {
                length = cursor;
            }
        }

        public void WriteInt32(int value) => WriteUInt32((uint)value);

        public void WriteFixed(decimal value)
        {
            WriteInt32((int)(value * (1 << 16)));
        }

        public void WriteDateTime(DateTime dt)
        {
            EnsureCapacity(cursor + 8);

            var seconds = (long)(dt.ToUniversalTime() - Epoch).TotalSeconds;

            buffer[cursor + 0] = (byte)(seconds >> 56);
            buffer[cursor + 1] = (byte)(seconds >> 48);
            buffer[cursor + 2] = (byte)(seconds >> 40);
            buffer[cursor + 3] = (byte)(seconds >> 32);
            buffer[cursor + 4] = (byte)(seconds >> 24);
            buffer[cursor + 5] = (byte)(seconds >> 16);
            buffer[cursor + 6] = (byte)(seconds >> 8);
            buffer[cursor + 7] = (byte)(seconds);

            cursor += 8;

            if (cursor > length)
            {
                length = cursor;
            }
        }
    }
}
