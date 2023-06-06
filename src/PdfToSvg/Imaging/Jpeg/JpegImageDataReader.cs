// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpeg
{
#if DEBUG
    [DebuggerDisplay("{DebugView,nq}")]
#endif
    internal class JpegImageDataReader
    {
        private readonly byte[] buffer;
        private readonly int offset;
        private readonly int count;

        private int byteValue;
        private int cursor;
        private int bitCursor;

        public JpegImageDataReader(byte[] buffer, int offset, int count)
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

        public int Cursor => cursor;

        public bool ReadRestartMarker()
        {
            if (bitCursor > 0)
            {
                bitCursor = 0;
                cursor++;
            }

            if (cursor + 1 < count)
            {
                var currentByte = buffer[offset + cursor];
                var nextByte = buffer[offset + cursor + 1];

                if (currentByte == 0xff &&
                    nextByte >= 0xd0 && nextByte <= 0xd7)
                {
                    // Restart marker found
                    cursor += 2;
                    return true;
                }
            }

            return false;
        }

        private void PopulateByteValue()
        {
            if (cursor >= count)
            {
                byteValue = -1;
                return;
            }

            byteValue = buffer[offset + cursor];

            if (byteValue != 0xff)
            {
                return;
            }

            cursor++;

            if (cursor < count)
            {
                var nextByte = buffer[offset + cursor];
                if (nextByte == 0x00) // Restart termination
                {
                    // Stuffed 0xff
                    byteValue = 0xff;
                    return;
                }
            }

            cursor = count;
            byteValue = -1;
        }

        public int ReadBit() => ReadBits(1);

        public int ReadValue(int ssss)
        {
            if (ssss == 0)
            {
                return 0;
            }

            var sign = ReadBit();
            var value = ReadBits(ssss - 1);

            if (sign == 0)
            {
                var lowerBound = ((-1) << ssss) + 1;
                value += lowerBound;
            }
            else
            {
                value |= 1 << (ssss - 1);
            }

            return value;
        }

        public int ReadBits(int bitCount)
        {
            if (cursor < count)
            {
                var result = 0;

                while (bitCount > 0)
                {
                    if (bitCursor == 0)
                    {
                        PopulateByteValue();

                        if (byteValue < 0)
                        {
                            return -1;
                        }
                    }

                    var iterationBitCount = Math.Min(8 - bitCursor, bitCount);
                    var iterationBitMask = (1 << iterationBitCount) - 1;

                    result <<= iterationBitCount;
                    result |= (byteValue >> (8 - bitCursor - iterationBitCount)) & iterationBitMask;

                    bitCursor += iterationBitCount;
                    bitCount -= iterationBitCount;

                    if (bitCursor >= 8)
                    {
                        bitCursor = 0;
                        cursor++;
                    }
                }

                return result;
            }
            else
            {
                return -1;
            }
        }

        public int ReadHuffman(JpegHuffmanTable table)
        {
            var code = 0;

            for (var codeLength = 1; codeLength <= table.MaxCodeLength; codeLength++)
            {
                var bit = ReadBit();
                if (bit < 0)
                {
                    return -1;
                }

                code = (code << 1) | bit;

                if (table.TryDecode(code, codeLength, out var result))
                {
                    return result;
                }
            }

            return -1;
        }

#if DEBUG
        private string DebugView => BitReaderUtils.FormatDebugView(
            new ArraySegment<byte>(buffer, offset, count), cursor, bitCursor, byteValue);
#endif
    }
}
