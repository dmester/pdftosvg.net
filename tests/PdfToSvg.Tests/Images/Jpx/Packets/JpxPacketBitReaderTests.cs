// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx.IO;
using PdfToSvg.Imaging.Jpx.Packets;
using System.IO;

namespace PdfToSvg.Tests.Images.Jpx.Packets
{
    internal class JpxPacketBitReaderTests
    {
        private static JpxPacketBitReader Reader(params byte[] data)
        {
            var dataReader = new JpxDataReader(data);
            return new JpxPacketBitReader(dataReader);
        }

        private static int[] ReadBits(ref JpxPacketBitReader reader, int count)
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
            Assert.AreEqual(new[] { 1, 0, 1, 1, 0, 1, 0, 0 }, ReadBits(ref reader, 8));
        }

        [Test]
        public void ReadBit_StuffedBitDiscardedAfterFf()
        {
            var reader = Reader(0xFF, 0x80);
            Assert.AreEqual(
                new[] { 1, 1, 1, 1, 1, 1, 1, 1, /* stuffed bit dropped */ 0, 0, 0, 0, 0, 0, 0 },
                ReadBits(ref reader, 15));
        }

        [Test]
        public void ReadBit_StuffedBitDiscardedWhenSecondByteAlsoFf()
        {
            // 0xFF, 0xFF: after the first byte's eight 1-bits, the stuffed MSB of the second byte
            // is dropped, leaving its seven data bits (all 1s).
            var reader = Reader(0xFF, 0xFF, 0x00);
            Assert.AreEqual(
                new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
                ReadBits(ref reader, 16));
        }

        [Test]
        public void ReadBits_AssemblesBitsMsbFirst()
        {
            // 0xB4 = 1011 0100 => first three bits 101 = 5, next five 10100 = 20.
            var reader = Reader(0xB4);
            Assert.AreEqual(5, reader.ReadBits(3));
            Assert.AreEqual(20, reader.ReadBits(5));
        }

        [Test]
        public void Position_TracksBufferedByte()
        {
            // Position reports the offset of the byte currently being read; it does not advance
            // until the buffered byte is exhausted.
            var reader = Reader(0xB4, 0x2A);
            Assert.AreEqual(0, reader.Cursor);

            ReadBits(ref reader, 7);
            Assert.AreEqual(0, reader.Cursor);

            ReadBits(ref reader, 1);
            Assert.AreEqual(1, reader.Cursor);
        }

        [Test]
        public void ReadBit_ThrowsAtEndOfStream()
        {
            var reader = Reader(0xB4);
            ReadBits(ref reader, 8);
            Assert.Throws<EndOfStreamException>(() => reader.ReadBit());
        }
    }
}
