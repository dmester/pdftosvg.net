// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public int Length => count;

        public bool EndOfInput => cursor.Cursor >= count;

        public int ReadBit() => ReadBits(1, throwOnError: false);

        public int ReadBitOrThrow() => ReadBits(1, throwOnError: true);

        public long ReadLongBits(int bitCount) => ReadLongBits(bitCount, throwOnError: false);

        public long ReadLongBitsOrThrow(int bitCount) => ReadLongBits(bitCount, throwOnError: true);

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private long ReadLongBits(int bitCount, bool throwOnError)
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

                            return throwOnError
                                ? throw new EndOfStreamException()
                                : -1;
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
                return throwOnError
                    ? throw new EndOfStreamException()
                    : -1;
            }
        }

        public int ReadBytes(int byteCount) => ReadBytes(byteCount, throwOnError: false);
        public int ReadBytesOrThrow(int byteCount) => ReadBytes(byteCount, throwOnError: true);

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private int ReadBytes(int byteCount, bool throwOnError)
        {
            if (byteCount < 0 || byteCount > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            }

            if (cursor.BitCursor == 0)
            {
                var result = 0;

                while (byteCount-- > 0)
                {
                    if (cursor.Cursor >= count)
                    {
                        return throwOnError
                            ? throw new EndOfStreamException()
                            : -1;
                    }

                    result = (result << 8) | buffer[offset + cursor.Cursor];

                    cursor.Cursor++;
                }

                return result;
            }
            else
            {
                return ReadBits(byteCount * 8, throwOnError);
            }
        }

        public int ReadByte() => ReadByte(throwOnError: false);
        public int ReadByteOrThrow() => ReadByte(throwOnError: true);

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private int ReadByte(bool throwOnError)
        {
            if (cursor.BitCursor == 0)
            {
                if (cursor.Cursor >= count)
                {
                    return throwOnError
                        ? throw new EndOfStreamException()
                        : -1;
                }

                var result = buffer[offset + cursor.Cursor];

                cursor.Cursor++;

                return result;
            }
            else
            {
                return ReadBits(8, throwOnError);
            }
        }

        public int ReadBits(int bitCount) => ReadBits(bitCount, throwOnError: false);
        public int ReadBitsOrThrow(int bitCount) => ReadBits(bitCount, throwOnError: true);

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private int ReadBits(int bitCount, bool throwOnError)
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

                            return throwOnError
                                ? throw new EndOfStreamException()
                                : -1;
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
                return throwOnError
                    ? throw new EndOfStreamException()
                    : -1;
            }
        }

        public void SkipBytes(int byteCount) => SkipBits(byteCount * 8);
        public void SkipBits(int bitCount)
        {
            if (bitCount <= 0)
            {
                return;
            }

            // Initial partial byte
            if (cursor.BitCursor > 0)
            {
                var remainingBits = 8 - cursor.BitCursor;

                if (remainingBits < bitCount)
                {
                    bitCount -= remainingBits;
                    cursor.BitCursor = 0;
                    cursor.Cursor++;
                }
                else
                {
                    cursor.BitCursor += bitCount;
                    bitCount = 0;
                }
            }

            if (bitCount <= 0)
            {
                return;
            }

            // Full bytes
            var fullBytes = bitCount / 8;
            cursor.Cursor += fullBytes;
            bitCount -= fullBytes * 8;

            if (bitCount <= 0)
            {
                return;
            }

            // Last partial byte
            if (cursor.Cursor < count)
            {
                cursor.BitCursor += bitCount;
                byteValue = buffer[offset + cursor.Cursor];
            }
            else
            {
                byteValue = -1;
            }
        }

        /// <summary>
        /// Creates a subreader for the specified number of following bytes and consumes those bytes in the parent reader.
        /// </summary>
        /// <param name="byteCount">Maximum number of bytes to include in the subreader. If there are not enougth bytes, the subreader will contain fewer bytes.</param>
        /// <exception cref="InvalidOperationException">Called when the parent reader is not aligned to a byte.</exception>
        public VariableBitReader CreateSubReader(int byteCount)
        {
            if (cursor.BitCursor != 0)
            {
                throw new InvalidOperationException("Cannot create a subreader unless the cursor is byte aligned");
            }

            var subReaderOffset = offset + cursor.Cursor;
            var subReaderCount = Math.Min(byteCount, count - cursor.Cursor);

            cursor.Cursor += subReaderCount;

            return new VariableBitReader(buffer, subReaderOffset, subReaderCount);
        }

        public void GetBuffer(out byte[] buffer, out int offset, out int count)
        {
            buffer = this.buffer;
            offset = this.offset;
            count = this.count;
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
