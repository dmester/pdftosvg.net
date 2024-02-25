// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PdfToSvg.IO
{
    [DebuggerDisplay("{DebugView,nq}")]
    internal class VariableBitReader
    {
        private readonly byte[] buffer;
        private readonly int offset;
        private readonly int count;

        private int byteValue;
        private VariableBitReaderCursor cursor;

        public VariableBitReader(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (count < 0 || offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            this.buffer = buffer;
            this.offset = offset;
            this.count = count;
        }

        public VariableBitReaderCursor Cursor
        {
            get => cursor;
            set
            {
                cursor = value;

                if (value.BitCursor > 0)
                {
                    byteValue = buffer[offset + value.Cursor];
                }
            }
        }

        public bool EndOfInput => cursor.Cursor >= count;

        public int ReadBit() => ReadBits(1);

        public long ReadLongBits(int bitCount)
        {
            if (cursor.Cursor < count)
            {
                var result = 0L;

                while (bitCount > 0)
                {
                    if (cursor.BitCursor == 0)
                    {
                        if (cursor.Cursor >= count)
                        {
                            byteValue = -1;
                            return -1;
                        }

                        byteValue = buffer[offset + cursor.Cursor];
                    }

                    var iterationBitCount = Math.Min(8 - cursor.BitCursor, bitCount);
                    var iterationBitMask = (1L << iterationBitCount) - 1;

                    result <<= iterationBitCount;
                    result |= (byteValue >> (8 - cursor.BitCursor - iterationBitCount)) & iterationBitMask;

                    cursor.BitCursor += iterationBitCount;
                    bitCount -= iterationBitCount;

                    if (cursor.BitCursor >= 8)
                    {
                        cursor.BitCursor = 0;
                        cursor.Cursor++;
                    }
                }

                return result;
            }
            else
            {
                return -1;
            }
        }

        public int ReadBits(int bitCount)
        {
            if (cursor.Cursor < count)
            {
                var result = 0;

                while (bitCount > 0)
                {
                    if (cursor.BitCursor == 0)
                    {
                        if (cursor.Cursor >= count)
                        {
                            byteValue = -1;
                            return -1;
                        }

                        byteValue = buffer[offset + cursor.Cursor];
                    }

                    var iterationBitCount = Math.Min(8 - cursor.BitCursor, bitCount);
                    var iterationBitMask = (1 << iterationBitCount) - 1;

                    result <<= iterationBitCount;
                    result |= (byteValue >> (8 - cursor.BitCursor - iterationBitCount)) & iterationBitMask;

                    cursor.BitCursor += iterationBitCount;
                    bitCount -= iterationBitCount;

                    if (cursor.BitCursor >= 8)
                    {
                        cursor.BitCursor = 0;
                        cursor.Cursor++;
                    }
                }

                return result;
            }
            else
            {
                return -1;
            }
        }

        public void AlignByte()
        {
            if (cursor.BitCursor > 0)
            {
                cursor.BitCursor = 0;
                cursor.Cursor++;
            }
        }

#if DEBUG
        private string DebugView => BitReaderUtils.FormatDebugView(
            new ArraySegment<byte>(buffer, offset, count), cursor.Cursor, cursor.BitCursor);
#endif
    }
}
