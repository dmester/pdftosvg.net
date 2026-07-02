// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private int byteCursor;

        private ulong bitIsMarker;
        private ulong bitBuffer;
        private int bitBufferSize;

        private const int MaxBitBufferSize = 64;

        public JpegImageDataReader(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            this.buffer = buffer;
            this.count = buffer.Length;
        }

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

        public JpegImageDataReader(ArraySegment<byte> data)
        {
            if (data.Array == null)
            {
                throw new ArgumentNullException(nameof(data.Array));
            }

            this.buffer = data.Array;
            this.offset = data.Offset;
            this.count = data.Count;
        }

        public bool ReadRestartMarker()
        {
            // Byte align
            bitBufferSize &= ~7;

            var nextByte = ReadBits(8);

            var cursorAtMarker = ((bitIsMarker >> bitBufferSize) & 1) == 1;
            if (!cursorAtMarker)
            {
                return false;
            }

            if (nextByte < 0xd0 || nextByte > 0xd7)
            {
                return false;
            }

            // Restart marker found
            return true;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void PopulateBuffer()
        {
            bitIsMarker = 0;
            bitBuffer = 0;
            bitBufferSize = 0;

            while (byteCursor < count && bitBufferSize < MaxBitBufferSize)
            {
                var byteValue = buffer[offset + byteCursor++];
                var escapedBitsValue = 0UL;

                if (byteValue == 0xff)
                {
                    // Byte stuffing
                    if (byteCursor >= count)
                    {
                        break;
                    }

                    byteValue = buffer[offset + byteCursor++];

                    if (byteValue == 0x00)
                    {
                        // Stuffed 0xff
                        byteValue = 0xff;
                    }
                    else
                    {
                        // Other marker
                        escapedBitsValue = 0xff;
                    }
                }

                bitBuffer = (bitBuffer << 8) | byteValue;
                bitIsMarker = (bitIsMarker << 8) | escapedBitsValue;
                bitBufferSize += 8;
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public int ReadBits(int bitCount)
        {
            var result = 0UL;

            while (bitCount > 0)
            {
                if (bitBufferSize == 0)
                {
                    PopulateBuffer();

                    if (bitBufferSize == 0)
                    {
                        return -1;
                    }
                }

                var iterationBitCount = Math.Min(bitBufferSize, bitCount);
                result <<= iterationBitCount;

                var iterationBitMask = (1UL << iterationBitCount) - 1;
                result |= (bitBuffer >> (bitBufferSize - iterationBitCount)) & iterationBitMask;

                bitBufferSize -= iterationBitCount;
                bitCount -= iterationBitCount;
            }

            return (int)result;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public int ReadBit()
        {
            if (bitBufferSize == 0)
            {
                PopulateBuffer();

                if (bitBufferSize == 0)
                {
                    return -1;
                }
            }

            return (int)((bitBuffer >> (bitBufferSize-- - 1)) & 1);
        }
    }
}
