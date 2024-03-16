// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CompactFonts
{
    internal class CompactFontWriter : MemoryWriter
    {
        public CompactFontWriter() { }
        public CompactFontWriter(int capacity) : base(capacity) { }

        public void WriteHeader(CompactFontHeader header)
        {
            WriteCard8(header.Major);
            WriteCard8(header.Minor);
            WriteCard8(header.HdrSize);
            WriteCard8(header.OffSize);
        }

        public void WriteIndex(IList<int> index)
        {
            if (index.Count < 2)
            {
                WriteCard16(0); // Count
            }
            else
            {
                var valueOffset = 1 - index[0];
                var offSize = GetOffSize(index.Last() + valueOffset);

                WriteCard16(index.Count - 1);
                WriteCard8(offSize);

                for (var i = 0; i < index.Count; i++)
                {
                    WriteOffset(offSize, index[i] + valueOffset);
                }
            }
        }

        public void WriteIndex(IList<ArraySegment<byte>> indexData)
        {
            var index = new int[indexData.Count + 1];

            index[0] = cursor;

            for (var i = 0; i < indexData.Count; i++)
            {
                index[i + 1] = index[i] + indexData[i].Count;
            }

            WriteIndex(index);

            EnsureCapacity(cursor + index.Last());

            for (var i = 0; i < indexData.Count; i++)
            {
                var item = indexData[i];
                if (item.Array != null)
                {
                    Buffer.BlockCopy(item.Array, item.Offset, buffer, cursor, item.Count);
                    cursor += indexData[i].Count;
                }
            }

            if (cursor > length)
            {
                length = cursor;
            }
        }

        public void WriteDict(IEnumerable<KeyValuePair<int, double[]>> dict)
        {
            foreach (var entry in dict)
            {
                // Operands
                foreach (var operand in entry.Value)
                {
                    WriteNumber(operand);
                }

                var op = entry.Key;

                if (op > 255)
                {
                    // Two byte operator
                    WriteCard16(op);
                }
                else
                {
                    // One byte operator
                    WriteCard8(op);
                }
            }
        }

        public int GetOffSize(int offset)
        {
            if (offset < (1 << 8))
            {
                return 1;
            }

            if (offset < (1 << 16))
            {
                return 2;
            }

            if (offset < (1 << 24))
            {
                return 3;
            }

            return 4;
        }

        public void WriteOffset(int offSize, int offset)
        {
            EnsureCapacity(cursor + offSize);

            switch (offSize)
            {
                case 1:
                    buffer[cursor] = (byte)offset;
                    break;

                case 2:
                    buffer[cursor + 0] = (byte)(offset >> 8);
                    buffer[cursor + 1] = (byte)(offset);
                    break;

                case 3:
                    buffer[cursor + 0] = (byte)(offset >> 16);
                    buffer[cursor + 1] = (byte)(offset >> 8);
                    buffer[cursor + 2] = (byte)(offset);
                    break;

                case 4:
                    buffer[cursor + 0] = (byte)(offset >> 24);
                    buffer[cursor + 1] = (byte)(offset >> 16);
                    buffer[cursor + 2] = (byte)(offset >> 8);
                    buffer[cursor + 3] = (byte)(offset);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(offSize), "Invalid offSize. Only values in the range 1-4 are allowed.");
            }

            cursor += offSize;

            if (cursor > length)
            {
                length = cursor;
            }
        }

        public void WriteOffSize(int offSize)
        {
            if (offSize < 0 || offSize > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(offSize), nameof(offSize) + " must be in the range 0-4.");
            }

            WriteCard8(offSize);
        }

        public void WriteCard8(int value)
        {
            EnsureCapacity(cursor + 1);

            buffer[cursor++] = (byte)value;

            if (cursor > length)
            {
                length = cursor;
            }
        }

        public void WriteCard16(int value)
        {
            EnsureCapacity(cursor + 2);

            buffer[cursor++] = (byte)(value >> 8);
            buffer[cursor++] = (byte)value;

            if (cursor > length)
            {
                length = cursor;
            }
        }

        public void WriteSID(int value) => WriteCard16(value);

        public void WriteNumber(double value)
        {
            if (value >= int.MinValue && value <= int.MaxValue &&
                value == Math.Truncate(value))
            {
                WriteInteger((int)value);
            }
            else
            {
                WriteReal(value);
            }
        }

        public void WriteInteger(int value)
        {
            if (value >= -107 && value <= 107)
            {
                EnsureCapacity(cursor + 1);

                buffer[cursor++] = (byte)(value + 139);
            }
            else if (value >= 108 && value <= 1131)
            {
                EnsureCapacity(cursor + 2);

                value -= 108;

                buffer[cursor + 0] = (byte)((value >> 8) + 247);
                buffer[cursor + 1] = (byte)(value);

                cursor += 2;
            }
            else if (value >= -1131 && value <= -108)
            {
                EnsureCapacity(cursor + 2);

                value = -value - 108;

                buffer[cursor + 0] = (byte)((value >> 8) + 251);
                buffer[cursor + 1] = (byte)(value);

                cursor += 2;
            }
            else if (value >= -32768 && value <= 32767)
            {
                EnsureCapacity(cursor + 3);

                buffer[cursor + 0] = 28;
                buffer[cursor + 1] = (byte)(value >> 8);
                buffer[cursor + 2] = (byte)(value);

                cursor += 3;
            }
            else
            {
                EnsureCapacity(cursor + 5);

                buffer[cursor + 0] = 29;
                buffer[cursor + 1] = (byte)(value >> 24);
                buffer[cursor + 2] = (byte)(value >> 16);
                buffer[cursor + 3] = (byte)(value >> 8);
                buffer[cursor + 4] = (byte)(value);

                cursor += 5;
            }

            if (cursor > length)
            {
                length = cursor;
            }
        }

        public void WriteBytes(ArraySegment<byte> data)
        {
            if (data.Array != null)
            {
                EnsureCapacity(cursor + data.Count);

                Buffer.BlockCopy(data.Array, data.Offset, buffer, cursor, data.Count);

                cursor += data.Count;

                if (cursor > length)
                {
                    length = cursor;
                }
            }
        }

        public void WriteReal(double value)
        {
            var str = value.ToString("G", CultureInfo.InvariantCulture);

            EnsureCapacity(cursor + str.Length / 2 + 2);

            buffer[cursor++] = 30;

            var incompleteByte = -1;

            for (var i = 0; i < str.Length; i++)
            {
                var ch = str[i];
                int nibble;

                if (ch >= '0' && ch <= '9')
                {
                    nibble = ch - '0';
                }
                else if (ch == 'E')
                {
                    if (i + 1 < str.Length && str[i + 1] == '-')
                    {
                        nibble = 0xc;
                        i++;
                    }
                    else
                    {
                        nibble = 0xb;
                    }
                }
                else if (ch == '.')
                {
                    nibble = 0xa;
                }
                else if (ch == '-')
                {
                    nibble = 0xe;
                }
                else if (ch == 'f')
                {
                    nibble = 0xf;
                }
                else
                {
                    continue;
                }

                if (incompleteByte == -1)
                {
                    incompleteByte = nibble << 4;
                }
                else
                {
                    buffer[cursor++] = (byte)(incompleteByte | nibble);
                    incompleteByte = -1;
                }
            }

            // Terminate real
            if (incompleteByte == -1)
            {
                buffer[cursor++] = 0xff;
            }
            else
            {
                buffer[cursor++] = (byte)(incompleteByte | 0x0f);
            }

            if (cursor > length)
            {
                length = cursor;
            }
        }
    }
}
