// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Fonts.CompactFonts
{
    internal class CompactFontReader
    {
        private byte[] data;
        private int cursor;
        private StringBuilder sb = new StringBuilder();

        public CompactFontReader(byte[] data)
        {
            this.data = data;
        }

        public int Position
        {
            get => cursor;
            set => cursor = value <= data.Length && value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(Position));
        }

        public int Length => data.Length;

        public CompactFontHeader ReadHeader()
        {
            var header = new CompactFontHeader();

            header.Major = ReadCard8();
            header.Minor = ReadCard8();
            header.HdrSize = ReadCard8();
            header.OffSize = ReadOffSize();

            return header;
        }

        private int[] ReadIndexData()
        {
            var count = ReadCard16();

            // Empty index should be 2 byte
            if (count == 0)
            {
                return new int[] { 1 };
            }
            else
            {
                var offSize = ReadOffSize();
                var offset = new int[count + 1];

                for (var i = 0; i < offset.Length; i++)
                {
                    offset[i] = ReadOffset(offSize);
                }

                return offset;
            }
        }

        public int[] ReadIndex()
        {
            var offset = ReadIndexData();
            var delta = cursor - 1;

            for (var i = 0; i < offset.Length; i++)
            {
                offset[i] += delta;
            }

            cursor = offset.Last();
            return offset;
        }

        public string[] ReadStrings(int[] indexData)
        {
            var result = new string[indexData.Length - 1];

            for (var i = 0; i + 1 < indexData.Length; i++)
            {
                result[i] = Encoding.ASCII.GetString(data,
                    index: indexData[i],
                    count: indexData[i + 1] - indexData[i]);
            }

            return result;
        }

        public int ReadCard8()
        {
            if (cursor >= data.Length)
            {
                throw new EndOfStreamException();
            }

            return data[cursor++];
        }

        public int ReadSID() => ReadCard16();

        public int ReadOffSize()
        {
            if (cursor >= data.Length)
            {
                throw new EndOfStreamException();
            }

            var result = data[cursor++];
            if (result < 0 || result > 4)
            {
                throw new CompactFontException("Invalid OffSize.");
            }

            return result;
        }

        public int ReadOffset(int offSize)
        {
            if (cursor + offSize > data.Length)
            {
                throw new EndOfStreamException();
            }

            int result;

            switch (offSize)
            {
                case 1:
                    result = data[cursor];
                    break;

                case 2:
                    result =
                        (data[cursor] << 8) |
                        data[cursor + 1];
                    break;

                case 3:
                    result =
                        (data[cursor] << 16) |
                        (data[cursor + 1] << 8) |
                        data[cursor + 2];
                    break;

                case 4:
                    result =
                        (data[cursor] << 24) |
                        (data[cursor + 1] << 16) |
                        (data[cursor + 2] << 8) |
                        data[cursor + 3];
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(offSize), "Invalid offSize. Only values in the range 1-4 are allowed.");
            }

            cursor += offSize;

            return result;
        }

        public int ReadCard16()
        {
            if (cursor + 2 > data.Length)
            {
                throw new EndOfStreamException();
            }

            var result = (data[cursor] << 8) | data[cursor + 1];
            cursor += 2;
            return result;
        }

        public Dictionary<int, double[]> ReadDict(int length)
        {
            var result = new Dictionary<int, double[]>();
            var startCursor = cursor;

            var operands = new List<double>();

            while (cursor < startCursor + length)
            {
                var b0 = data[cursor];

                if (b0 <= 21)
                {
                    // Operator
                    var op = (int)b0;

                    cursor++;

                    if (b0 == 12)
                    {
                        op = (b0 << 8) | data[cursor++];
                    }

                    result[op] = operands.ToArray();
                    operands.Clear();
                }

                else if (b0 >= 32 && b0 <= 254 ||
                    b0 == 28 ||
                    b0 == 29)
                {
                    operands.Add(ReadInteger());
                }

                else if (b0 == 30)
                {
                    operands.Add(ReadReal());
                }

                else
                {
                    throw new CompactFontException("Unexpected byte in DICT.");
                }
            }

            cursor = startCursor + length;
            return result;
        }

        public double ReadReal()
        {
            // CFF spec, Table 5

            if (cursor + 1 >= data.Length)
            {
                // Min length is 2 characters
                throw new EndOfStreamException();
            }

            var localCursor = cursor;

            if (data[localCursor++] != 30)
            {
                throw new CompactFontException("Not a real value.");
            }

            sb.Clear();

            while (true)
            {
                if (localCursor >= data.Length)
                {
                    throw new EndOfStreamException();
                }

                if (localCursor - cursor > 200)
                {
                    throw new CompactFontException("Invalid real value. The value was too long.");
                }

                var byteValue = data[localCursor++];

                for (var shift = 4; shift >= 0; shift -= 4)
                {
                    var nibble = (byteValue >> shift) & 0xf;

                    if (nibble < 10)
                    {
                        sb.Append((char)('0' + nibble));
                    }
                    else if (nibble == 0xa)
                    {
                        sb.Append('.');
                    }
                    else if (nibble == 0xb)
                    {
                        sb.Append('E');
                    }
                    else if (nibble == 0xc)
                    {
                        sb.Append("E-");
                    }
                    else if (nibble == 0xd)
                    {
                        // Reserved
                    }
                    else if (nibble == 0xe)
                    {
                        sb.Append('-');
                    }
                    else if (nibble == 0xf)
                    {
                        // End of number
                        if (double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                        {
                            cursor = localCursor;
                            return result;
                        }
                        else
                        {
                            throw new CompactFontException("Invalid real value '" + sb + "'.");
                        }
                    }
                }
            }
        }

        public int ReadInteger()
        {
            // CFF spec, Table 3
            if (cursor >= data.Length)
            {
                throw new EndOfStreamException();
            }

            var b0 = data[cursor];
            int result;

            if (b0 >= 32 && b0 <= 246)
            {
                result = b0 - 139;
                cursor += 1;
            }
            else if (b0 >= 247 && b0 <= 250)
            {
                if (cursor + 1 >= data.Length)
                {
                    throw new EndOfStreamException();
                }

                result = ((b0 - 247) << 8) + data[cursor + 1] + 108;
                cursor += 2;
            }
            else if (b0 >= 251 && b0 <= 254)
            {
                if (cursor + 1 >= data.Length)
                {
                    throw new EndOfStreamException();
                }

                result = -((b0 - 251) << 8) - data[cursor + 1] - 108;
                cursor += 2;
            }
            else if (b0 == 28)
            {
                if (cursor + 2 >= data.Length)
                {
                    throw new EndOfStreamException();
                }

                result = unchecked((short)((data[cursor + 1] << 8) | data[cursor + 2]));
                cursor += 3;
            }
            else if (b0 == 29)
            {
                if (cursor + 4 >= data.Length)
                {
                    throw new EndOfStreamException();
                }

                result =
                    (data[cursor + 1] << 24) |
                    (data[cursor + 2] << 16) |
                    (data[cursor + 3] << 8) |
                    data[cursor + 4];
                cursor += 5;
            }
            else
            {
                throw new CompactFontException("Not an integer.");
            }

            return result;
        }
    }
}
