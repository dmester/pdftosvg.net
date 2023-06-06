// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Fax
{
    [DebuggerDisplay("{DebugView,nq}")]
    internal class FaxReader
    {
        private readonly byte[] buffer;
        private readonly int offset;
        private readonly int count;

        private int byteValue;
        private FaxReaderCursor cursor;

        public FaxReader(byte[] buffer, int offset, int count)
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

        public FaxReaderCursor Cursor
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

        public int ReadBit() => ReadBits(1);

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

        public bool TryReadCode(FaxCodeTable codeTable, out int result)
        {
            var code = 0b1;

            for (var codeLength = 1; codeLength <= codeTable.MaxCodeLength; codeLength++)
            {
                var bit = ReadBit();
                if (bit < 0)
                {
                    break;
                }

                code = (code << 1) | bit;

                if (codeTable.TryGetValue(code, out result))
                {
                    return true;
                }
            }

            result = default!;
            return false;
        }

        public bool TryReadRunLength(FaxCodeTable codeTable, out int runLength)
        {
            runLength = 0;

            while (true)
            {
                if (TryReadCode(codeTable, out var iterationRunLength))
                {
                    runLength += iterationRunLength;

                    if (iterationRunLength <= FaxCodes.MaxTerminatingRunLength)
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
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
