// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx.IO;
using PdfToSvg.Imaging.Jpx.Packets;
using System;

namespace PdfToSvg.Tests.Images.Jpx.Packets
{
    public class JpxPackedPacketHeaderBufferTests
    {
        private static JpxPackedPacketHeaderSegment Segment(int index, params byte[] data)
        {
            return new JpxPackedPacketHeaderSegment { Index = index, Data = new ArraySegment<byte>(data) };
        }

        private static byte[] ReadAll(JpxDataReader reader)
        {
            Assert.IsNotNull(reader);

            var result = new byte[reader!.Length - reader.Cursor];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = reader.ReadByte();
            }
            return result;
        }

        [Test]
        public void GetTileHeaderReader_SplitsNppmEntriesPerTilePart()
        {
            var buffer = new JpxPackedPacketHeaderBuffer();
            buffer.AddMainHeaderSegment(Segment(0,
                0, 0, 0, 3, 0xA0, 0xA1, 0xA2,   // Nppm = 3: first tile-part (tile 0)
                0, 0, 0, 2, 0xB0, 0xB1));       // Nppm = 2: second tile-part (tile 1)

            buffer.RegisterTilePart(0);
            buffer.RegisterTilePart(1);

            Assert.AreEqual(new byte[] { 0xA0, 0xA1, 0xA2 }, ReadAll(buffer.GetTileHeaderReader(0)));
            Assert.AreEqual(new byte[] { 0xB0, 0xB1 }, ReadAll(buffer.GetTileHeaderReader(1)));
        }

        [Test]
        public void GetTileHeaderReader_NppmSpansSegmentBoundary()
        {
            // ITU-T T.800 (06/2019) Section A.7.4: the Ippm series of one Nppm block may continue
            // into the next PPM marker segment.
            var buffer = new JpxPackedPacketHeaderBuffer();
            buffer.AddMainHeaderSegment(Segment(0, 0, 0, 0, 6, 0xA0, 0xA1));
            buffer.AddMainHeaderSegment(Segment(1, 0xA2, 0xA3, 0xA4, 0xA5, 0, 0, 0, 1, 0xB0));

            buffer.RegisterTilePart(0);
            buffer.RegisterTilePart(0);

            Assert.AreEqual(
                new byte[] { 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xB0 },
                ReadAll(buffer.GetTileHeaderReader(0)));
        }

        [Test]
        public void GetTileHeaderReader_OutOfOrderZppmIsReordered()
        {
            var buffer = new JpxPackedPacketHeaderBuffer();
            buffer.AddMainHeaderSegment(Segment(1, 0xA1));
            buffer.AddMainHeaderSegment(Segment(0, 0, 0, 0, 2, 0xA0));

            buffer.RegisterTilePart(0);

            Assert.AreEqual(new byte[] { 0xA0, 0xA1 }, ReadAll(buffer.GetTileHeaderReader(0)));
        }

        [Test]
        public void GetTileHeaderReader_TruncatedNppmBlock()
        {
            // Nppm declares 10 bytes but only 3 remain; the entry is clamped best-effort.
            var buffer = new JpxPackedPacketHeaderBuffer();
            buffer.AddMainHeaderSegment(Segment(0, 0, 0, 0, 10, 1, 2, 3));

            buffer.RegisterTilePart(0);

            Assert.AreEqual(new byte[] { 1, 2, 3 }, ReadAll(buffer.GetTileHeaderReader(0)));
        }

        [Test]
        public void GetTileHeaderReader_TruncatedNppmLengthField()
        {
            // Three trailing bytes cannot hold the 4-byte Nppm field and are ignored.
            var buffer = new JpxPackedPacketHeaderBuffer();
            buffer.AddMainHeaderSegment(Segment(0, 0, 0, 0, 1, 0xA0, 0, 0, 0));

            buffer.RegisterTilePart(0);
            buffer.RegisterTilePart(1);

            Assert.AreEqual(new byte[] { 0xA0 }, ReadAll(buffer.GetTileHeaderReader(0)));

            // The second tile-part has no PPM entry left; it gets an empty header stream rather
            // than null, since the packet headers are not in the bit stream.
            var second = buffer.GetTileHeaderReader(1);
            Assert.IsNotNull(second);
            Assert.AreEqual(0, second!.Length);
        }

        [Test]
        public void GetTileHeaderReader_PptTakesPrecedenceOverPpm()
        {
            // ITU-T T.800 (06/2019) Section A.7.5: PPM and PPT must not be combined; the PPT data
            // wins best-effort.
            var buffer = new JpxPackedPacketHeaderBuffer();
            buffer.AddMainHeaderSegment(Segment(0, 0, 0, 0, 1, 0xAA));

            buffer.RegisterTilePart(0);
            buffer.AddTileHeaderSegment(0, Segment(0, 0xBB));

            Assert.AreEqual(new byte[] { 0xBB }, ReadAll(buffer.GetTileHeaderReader(0)));
        }

        [Test]
        public void GetTileHeaderReader_ConcatenatesPptInZpptOrder()
        {
            var buffer = new JpxPackedPacketHeaderBuffer();
            buffer.RegisterTilePart(0);
            buffer.AddTileHeaderSegment(0, Segment(1, 0xA1));
            buffer.AddTileHeaderSegment(0, Segment(0, 0xA0));

            Assert.AreEqual(new byte[] { 0xA0, 0xA1 }, ReadAll(buffer.GetTileHeaderReader(0)));
        }

        [Test]
        public void GetTileHeaderReader_ZpptOrderIsScopedToTilePart()
        {
            // ITU-T T.800 (06/2019) Section A.7.5: Zppt indexes PPT segments within the current
            // tile-part header, so a later tile-part restarting at Zppt = 0 must not be sorted
            // before the earlier tile-part's data.
            var buffer = new JpxPackedPacketHeaderBuffer();
            buffer.RegisterTilePart(0);
            buffer.AddTileHeaderSegment(0, Segment(0, 0xA0));
            buffer.AddTileHeaderSegment(0, Segment(1, 0xA1));
            buffer.RegisterTilePart(0);
            buffer.AddTileHeaderSegment(0, Segment(0, 0xA2));

            Assert.AreEqual(new byte[] { 0xA0, 0xA1, 0xA2 }, ReadAll(buffer.GetTileHeaderReader(0)));
        }

        [Test]
        public void GetTileHeaderReader_NoPackedHeaders()
        {
            var buffer = new JpxPackedPacketHeaderBuffer();
            buffer.RegisterTilePart(0);

            Assert.IsNull(buffer.GetTileHeaderReader(0));
        }

        [Test]
        public void GetTileHeaderReader_ReturnsCachedReader()
        {
            // The reader instance is cached so consecutive packets consume header bytes
            // sequentially.
            var buffer = new JpxPackedPacketHeaderBuffer();
            buffer.RegisterTilePart(0);
            buffer.AddTileHeaderSegment(0, Segment(0, 1, 2, 3));

            var first = buffer.GetTileHeaderReader(0);
            Assert.IsNotNull(first);
            first!.ReadByte();

            var second = buffer.GetTileHeaderReader(0);
            Assert.AreSame(first, second);
            Assert.AreEqual(1, second!.Cursor);
        }
    }
}
