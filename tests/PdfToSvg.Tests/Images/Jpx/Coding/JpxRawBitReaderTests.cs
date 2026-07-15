// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx.Coding;
using System;

namespace PdfToSvg.Tests.Images.Jpx.Coding
{
    internal class JpxRawBitReaderTests
    {
        private static JpxRawBitReader Reader(params byte[] data) =>
            new JpxRawBitReader(new ArraySegment<byte>(data));

        private static int[] ReadBits(JpxRawBitReader reader, int count)
        {
            var bits = new int[count];
            for (var i = 0; i < count; i++)
            {
                bits[i] = reader.ReadBit();
            }
            return bits;
        }

        [Test]
        public void ReadBit_MostSignificantBitFirst()
        {
            // 0xB4 = 1011 0100, read MSB first.
            var reader = Reader(0xB4);
            Assert.AreEqual(new[] { 1, 0, 1, 1, 0, 1, 0, 0 }, ReadBits(reader, 8));
        }

        [Test]
        public void ReadBit_SevenDataBitsAfterFf()
        {
            // ITU-T T.800 (06/2019) D.6: the byte following an 0xFF has a stuffed zero in its most significant bit, so
            // only seven of its bits carry data. 0xFF, 0x00 => eight 1-bits then seven 0-bits (the leading data bits
            // of 0x00 after the dropped stuffed bit).
            var reader = Reader(0xFF, 0x00);
            Assert.AreEqual(
                new[] { 1, 1, 1, 1, 1, 1, 1, 1, /* stuffed bit */ 0, 0, 0, 0, 0, 0, 0 },
                ReadBits(reader, 15));
        }

        [Test]
        public void ReadBit_SevenDataBitsWhenSecondByteAlsoFf()
        {
            // 0xFF, 0xFF => eight 1-bits, then seven data bits (all 1s) of the second byte.
            var reader = Reader(0xFF, 0xFF);
            Assert.AreEqual(
                new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
                ReadBits(reader, 15));
        }

        [Test]
        public void ReadBit_PadsWithFfPastEnd()
        {
            // ITU-T T.800 (06/2019) D.4.1: past the end of the segment the reader synthesizes 0xFF bytes. After a
            // non-0xFF real byte (0x00) the first pad byte carries a full eight 1-bits
            // (no stuffing because the preceding byte was not 0xFF).
            var reader = Reader(0x00);
            Assert.AreEqual(
                new[] { 0, 0, 0, 0, 0, 0, 0, 0, /* pad 0xff */ 1, 1, 1, 1, 1, 1, 1, 1 },
                ReadBits(reader, 16));
        }

        [Test]
        public void ReadBit_StuffedBitAppliesToPadByteAfterRealFf()
        {
            // A real trailing 0xFF forces the following (synthesized) pad byte to be treated as stuffed: only seven of
            // its bits are read. 0xFF real => eight 1-bits, then seven 1-bits from the 0xFF pad byte
            var reader = Reader(0xFF);
            Assert.AreEqual(
                new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
                ReadBits(reader, 15));
        }

        [Test]
        public void ReadBit_HonoursSegmentBounds()
        {
            // Only the bytes inside the ArraySegment window are treated as real data; bytes outside are past-end
            // padding (0xFF). Window covers a single 0x2A byte.
            var buffer = new byte[] { 0xFF, 0x2A, 0xFF };
            var reader = new JpxRawBitReader(new ArraySegment<byte>(buffer, 1, 1));

            // 0x2A = 0010 1010 (eight real data bits), then eight 1-bits of the first pad byte
            // (0x2A is not 0xFF, so the pad byte is not stuffed).
            Assert.AreEqual(
                new[] { 0, 0, 1, 0, 1, 0, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1 },
                ReadBits(reader, 16));
        }
    }
}
