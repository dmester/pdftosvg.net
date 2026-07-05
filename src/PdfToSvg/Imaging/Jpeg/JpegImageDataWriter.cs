// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace PdfToSvg.Imaging.Jpeg
{
    internal class JpegImageDataWriter : IDisposable
    {
        // The writer adds incoming bits to `accumulator`.
        // `accumulator` is flushed to `buffer`.
        // `buffer` is flushed to `stream`.

        private const int BufferSize = 4096;

        private readonly MemoryStream stream;

        private readonly byte[] buffer = new byte[BufferSize];
        private int bufferByteCount;

        private ulong accumulator; // MSB order
        private int accumulatorBitCount;

        private int restartMarkerCount;

        public JpegImageDataWriter(MemoryStream stream)
        {
            this.stream = stream;
        }

        private void FlushBuffer()
        {
            if (bufferByteCount > 0)
            {
                stream.Write(buffer, 0, bufferByteCount);
                bufferByteCount = 0;
            }
        }

        private void Emit(byte value)
        {
            // Ensure room for the byte plus a potential stuffing byte.
            if (bufferByteCount + 2 > buffer.Length)
            {
                FlushBuffer();
            }

            buffer[bufferByteCount++] = value;

            if (value == 0xff)
            {
                // Byte stuffing
                buffer[bufferByteCount++] = 0x00;
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private void FlushWholeBytes()
        {
            while (accumulatorBitCount >= 8)
            {
                Emit((byte)(accumulator >> 56));
                accumulator <<= 8;
                accumulatorBitCount -= 8;
            }
        }

        private void FlushPendingBits()
        {
            if (accumulatorBitCount > 0)
            {
                // Pad the remaining low bits of the last byte with 1s.
                Emit((byte)((accumulator >> 56) | (uint)((1 << (8 - accumulatorBitCount)) - 1)));

                accumulator = 0;
                accumulatorBitCount = 0;
            }
        }

        public void WriteCode(JpegHuffmanCode code) => WriteBits((uint)code.Code, code.CodeLength);

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void WriteBits(ulong value, int bitCount)
        {
            if (bitCount == 0)
            {
                return;
            }

            // Only the low bitCount bits of value are appended, MSB-first. heldBitCount is always
            // < 8 here, so bitCount can be up to 57 without overflowing the accumulator.
            var mask = bitCount >= 64 ? ulong.MaxValue : (1u << bitCount) - 1;
            accumulator |= (ulong)(value & mask) << (64 - accumulatorBitCount - bitCount);
            accumulatorBitCount += bitCount;

            FlushWholeBytes();
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void WriteSymbol(JpegHuffmanCode code, int ssss, int value)
        {
            // F.1.2.1.1
            var diffMask = (1 << ssss) - 1;
            var diff = value >= 0 ? value : value + diffMask;

            var bits = ((ulong)code.Code << ssss) | (uint)(diff & diffMask);
            var bitCount = code.CodeLength + ssss;
            WriteBits(bits, bitCount);
        }

        public void WriteValue(int ssss, int value)
        {
            if (ssss == 0)
            {
                return;
            }

            // F.1.2.1.1
            var diffMask = (1 << ssss) - 1;
            var diff = value >= 0 ? value : value + diffMask;

            WriteBits((uint)(diff & diffMask), ssss);
        }

        public void WriteRestartMarker()
        {
            FlushPendingBits();

            if (bufferByteCount + 2 > buffer.Length)
            {
                FlushBuffer();
            }

            buffer[bufferByteCount++] = 0xff;
            buffer[bufferByteCount++] = (byte)(0xd0 + (restartMarkerCount & 7));

            restartMarkerCount++;
        }

        public void Dispose()
        {
            FlushPendingBits();
            FlushBuffer();
        }
    }
}
