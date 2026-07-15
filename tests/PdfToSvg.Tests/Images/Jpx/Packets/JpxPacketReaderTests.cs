// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx;
using PdfToSvg.Imaging.Jpx.Codestream;
using PdfToSvg.Imaging.Jpx.ImageModel;
using PdfToSvg.Imaging.Jpx.IO;
using PdfToSvg.Imaging.Jpx.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfToSvg.Tests.Images.Jpx.Packets
{
    // ITU-T T.800 (06/2019) Section B.10: packet header information coding. The packet header bits in these tests are
    // crafted by hand from the syntax of Sections B.10.3 to B.10.8; the derivations are given in comments.
    internal class JpxPacketReaderTests
    {
        [Test]
        public void ReadTilePackets_FirstInclusionAtLayerZero()
        {
            // B.10.8 header order for a packet with one 1x1-tag-tree code-block:
            //   1     non-zero length packet (B.10.3)
            //   1     inclusion tag tree, leaf value 0 < threshold 1 (B.10.4)
            //   0001  zero bit-plane tag tree, leaf value 3 (B.10.5)
            //   0     1 coding pass (Table B.4)
            //   0     Lblock unchanged (B.10.7.1)
            //   101   5 bytes; 3 + floor(log2 1) = 3 bits (Equation B-19)
            var tile = CreateTile();
            var header = new PacketBitWriter().Write("1 1 0001 0 0 101").ToArray();

            tile.Read(Concat(header, Body(5)));

            var codeBlock = tile.CodeBlock();
            Assert.AreEqual(true, codeBlock.Included);
            Assert.AreEqual(3, codeBlock.ZeroBitPlanes);
            Assert.AreEqual(1, codeBlock.CodingPasses);
            Assert.AreEqual(3, codeBlock.Lblock);
            Assert.AreEqual(1, codeBlock.Segments.Count);
            Assert.AreEqual(1, codeBlock.Segments[0].CodingPasses);
            Assert.AreEqual(false, codeBlock.Segments[0].Terminated);
            Assert.AreEqual(Body(5), codeBlock.Segments[0].Data.ToArray());

            var precinctBand = tile.Tile.Components[0].ResolutionLevels[0].Precincts[0].SubBands[0];
            Assert.IsNotNull(precinctBand.InclusionTree);
            Assert.IsNotNull(precinctBand.ZeroBitPlanesTree);
        }

        [Test]
        public void ReadTilePackets_DiscardedComponentDoesNotRetainSegmentData()
        {
            var tile = CreateTile();
            var header = new PacketBitWriter().Write("1 1 0001 0 0 101").ToArray();

            tile.CreateReader(Concat(header, Body(5)), retainCodeBlockData: false).ReadTilePackets();

            var codeBlock = tile.CodeBlock();
            Assert.AreEqual(true, codeBlock.Included);
            Assert.AreEqual(3, codeBlock.ZeroBitPlanes);
            Assert.AreEqual(1, codeBlock.CodingPasses);
            Assert.AreEqual(3, codeBlock.Lblock);
            Assert.AreEqual(0, codeBlock.Segments.Count);
        }

        [Test]
        public void ReadTilePackets_ReInclusionAppendsToOpenSegment()
        {
            // Layer 0 as in ReadTilePackets_FirstInclusionAtLayerZero (5 bytes, 1 pass).
            // Layer 1 (B.10.4: previously included code-blocks use a single bit):
            //   1     non-zero length packet
            //   1     included again
            //   10    2 coding passes (Table B.4)
            //   0     Lblock unchanged
            //   1001  9 bytes; 3 + floor(log2 2) = 4 bits
            // Without termination the codeword segment continues across packets (B.10.7.2), so
            // both contributions concatenate into one segment.
            var tile = CreateTile(layers: 2);
            var packet0 = Concat(new PacketBitWriter().Write("1 1 0001 0 0 101").ToArray(), Body(5));
            var packet1 = Concat(new PacketBitWriter().Write("1 1 10 0 1001").ToArray(), Body(9, seed: 5));

            tile.Read(Concat(packet0, packet1));

            var codeBlock = tile.CodeBlock();
            Assert.AreEqual(3, codeBlock.CodingPasses);
            Assert.AreEqual(1, codeBlock.Segments.Count);
            Assert.AreEqual(3, codeBlock.Segments[0].CodingPasses);
            Assert.AreEqual(Concat(Body(5), Body(9, seed: 5)), codeBlock.Segments[0].Data.ToArray());
        }

        [Test]
        public void ReadTilePackets_NotIncludedAgainBit()
        {
            // Layer 1 header: 1 (non-zero length), 0 (not included in this layer, B.10.4).
            var tile = CreateTile(layers: 2);
            var packet0 = Concat(new PacketBitWriter().Write("1 1 0001 0 0 101").ToArray(), Body(5));
            var packet1 = new PacketBitWriter().Write("1 0").ToArray();

            tile.Read(Concat(packet0, packet1));

            var codeBlock = tile.CodeBlock();
            Assert.AreEqual(1, codeBlock.CodingPasses);
            Assert.AreEqual(1, codeBlock.Segments.Count);
        }

        [Test]
        public void ReadTilePackets_ZeroLengthPacket()
        {
            // B.10.3: a first bit of 0 denotes a zero length packet; no code-blocks are included.
            var tile = CreateTile();

            tile.Read(new PacketBitWriter().Write("0").ToArray());

            var codeBlock = tile.CodeBlock();
            Assert.AreEqual(false, codeBlock.Included);
            Assert.AreEqual(-1, codeBlock.ZeroBitPlanes);
            Assert.AreEqual(0, codeBlock.Segments.Count);

            var precinctBand = tile.Tile.Components[0].ResolutionLevels[0].Precincts[0].SubBands[0];
            Assert.IsNull(precinctBand.InclusionTree);
            Assert.IsNull(precinctBand.ZeroBitPlanesTree);
        }

        [Test]
        public void ReadTilePackets_FirstInclusionAfterZeroLengthPacket()
        {
            // Layer 0 is a zero length packet. In layer 1, the inclusion tag tree value is the
            // first layer to which the code-block contributes (B.10.3 NOTE, B.10.4): value 1
            // codes as 01 against threshold 2.
            //   Layer 0: 0
            //   Layer 1: 1, 01 (inclusion value 1), 1 (zero bit-planes 0), 0 (1 pass),
            //            0 (Lblock), 010 (2 bytes)
            var tile = CreateTile(layers: 2);
            var packet0 = new PacketBitWriter().Write("0").ToArray();
            var packet1 = Concat(new PacketBitWriter().Write("1 01 1 0 0 010").ToArray(), Body(2));

            tile.Read(Concat(packet0, packet1));

            var codeBlock = tile.CodeBlock();
            Assert.AreEqual(true, codeBlock.Included);
            Assert.AreEqual(0, codeBlock.ZeroBitPlanes);
            Assert.AreEqual(1, codeBlock.CodingPasses);
            Assert.AreEqual(Body(2), codeBlock.Segments[0].Data.ToArray());
        }

        // ITU-T T.800 (06/2019) Table B.4 codewords across all its boundaries.
        [TestCase(1, "0")]
        [TestCase(2, "10")]
        [TestCase(3, "1100")]
        [TestCase(4, "1101")]
        [TestCase(5, "1110")]
        [TestCase(6, "1111 00000")]
        [TestCase(36, "1111 11110")]
        [TestCase(37, "1111 11111 0000000")]
        [TestCase(164, "1111 11111 1111111")]
        public void ReadTilePackets_CodingPassCounts(int expectedPasses, string codeword)
        {
            // Header: 1 (non-zero), 1 (included), 1 (zero bit-planes 0), the Table B.4 codeword,
            // 0 (Lblock unchanged), then a length of 3 + floor(log2 passes) bits with value 1.
            var lengthBits = 3;
            for (var n = expectedPasses; n > 1; n >>= 1)
            {
                lengthBits++;
            }

            var tile = CreateTile();
            var header = new PacketBitWriter()
                .Write("1 1 1")
                .Write(codeword)
                .Write("0")
                .Write(1, lengthBits)
                .ToArray();

            tile.Read(Concat(header, Body(1)));

            var codeBlock = tile.CodeBlock();
            Assert.AreEqual(expectedPasses, codeBlock.CodingPasses);
            Assert.AreEqual(1, codeBlock.Segments.Count);
            Assert.AreEqual(expectedPasses, codeBlock.Segments[0].CodingPasses);
            Assert.AreEqual(Body(1), codeBlock.Segments[0].Data.ToArray());
        }

        [Test]
        public void ReadTilePackets_LblockGrowthAndLengthDecoding()
        {
            // The example of ITU-T T.800 (06/2019) B.10.7.1 NOTE 1: successive layers contribute
            // 6, 31, 44 and 134 bytes with 1, 9, 2 and 5 coding passes:
            //   layer 0: 0 (Lblock stays 3), 110 = 6 in 3 bits
            //   layer 1: 0, 011111 = 31 in 3 + floor(log2 9) = 6 bits
            //   layer 2: 110 (Lblock 3 -> 5), 101100 = 44 in 5 + floor(log2 2) = 6 bits
            //   layer 3: 10 (Lblock 5 -> 6), 10000110 = 134 in 6 + floor(log2 5) = 8 bits
            // The coding pass codewords come from Table B.4 (9 passes: 1111 00011).
            var tile = CreateTile(layers: 4);
            var packets = Concat(
                Concat(new PacketBitWriter().Write("1 1 1 0 0 110").ToArray(), Body(6, seed: 0)),
                Concat(new PacketBitWriter().Write("1 1 111100011 0 011111").ToArray(), Body(31, seed: 6)),
                Concat(new PacketBitWriter().Write("1 1 10 110 101100").ToArray(), Body(44, seed: 37)),
                Concat(new PacketBitWriter().Write("1 1 1110 10 10000110").ToArray(), Body(134, seed: 81)));

            tile.Read(packets);

            var codeBlock = tile.CodeBlock();
            Assert.AreEqual(17, codeBlock.CodingPasses);
            Assert.AreEqual(6, codeBlock.Lblock);
            Assert.AreEqual(1, codeBlock.Segments.Count);
            Assert.AreEqual(17, codeBlock.Segments[0].CodingPasses);
            Assert.AreEqual(
                Concat(Body(6, seed: 0), Body(31, seed: 6), Body(44, seed: 37), Body(134, seed: 81)),
                codeBlock.Segments[0].Data.ToArray());
        }

        [Test]
        public void ReadTilePackets_MultipleCodewordSegmentsWithBypass()
        {
            // The example of ITU-T T.800 (06/2019) B.10.7.2 NOTE with the selective arithmetic
            // coding bypass (Table D.9). Layer 0 delivers passes 0-8 (one unterminated segment).
            // Layer 1 delivers passes 9-13: the cleanup pass 9 is terminated, the raw pass pair
            // 10-11 ends terminated at 11, the cleanup pass 12 is terminated, and the final raw
            // pass 13 is unterminated. T = {9, 11, 12} plus the final pass 13 => K = 4 lengths
            // {6, 75, 134, 192} with added passes {1, 2, 1, 1}:
            //   layer 0: 1 1 1, 111100011 (9 passes), 0, length 20 in 3 + floor(log2 9) = 6 bits
            //   layer 1: 1 1, 1110 (5 passes), 111110 (Lblock 3 -> 8), then
            //            6 in 8 bits, 75 in 8 + floor(log2 2) = 9 bits, 134 in 8 bits,
            //            192 in 8 bits
            var style = new JpxCodeBlockStyle { SelectiveArithmeticCodingBypass = true };
            var tile = CreateTile(layers: 2, codeBlockStyle: style);

            var packet0 = Concat(
                new PacketBitWriter().Write("1 1 1 111100011 0").Write(20, 6).ToArray(),
                Body(20));
            var packet1 = Concat(
                new PacketBitWriter()
                    .Write("1 1 1110 111110")
                    .Write(6, 8)
                    .Write(75, 9)
                    .Write(134, 8)
                    .Write(192, 8)
                    .ToArray(),
                Body(6 + 75 + 134 + 192, seed: 20));

            tile.Read(Concat(packet0, packet1));

            var codeBlock = tile.CodeBlock();
            Assert.AreEqual(14, codeBlock.CodingPasses);
            Assert.AreEqual(8, codeBlock.Lblock);
            Assert.AreEqual(4, codeBlock.Segments.Count);

            // Pass 9 continues the open segment from layer 0 and terminates it.
            Assert.AreEqual(10, codeBlock.Segments[0].CodingPasses);
            Assert.AreEqual(true, codeBlock.Segments[0].Terminated);
            Assert.AreEqual(26, codeBlock.Segments[0].Data.ByteCount);

            Assert.AreEqual(2, codeBlock.Segments[1].CodingPasses);
            Assert.AreEqual(true, codeBlock.Segments[1].Terminated);
            Assert.AreEqual(75, codeBlock.Segments[1].Data.ByteCount);

            Assert.AreEqual(1, codeBlock.Segments[2].CodingPasses);
            Assert.AreEqual(true, codeBlock.Segments[2].Terminated);
            Assert.AreEqual(134, codeBlock.Segments[2].Data.ByteCount);

            Assert.AreEqual(1, codeBlock.Segments[3].CodingPasses);
            Assert.AreEqual(false, codeBlock.Segments[3].Terminated);
            Assert.AreEqual(192, codeBlock.Segments[3].Data.ByteCount);

            // The body bytes are consumed contiguously in segment order.
            Assert.AreEqual(
                Concat(Body(20), Body(6, seed: 20)),
                codeBlock.Segments[0].Data.ToArray());
            Assert.AreEqual(Body(75, seed: 26), codeBlock.Segments[1].Data.ToArray());
        }

        [Test]
        public void ReadTilePackets_TerminationOnEachCodingPass()
        {
            // With termination on each coding pass (Table A.19), every pass is a terminated
            // codeword segment, so one length per pass is signalled (D.4.1, B.10.7.2):
            //   1 1 1, 1100 (3 passes), 0 (Lblock), then 3 lengths of 3 bits: 1, 2, 3.
            var style = new JpxCodeBlockStyle { TerminationOnEachCodingPass = true };
            var tile = CreateTile(codeBlockStyle: style);

            var header = new PacketBitWriter().Write("1 1 1 1100 0 001 010 011").ToArray();
            tile.Read(Concat(header, Body(6)));

            var codeBlock = tile.CodeBlock();
            Assert.AreEqual(3, codeBlock.CodingPasses);
            Assert.AreEqual(3, codeBlock.Segments.Count);

            Assert.AreEqual(1, codeBlock.Segments[0].CodingPasses);
            Assert.AreEqual(true, codeBlock.Segments[0].Terminated);
            Assert.AreEqual(new byte[] { 0 }, codeBlock.Segments[0].Data.ToArray());

            Assert.AreEqual(true, codeBlock.Segments[1].Terminated);
            Assert.AreEqual(new byte[] { 1, 2 }, codeBlock.Segments[1].Data.ToArray());

            Assert.AreEqual(true, codeBlock.Segments[2].Terminated);
            Assert.AreEqual(new byte[] { 3, 4, 5 }, codeBlock.Segments[2].Data.ToArray());
        }

        [Test]
        public void ReadTilePackets_CodeBlocksInRasterOrderWithinPrecinct()
        {
            // A 32x16 image with 16x16 code-blocks gives two code-blocks in the LL band. The
            // inclusion and zero bit-plane tag trees are 2x1 (root + two leaves):
            //   1        non-zero length packet
            //   11       inclusion of block 0: root value 0, leaf 0 value 0
            //   11       zero bit-planes of block 0: root value 0, leaf 0 value 0
            //   0 0 001  1 pass, Lblock unchanged, 1 byte
            //   1        inclusion of block 1: leaf 1 value 0 (root already known)
            //   1        zero bit-planes of block 1: leaf 1 value 0
            //   0 0 010  1 pass, Lblock unchanged, 2 bytes
            // The bodies follow in the same order (B.9, B.10.8).
            var tile = CreateTile(width: 32);
            var header = new PacketBitWriter().Write("1 11 11 0 0 001 1 1 0 0 010").ToArray();

            tile.Read(Concat(header, Body(3)));

            var codeBlock0 = tile.CodeBlock(0);
            var codeBlock1 = tile.CodeBlock(1);

            Assert.AreEqual(true, codeBlock0.Included);
            Assert.AreEqual(true, codeBlock1.Included);
            Assert.AreEqual(new byte[] { 0 }, codeBlock0.Segments[0].Data.ToArray());
            Assert.AreEqual(new byte[] { 1, 2 }, codeBlock1.Segments[0].Data.ToArray());
        }

        [Test]
        public void ReadTilePackets_PackedPacketHeaders()
        {
            // With PPM/PPT, the packet headers come from the packed header reader and the tile
            // data holds only the packet bodies (B.10).
            var tile = CreateTile(layers: 2);
            var headers = Concat(
                new PacketBitWriter().Write("1 1 0001 0 0 101").ToArray(),
                new PacketBitWriter().Write("1 1 10 0 1001").ToArray());
            var bodies = Concat(Body(5), Body(9, seed: 5));

            tile.Read(bodies, packedHeaders: headers);

            var codeBlock = tile.CodeBlock();
            Assert.AreEqual(true, codeBlock.Included);
            Assert.AreEqual(3, codeBlock.ZeroBitPlanes);
            Assert.AreEqual(3, codeBlock.CodingPasses);
            Assert.AreEqual(1, codeBlock.Segments.Count);
            Assert.AreEqual(Concat(Body(5), Body(9, seed: 5)), codeBlock.Segments[0].Data.ToArray());
        }

        [Test]
        public void ReadTilePackets_DataSpanningTileParts()
        {
            // B.11: tile-part boundaries fall at packet boundaries, but the caller concatenates
            // the tile-part data before creating the reader, so it also survives a split inside
            // a packet.
            var tile = CreateTile(layers: 2);
            var packet0 = Concat(new PacketBitWriter().Write("1 1 0001 0 0 101").ToArray(), Body(5));
            var packet1 = Concat(new PacketBitWriter().Write("1 1 10 0 1001").ToArray(), Body(9, seed: 5));

            tile.Read(Concat(packet0, packet1));

            var codeBlock = tile.CodeBlock();
            Assert.AreEqual(3, codeBlock.CodingPasses);
            Assert.AreEqual(Concat(Body(5), Body(9, seed: 5)), codeBlock.Segments[0].Data.ToArray());
        }

        [Test]
        public void ReadTilePackets_SopMarkerSegments()
        {
            // A.8.1: an SOP marker segment (FF91 0004 Nsop) may precede each packet.
            var tile = CreateTile(layers: 2, sop: true);
            var data = Concat(
                new byte[] { 0xFF, 0x91, 0x00, 0x04, 0x00, 0x00 },
                new PacketBitWriter().Write("1 1 0001 0 0 101").ToArray(),
                Body(5),
                new byte[] { 0xFF, 0x91, 0x00, 0x04, 0x00, 0x01 },
                new PacketBitWriter().Write("1 0").ToArray());

            tile.Read(data);

            var codeBlock = tile.CodeBlock();
            Assert.AreEqual(true, codeBlock.Included);
            Assert.AreEqual(Body(5), codeBlock.Segments[0].Data.ToArray());
        }

        [Test]
        public void ReadTilePackets_SopMarkersMissingDespiteScodFlag()
        {
            // Whether or not the SOP marker segment is used is up to the encoder per packet
            // (A.8.1), so a stream without SOP markers decodes even when Scod announced them.
            var tile = CreateTile(sop: true);
            var header = new PacketBitWriter().Write("1 1 0001 0 0 101").ToArray();

            tile.Read(Concat(header, Body(5)));

            Assert.AreEqual(true, tile.CodeBlock().Included);
        }

        [Test]
        public void ReadTilePackets_SopNsopMismatchIsTolerated()
        {
            var tile = CreateTile(sop: true);
            var data = Concat(
                new byte[] { 0xFF, 0x91, 0x00, 0x04, 0x12, 0x34 },
                new PacketBitWriter().Write("1 1 0001 0 0 101").ToArray(),
                Body(5));

            tile.Read(data);

            Assert.AreEqual(true, tile.CodeBlock().Included);
            Assert.AreEqual(Body(5), tile.CodeBlock().Segments[0].Data.ToArray());
        }

        [Test]
        public void ReadTilePackets_EphMarkers()
        {
            // A.8.2: with EPH signalled in Scod, each packet header is postpended with an EPH
            // marker before the packet body.
            var tile = CreateTile(layers: 2, eph: true);
            var data = Concat(
                new PacketBitWriter().Write("1 1 0001 0 0 101").ToArray(),
                new byte[] { 0xFF, 0x92 },
                Body(5),
                new PacketBitWriter().Write("1 0").ToArray(),
                new byte[] { 0xFF, 0x92 });

            tile.Read(data);

            var codeBlock = tile.CodeBlock();
            Assert.AreEqual(true, codeBlock.Included);
            Assert.AreEqual(Body(5), codeBlock.Segments[0].Data.ToArray());
        }

        [Test]
        public void ReadTilePackets_MissingEphIsTolerated()
        {
            var tile = CreateTile(eph: true);
            var header = new PacketBitWriter().Write("1 1 0001 0 0 101").ToArray();

            tile.Read(Concat(header, Body(5)));

            Assert.AreEqual(true, tile.CodeBlock().Included);
            Assert.AreEqual(Body(5), tile.CodeBlock().Segments[0].Data.ToArray());
        }

        [Test]
        public void ReadTilePackets_HeaderWithFFByteUsesBitStuffing()
        {
            // B.10.1: a header byte of 0xFF is followed by a stuffed zero bit. Header bits:
            //   1 1 1 (non-zero, included, zero bit-planes 0), then the 37-pass codeword
            //   1111 11111 0000000 (Table B.4): the first eight bits are all ones, so the first
            //   header byte is 0xFF and the following byte carries a stuffed zero MSB.
            //   Then 0 (Lblock unchanged) and a length of 3 + floor(log2 37) = 8 bits, value 1.
            var tile = CreateTile();
            var header = new PacketBitWriter().Write("1 1 1 1111111110000000 0").Write(1, 8).ToArray();

            Assert.AreEqual(0xFF, header[0]);
            Assert.AreEqual(0, header[1] & 0x80);

            tile.Read(Concat(header, Body(1)));

            var codeBlock = tile.CodeBlock();
            Assert.AreEqual(37, codeBlock.CodingPasses);
            Assert.AreEqual(Body(1), codeBlock.Segments[0].Data.ToArray());
        }

        [Test]
        public void ReadTilePackets_TruncatedBodyDecodesBestEffort()
        {
            // B.11: the final packet may be truncated. The available bytes are kept.
            var tile = CreateTile();
            var header = new PacketBitWriter().Write("1 1 0001 0 0 101").ToArray();

            tile.Read(Concat(header, Body(2)));

            var codeBlock = tile.CodeBlock();
            Assert.AreEqual(true, codeBlock.Included);
            Assert.AreEqual(1, codeBlock.Segments.Count);
            Assert.AreEqual(Body(2), codeBlock.Segments[0].Data.ToArray());
        }

        [Test]
        public void ReadTilePackets_MissingTrailingPacketsDecodeBestEffort()
        {
            // B.11: any number of whole packets may be dropped from the end of a tile.
            var tile = CreateTile(layers: 3);
            var packet0 = Concat(new PacketBitWriter().Write("1 1 0001 0 0 101").ToArray(), Body(5));

            tile.Read(packet0);

            var codeBlock = tile.CodeBlock();
            Assert.AreEqual(true, codeBlock.Included);
            Assert.AreEqual(1, codeBlock.CodingPasses);
        }

        [Test]
        public void ReadTilePackets_T800TableB5Example()
        {
            // The example packet header bit stream of ITU-T T.800 (06/2019) Table B.5, with the code-block
            // information known to the encoder given in Figure B.13: a sub-band with 3x2 code-blocks whose first
            // inclusion layers are {0, 0, 2; 2, 1, 1} and zero bit-plane counts {3, 4, 7; 3, 3, 6}. A 48x32 image
            // with 16x16 code-blocks realizes the 3x2 grid in the LL band. Each bit group below is one row of
            // Table B.5 with the derived meaning stated in the table.
            var tile = CreateTile(width: 48, height: 32, layers: 2);

            var packet0 = Concat(
                new PacketBitWriter()
                    .Write("1")      // Packet non-zero in length
                    .Write("111")    // Code-block 0, 0 included for the first time (partial inclusion tag tree)
                    .Write("000111") // Code-block 0, 0 insignificant for 3 bit-planes
                    .Write("1100")   // Code-block 0, 0 has 3 coding passes included
                    .Write("0")      // Code-block 0, 0 length indicator is unchanged
                    .Write("0100")   // Code-block 0, 0 has 4 bytes, 4 bits are used, 3 + floor(log2 3)
                    .Write("1")      // Code-block 1, 0 included for the first time (partial inclusion tag tree)
                    .Write("01")     // Code-block 1, 0 insignificant for 4 bit-planes
                    .Write("10")     // Code-block 1, 0 has 2 coding passes included
                    .Write("10")     // Code-block 1, 0 length indicator is increased by 1 bit (3 to 4)
                    .Write("00100")  // Code-block 1, 0 has 4 bytes, 5 bits are used, 4 + floor(log2 2)
                    .Write("0")      // Code-block 2, 0 not yet included (partial tag tree)
                    .Write("0")      // Code-block 0, 1 not yet included
                    .Write("0")      // Code-block 1, 1 not yet included
                                     // Code-block 2, 1 not yet included (no data needed, already conveyed by
                                     // partial tag tree for code-block 2, 0)
                    .ToArray(),
                Body(8));

            var packet1 = Concat(
                new PacketBitWriter()
                    .Write("1")      // Packet non-zero in length
                    .Write("1")      // Code-block 0, 0 included again
                    .Write("1100")   // Code-block 0, 0 has 3 coding passes included
                    .Write("0")      // Code-block 0, 0 length indicator is unchanged
                    .Write("1010")   // Code-block 0, 0 has 10 bytes, 3 + log2(3) bits used
                    .Write("0")      // Code-block 1, 0 not included in this layer
                    .Write("10")     // Code-block 2, 0 not yet included
                    .Write("0")      // Code-block 0, 1 not yet included
                    .Write("1")      // Code-block 1, 1 included for the first time
                    .Write("1")      // Code-block 1, 1 insignificant for 3 bit-planes
                    .Write("0")      // Code-block 1, 1 has 1 coding pass included
                    .Write("0")      // Code-block 1, 1 length information is unchanged
                    .Write("001")    // Code-block 1, 1 has 1 byte, 3 + log2(1) bits used
                    .Write("1")      // Code-block 2, 1 included for the first time
                    .Write("00011")  // Code-block 2, 1 insignificant for 6 bit-planes
                    .Write("0")      // Code-block 2, 1 has 1 coding pass included
                    .Write("0")      // Code-block 2, 1 length indicator is unchanged
                    .Write("010")    // Code-block 2, 1 has 2 bytes, 3 + log2(1) bits used
                    .ToArray(),
                Body(13, seed: 8));

            tile.Read(Concat(packet0, packet1));

            var codeBlock00 = tile.CodeBlock(0);
            Assert.AreEqual(true, codeBlock00.Included);
            Assert.AreEqual(3, codeBlock00.ZeroBitPlanes);
            Assert.AreEqual(6, codeBlock00.CodingPasses);
            Assert.AreEqual(3, codeBlock00.Lblock);
            Assert.AreEqual(1, codeBlock00.Segments.Count);
            Assert.AreEqual(Concat(Body(4), Body(10, seed: 8)), codeBlock00.Segments[0].Data.ToArray());

            var codeBlock10 = tile.CodeBlock(1);
            Assert.AreEqual(true, codeBlock10.Included);
            Assert.AreEqual(4, codeBlock10.ZeroBitPlanes);
            Assert.AreEqual(2, codeBlock10.CodingPasses);
            Assert.AreEqual(4, codeBlock10.Lblock);
            Assert.AreEqual(Body(4, seed: 4), codeBlock10.Segments[0].Data.ToArray());

            // Code-blocks 2, 0 and 0, 1 are first included in layer 2, which is beyond the coded layers.
            Assert.AreEqual(false, tile.CodeBlock(2).Included);
            Assert.AreEqual(false, tile.CodeBlock(3).Included);

            var codeBlock11 = tile.CodeBlock(4);
            Assert.AreEqual(true, codeBlock11.Included);
            Assert.AreEqual(3, codeBlock11.ZeroBitPlanes);
            Assert.AreEqual(1, codeBlock11.CodingPasses);
            Assert.AreEqual(Body(1, seed: 18), codeBlock11.Segments[0].Data.ToArray());

            var codeBlock21 = tile.CodeBlock(5);
            Assert.AreEqual(true, codeBlock21.Included);
            Assert.AreEqual(6, codeBlock21.ZeroBitPlanes);
            Assert.AreEqual(1, codeBlock21.CodingPasses);
            Assert.AreEqual(Body(2, seed: 19), codeBlock21.Segments[0].Data.ToArray());
        }

        // The packet headers of the worked decoding example in ITU-T T.800 (06/2019) Section J.10: a 1x9 image
        // with one 8-bit component, one decomposition level, one layer, LRCP progression, 64x64 code-blocks and
        // no quantization (guard bits 2, exponents {8, 9, 9, 10}). Resolution level 0 holds a 1x5 LL code-block
        // and resolution level 1 a 1x4 1LH code-block (the 1HL and 1HH bands have no samples for a 1 sample wide
        // image), so the tile consists of two packets.
        [Test]
        public void ReadTilePackets_T800AnnexJ10Example()
        {
            var tile = CreateAnnexJ10Tile();

            var data = new byte[]
            {
                // First packet header (Table J.20): 1 (non-zero length packet), 1 (only code-block is included),
                // 0001 (3 zero bit-planes), 11 1101010 (16 coding passes), 0 (Lblock remains 3),
                // 0000110 (6 bytes of compressed data), 0 (padding)
                0xC7, 0xD4, 0x0C,

                // First packet body
                0x01, 0x8F, 0x0D, 0xC8, 0x75, 0x5D,

                // Second packet header (Table J.21): 1 (non-zero length packet), 1 (only code-block is included),
                // 000000 01 (7 zero bit-planes), 111100 001 (7 coding passes), 0 (Lblock remains 3),
                // 0001 1 (3 bytes of compressed data), 0000000 (padding)
                0xC0, 0x7C, 0x21, 0x80,

                // Second packet body
                0x0F, 0xB1, 0x76,
            };

            tile.Read(data);

            // Table J.20 footnote a: the maximum bit-planes from Equation (E-2) is 9 for the LL band
            var llBand = tile.Tile.Components[0].ResolutionLevels[0].SubBands[0];
            Assert.AreEqual(JpxSubBandType.LL, llBand.Type);
            Assert.AreEqual(9, llBand.MagnitudeBitPlanes);

            var llBlock = llBand.CodeBlocks[0];
            Assert.AreEqual(true, llBlock.Included);
            Assert.AreEqual(3, llBlock.ZeroBitPlanes);
            Assert.AreEqual(16, llBlock.CodingPasses);
            Assert.AreEqual(3, llBlock.Lblock);
            Assert.AreEqual(1, llBlock.Segments.Count);
            Assert.AreEqual(new byte[] { 0x01, 0x8F, 0x0D, 0xC8, 0x75, 0x5D }, llBlock.Segments[0].Data.ToArray());

            // Table J.21 footnote a: the maximum bit-planes is 10 for the 1LH band
            var lhBand = tile.Tile.Components[0].ResolutionLevels[1].SubBands
                .Single(subBand => subBand.Type == JpxSubBandType.LH);
            Assert.AreEqual(10, lhBand.MagnitudeBitPlanes);

            var lhBlock = lhBand.CodeBlocks[0];
            Assert.AreEqual(true, lhBlock.Included);
            Assert.AreEqual(7, lhBlock.ZeroBitPlanes);
            Assert.AreEqual(7, lhBlock.CodingPasses);
            Assert.AreEqual(3, lhBlock.Lblock);
            Assert.AreEqual(1, lhBlock.Segments.Count);
            Assert.AreEqual(new byte[] { 0x0F, 0xB1, 0x76 }, lhBlock.Segments[0].Data.ToArray());
        }

        [Test]
        public void ReadTilePackets_RunawayZeroBitPlanesThrows()
        {
            // A zero bit-plane value beyond the maximum of M'b - 1 = 73 (Equation E-2 bounds Mb
            // to 37, and Equation H-3 adds at most an ROI shift of 37) is malformed.
            var writer = new PacketBitWriter().Write("1 1");
            for (var i = 0; i < 100; i++)
            {
                writer.Write("0");
            }

            var tile = CreateTile();
            var data = writer.ToArray();

            Assert.Throws<JpxException>(() => tile.Read(data));
        }

        [Test]
        public void ReadTilePackets_RunawayLblockThrows()
        {
            // 40 one bits in the Lblock signalling would make the length field wider than 31
            // bits (Equation B-19).
            var writer = new PacketBitWriter().Write("1 1 1 0");
            for (var i = 0; i < 40; i++)
            {
                writer.Write("1");
            }
            writer.Write("0");

            var tile = CreateTile();
            var data = writer.ToArray();

            Assert.Throws<JpxException>(() => tile.Read(data));
        }

        /// <summary>
        /// Packs bits MSB first with the bit-stuffing of ITU-T T.800 (06/2019) Section B.10.1:
        /// after an 0xFF byte, a zero bit is stuffed into the MSB of the next byte.
        /// </summary>
        private class PacketBitWriter
        {
            private readonly List<byte> bytes = new List<byte>();
            private int current;
            private int bitCount;
            private bool stuffNext;

            public PacketBitWriter Write(string bits)
            {
                foreach (var ch in bits)
                {
                    if (ch == '0' || ch == '1')
                    {
                        WriteBit(ch - '0');
                    }
                }
                return this;
            }

            public PacketBitWriter Write(int value, int bits)
            {
                for (var i = bits - 1; i >= 0; i--)
                {
                    WriteBit((value >> i) & 1);
                }
                return this;
            }

            private void WriteBit(int bit)
            {
                if (bitCount == 0 && stuffNext)
                {
                    // Stuffed zero MSB after an 0xFF byte
                    current = 0;
                    bitCount = 1;
                    stuffNext = false;
                }

                current = (current << 1) | bit;
                bitCount++;

                if (bitCount == 8)
                {
                    bytes.Add((byte)current);
                    stuffNext = current == 0xFF;
                    current = 0;
                    bitCount = 0;
                }
            }

            /// <summary>Pads the last byte with zero bits (Section B.10.1).</summary>
            public byte[] ToArray()
            {
                var result = new List<byte>(bytes);
                if (bitCount > 0)
                {
                    result.Add((byte)(current << (8 - bitCount)));
                }
                else if (stuffNext)
                {
                    // The header must not end with an 0xFF byte
                    result.Add(0);
                }
                return result.ToArray();
            }
        }

        private static byte[] Concat(params byte[][] parts)
        {
            var result = new byte[parts.Sum(part => part.Length)];
            var cursor = 0;
            foreach (var part in parts)
            {
                part.CopyTo(result, cursor);
                cursor += part.Length;
            }
            return result;
        }

        /// <summary>Body bytes with a recognizable pattern, so slicing offsets can be asserted.</summary>
        private static byte[] Body(int length, int seed = 0)
        {
            var result = new byte[length];
            for (var i = 0; i < length; i++)
            {
                // Avoid 0xFF so crafted bodies never look like marker segments.
                result[i] = (byte)((seed + i) % 0xFF);
            }
            return result;
        }

        private class TestTile
        {
            public JpxImageInfo Image;
            public JpxTile Tile;
            public JpxResolvedTileParameters Parameters;

            public JpxCodeBlock CodeBlock(int index = 0) =>
                Tile.Components[0].ResolutionLevels[0].SubBands[0].CodeBlocks[index];

            public JpxPacketReader CreateReader(
                byte[] data, byte[] packedHeaders = null, bool retainCodeBlockData = true)
            {
                return new JpxPacketReader(
                    Tile,
                    Image.Components,
                    Parameters,
                    new JpxDataReader(data),
                    packedHeaders == null ? null : new JpxDataReader(new ArraySegment<byte>(packedHeaders)),
                    new[] { retainCodeBlockData });
            }

            public void Read(byte[] data, byte[] packedHeaders = null)
            {
                CreateReader(data, packedHeaders).ReadTilePackets();
            }
        }

        /// <summary>
        /// A single-tile, single-component image with NL = 0, so that resolution level 0 holds a single LL band
        /// covering [0, width) x [0, height) with one precinct (PPx = PPy = 15) and 16x16 code-blocks.
        /// Quantization: guard bits 2, exponent 9 => Mb = 10 (Equation E-2).
        /// </summary>
        private static TestTile CreateTile(
            int width = 16,
            int height = 16,
            int layers = 1,
            JpxCodeBlockStyle codeBlockStyle = default(JpxCodeBlockStyle),
            bool sop = false,
            bool eph = false)
        {
            var image = new JpxImageInfo
            {
                Xsiz = width,
                Ysiz = height,
                XTsiz = width,
                YTsiz = height,
                Components = new[] { new JpxComponent { Ssizi = 7, XRsizi = 1, YRsizi = 1 } },
            };

            var codingStyle = new JpxCodingStyleDefaults
            {
                NumberOfDecompositionLevels = 0,
                CodeBlockWidth = 16,
                CodeBlockHeight = 16,
                CodeBlockStyle = codeBlockStyle,
                PrecinctSize = new[] { new JpxPrecinctSize { PPx = 15, PPy = 15 } },
            };

            var quantization = new JpxQuantizationDefaults
            {
                NumberOfGuardBits = 2,
                Values = new[] { new JpxQuantizationDefaultValues { Exponent = 9, Mantissa = 0 } },
            };

            var parameters = new JpxResolvedTileParameters(
                JpxCodingProgressionOrder.LayerResolutionLevelComponentPosition,
                layers,
                multipleComponentTransformation: false,
                sopMarkerSegments: sop,
                ephMarkerSegments: eph,
                progressionOrderChanges: null,
                new[] { codingStyle },
                new[] { quantization },
                new int[1]);

            return new TestTile
            {
                Image = image,
                Parameters = parameters,
                Tile = JpxTileLayoutBuilder.BuildTile(image, 0, parameters),
            };
        }

        /// <summary>
        /// The image of the worked decoding example in ITU-T T.800 (06/2019) Section J.10: 1x9, one 8-bit unsigned
        /// component, a single tile, one decomposition level, one layer, LRCP progression, 64x64 code-blocks, no
        /// precincts, and no quantization with guard bits 2 and exponents {8, 9, 9, 10}.
        /// </summary>
        private static TestTile CreateAnnexJ10Tile()
        {
            var image = new JpxImageInfo
            {
                Xsiz = 1,
                Ysiz = 9,
                XTsiz = 1,
                YTsiz = 9,
                Components = new[] { new JpxComponent { Ssizi = 7, XRsizi = 1, YRsizi = 1 } },
            };

            var codingStyle = new JpxCodingStyleDefaults
            {
                NumberOfDecompositionLevels = 1,
                CodeBlockWidth = 64,
                CodeBlockHeight = 64,
                PrecinctSize = new[]
                {
                    new JpxPrecinctSize { PPx = 15, PPy = 15 },
                    new JpxPrecinctSize { PPx = 15, PPy = 15 },
                },
            };

            var quantization = new JpxQuantizationDefaults
            {
                NumberOfGuardBits = 2,
                Values = new[]
                {
                    new JpxQuantizationDefaultValues { Exponent = 8, Mantissa = 0 },
                    new JpxQuantizationDefaultValues { Exponent = 9, Mantissa = 0 },
                    new JpxQuantizationDefaultValues { Exponent = 9, Mantissa = 0 },
                    new JpxQuantizationDefaultValues { Exponent = 10, Mantissa = 0 },
                },
            };

            var parameters = new JpxResolvedTileParameters(
                JpxCodingProgressionOrder.LayerResolutionLevelComponentPosition,
                numberOfLayers: 1,
                multipleComponentTransformation: false,
                sopMarkerSegments: false,
                ephMarkerSegments: false,
                progressionOrderChanges: null,
                new[] { codingStyle },
                new[] { quantization },
                new int[1]);

            return new TestTile
            {
                Image = image,
                Parameters = parameters,
                Tile = JpxTileLayoutBuilder.BuildTile(image, 0, parameters),
            };
        }
    }
}
