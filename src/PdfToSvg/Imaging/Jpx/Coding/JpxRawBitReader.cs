// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

namespace PdfToSvg.Imaging.Jpx.Coding
{
    /// <summary>
    /// Reads raw (arithmetic-coder bypassed) bits from a code-block segment.
    /// </summary>
    /// <remarks>
    /// The reader is a struct to prevent allocating thousands of heap objects in <see cref="JpxTier1Decoder"/>.
    /// </remarks>
    internal struct JpxRawBitReader
    {
        private readonly byte[] data;
        private readonly int end;
        private int position;
        private int currentByte;
        private int bitsLeft;

        public JpxRawBitReader(ArraySegment<byte> segment)
        {
            data = segment.Array ?? throw new ArgumentException("Segment array must not be null", nameof(segment));
            position = segment.Offset;
            end = segment.Offset + segment.Count;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public int ReadBit()
        {
#if DEBUG
            if (data == null)
            {
                throw new InvalidOperationException(
                    "Calling ReadBit on a default JpxRawBitReader instance is not allowed");
            }
#endif

            // ITU-T T.800 (06/2019) Section D.6:
            // Bits are packed most-significant-bit first, and a stuffed zero bit follows every 0xFF byte.
            if (bitsLeft == 0)
            {
                var stuffedBit = currentByte == 0xff;
                var next = position < end ? data[position++] : 0xff;
                bitsLeft = stuffedBit ? 7 : 8;
                currentByte = next;
            }

            bitsLeft--;
            return (currentByte >> bitsLeft) & 1;
        }
    }
}
