// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx;
using PdfToSvg.Imaging.Jpx.Codestream;
using PdfToSvg.Imaging.Jpx.ImageModel;
using System;
using System.Linq;

namespace PdfToSvg.Tests.Images.Jpx.Codestream
{
    internal class JpxCodestreamReaderTests
    {
        private static JpxCodestreamReader CreateReader(byte[] codestream, out JpxImageInfo image)
        {
            var reader = new JpxCodestreamReader(new ArraySegment<byte>(codestream));
            image = new JpxImageInfo();
            reader.ReadMainHeader(image);
            return reader;
        }

        [Test]
        public void ReadMainHeader_PopulatesSizAndMainParameters()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(100, 50, 10, 5, 90, 45, 0, 0,
                    new byte[] { 7, 1, 1 },     // 8-bit unsigned
                    new byte[] { 0x87, 1, 1 },  // 8-bit signed
                    new byte[] { 11, 2, 2 }),   // 12-bit unsigned, 2x2 sub-sampling
                JpxTestCodestream.Cod(layers: 3, mct: true, decompositionLevels: 2),
                JpxTestCodestream.Qcd(),
                JpxTestCodestream.TilePart(0, 0, 1, new byte[0], new byte[] { 1, 2, 3 }),
                JpxTestCodestream.Eoc());

            var reader = CreateReader(codestream, out var image);

            Assert.AreEqual(100, image.Xsiz);
            Assert.AreEqual(50, image.Ysiz);
            Assert.AreEqual(10, image.XOsiz);
            Assert.AreEqual(5, image.YOsiz);
            Assert.AreEqual(3, image.Components.Length);
            Assert.AreEqual(8, image.Components[0].Precision);
            Assert.AreEqual(false, image.Components[0].Signed);
            Assert.AreEqual(true, image.Components[1].Signed);
            Assert.AreEqual(12, image.Components[2].Precision);

            var cod = reader.MainHeaderParameters.CodingStyleDefaults;
            Assert.IsNotNull(cod);
            Assert.AreEqual(3, cod!.NumberOfLayers);
            Assert.AreEqual(true, cod.MultipleComponentTransformation);
            Assert.IsNotNull(reader.MainHeaderParameters.QuantizationDefaults);
        }

        [Test]
        public void ReadMainHeader_StopsAtFirstSot()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 1, new byte[0], new byte[] { 0xAB, 0xCD }),
                JpxTestCodestream.Eoc());

            var reader = CreateReader(codestream, out _);

            // The SOT must not have been consumed by the main header parse.
            var tilePart = reader.ReadTilePart();

            Assert.IsNotNull(tilePart);
            Assert.AreEqual(0, tilePart!.TileIndex);
            Assert.AreEqual(new byte[] { 0xAB, 0xCD }, JpxTestCodestream.ToArray(tilePart.Data));
        }

        [Test]
        public void ReadMainHeader_MissingSoc()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Siz(32, 16, 0, 0, 32, 16, 0, 0, new byte[] { 7, 1, 1 }));

            var reader = new JpxCodestreamReader(new ArraySegment<byte>(codestream));
            Assert.Throws<JpxException>(() => reader.ReadMainHeader(new JpxImageInfo()));
        }

        [Test]
        public void ReadMainHeader_SizNotFirst()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Cod());

            var reader = new JpxCodestreamReader(new ArraySegment<byte>(codestream));
            Assert.Throws<JpxException>(() => reader.ReadMainHeader(new JpxImageInfo()));
        }

        [Test]
        public void ReadMainHeader_SkipsUnknownSkippableMarker()
        {
            // 0xFF65 is not defined by T.800; ITU-T T.800 (06/2019) Section A.1 says unknown
            // marker segments are discarded using their length field.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(32, 16, 0, 0, 32, 16, 0, 0, new byte[] { 7, 1, 1 }),
                JpxTestCodestream.Segment(0xFF65, new byte[] { 1, 2, 3 }),
                JpxTestCodestream.Cod(layers: 4),
                JpxTestCodestream.Qcd(),
                JpxTestCodestream.TilePart(0, 0, 1, new byte[0], new byte[] { 1 }),
                JpxTestCodestream.Eoc());

            var reader = CreateReader(codestream, out _);

            Assert.AreEqual(4, reader.MainHeaderParameters.CodingStyleDefaults!.NumberOfLayers);
        }

        [Test]
        public void ReadMainHeader_SkipsParameterlessReservedMarker()
        {
            // ITU-T T.800 (06/2019) Section A.1.3: markers 0xFF30-0xFF3F have no marker segment
            // parameters and shall be skipped.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(32, 16, 0, 0, 32, 16, 0, 0, new byte[] { 7, 1, 1 }),
                JpxTestCodestream.U16(0xFF30),
                JpxTestCodestream.Cod(layers: 2),
                JpxTestCodestream.Qcd(),
                JpxTestCodestream.TilePart(0, 0, 1, new byte[0], new byte[] { 1 }),
                JpxTestCodestream.Eoc());

            var reader = CreateReader(codestream, out _);

            Assert.AreEqual(2, reader.MainHeaderParameters.CodingStyleDefaults!.NumberOfLayers);
        }

        [Test]
        public void ReadMainHeader_McTransformExtensionRejected()
        {
            // An MCC marker segment (ITU-T T.801 (06/2021) Table A.19) signals the multiple component
            // transformation extension, which is required for correct colours and not supported.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(32, 16, 0, 0, 32, 16, 0, 0, new byte[] { 7, 1, 1 }),
                JpxTestCodestream.Cod(),
                JpxTestCodestream.Qcd(),
                JpxTestCodestream.Segment(0xFF75, new byte[] { 0, 0, 0, 0, 0 }));

            var reader = new JpxCodestreamReader(new ArraySegment<byte>(codestream));
            Assert.Throws<JpxException>(() => reader.ReadMainHeader(new JpxImageInfo()));
        }

        [Test]
        public void ReadTilePart_McTransformExtensionRejected()
        {
            // An NLT marker segment (ITU-T T.801 (06/2021) Table A.19) in a tile-part header signals the
            // non-linearity point transformation extension, which is required for correct colours and not supported.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 1,
                    JpxTestCodestream.Segment(0xFF76, new byte[] { 0, 0, 0 }),
                    new byte[] { 1 }),
                JpxTestCodestream.Eoc());

            var reader = CreateReader(codestream, out _);
            Assert.Throws<JpxException>(() => reader.ReadTilePart());
        }

        [Test]
        public void ReadMainHeader_SkipsOtherExtensionMarkers()
        {
            // Extension marker segments other than MCT/MCC/MCO/NLT (here CBD and QPD, ITU-T T.801 (06/2021)
            // Table A.19) are skipped, and the decode continues best-effort.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(32, 16, 0, 0, 32, 16, 0, 0, new byte[] { 7, 1, 1 }),
                JpxTestCodestream.Segment(0xFF78, new byte[] { 0, 1, 7 }),
                JpxTestCodestream.Cod(layers: 3),
                JpxTestCodestream.Qcd(),
                JpxTestCodestream.Segment(0xFF5A, new byte[] { 0, 0x22 }),
                JpxTestCodestream.TilePart(0, 0, 1, new byte[0], new byte[] { 1 }),
                JpxTestCodestream.Eoc());

            var reader = CreateReader(codestream, out _);

            Assert.AreEqual(3, reader.MainHeaderParameters.CodingStyleDefaults!.NumberOfLayers);
            Assert.IsNotNull(reader.ReadTilePart());
        }

        [Test]
        public void ReadMainHeader_UnskippableMarker()
        {
            // 0xFFD0 (RST0) is defined by ITU-T T.81 without a length field and cannot be
            // skipped safely.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(32, 16, 0, 0, 32, 16, 0, 0, new byte[] { 7, 1, 1 }),
                JpxTestCodestream.U16(0xFFD0),
                JpxTestCodestream.Cod());

            var reader = new JpxCodestreamReader(new ArraySegment<byte>(codestream));
            Assert.Throws<JpxException>(() => reader.ReadMainHeader(new JpxImageInfo()));
        }

        [Test]
        public void ReadMainHeader_TruncatedSegment()
        {
            // A COD whose declared length extends past the end of the codestream.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(32, 16, 0, 0, 32, 16, 0, 0, new byte[] { 7, 1, 1 }),
                JpxTestCodestream.U16(0xFF52),
                JpxTestCodestream.U16(200),
                new byte[] { 0, 0 });

            var reader = new JpxCodestreamReader(new ArraySegment<byte>(codestream));
            Assert.Throws<JpxException>(() => reader.ReadMainHeader(new JpxImageInfo()));
        }

        [Test]
        public void ReadTilePart_ReadsSotFieldsAndData()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 2, new byte[0], new byte[] { 1, 2, 3 }),
                JpxTestCodestream.TilePart(0, 1, 2, new byte[0], new byte[] { 4, 5 }),
                JpxTestCodestream.Eoc());

            var reader = CreateReader(codestream, out _);

            var first = reader.ReadTilePart();
            Assert.IsNotNull(first);
            Assert.AreEqual(0, first!.TileIndex);
            Assert.AreEqual(0, first.TilePartIndex);
            Assert.AreEqual(2, first.TilePartCount);
            Assert.AreEqual(true, first.IsFirstTilePart);
            Assert.AreEqual(new byte[] { 1, 2, 3 }, JpxTestCodestream.ToArray(first.Data));

            var second = reader.ReadTilePart();
            Assert.IsNotNull(second);
            Assert.AreEqual(1, second!.TilePartIndex);
            Assert.AreEqual(false, second.IsFirstTilePart);
            Assert.AreEqual(new byte[] { 4, 5 }, JpxTestCodestream.ToArray(second.Data));

            Assert.IsNull(reader.ReadTilePart());
            Assert.IsNull(reader.ReadTilePart());
        }

        [Test]
        public void ReadTilePart_PsotZeroTakesRestOfCodestream()
        {
            // ITU-T T.800 (06/2019) Section A.4.2: Psot = 0 means the tile-part contains all
            // data until the EOC marker.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 0, new byte[0], new byte[] { 9, 8, 7, 6 }, psotOverride: 0),
                JpxTestCodestream.Eoc());

            var reader = CreateReader(codestream, out _);

            var tilePart = reader.ReadTilePart();
            Assert.IsNotNull(tilePart);
            Assert.AreEqual(new byte[] { 9, 8, 7, 6 }, JpxTestCodestream.ToArray(tilePart!.Data));

            Assert.IsNull(reader.ReadTilePart());
        }

        [Test]
        public void ReadTilePart_PsotZeroWithoutEoc()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 0, new byte[0], new byte[] { 9, 8, 7 }, psotOverride: 0));

            var reader = CreateReader(codestream, out _);

            var tilePart = reader.ReadTilePart();
            Assert.IsNotNull(tilePart);
            Assert.AreEqual(new byte[] { 9, 8, 7 }, JpxTestCodestream.ToArray(tilePart!.Data));

            Assert.IsNull(reader.ReadTilePart());
        }

        [Test]
        public void ReadTilePart_TruncatedPsotIsClamped()
        {
            // Psot points past the end of the codestream; the tile-part data is clamped.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 1, new byte[0], new byte[] { 1, 2, 3 }, psotOverride: 100));

            var reader = CreateReader(codestream, out _);

            var tilePart = reader.ReadTilePart();
            Assert.IsNotNull(tilePart);
            Assert.AreEqual(new byte[] { 1, 2, 3 }, JpxTestCodestream.ToArray(tilePart!.Data));
        }

        [Test]
        public void ReadTilePart_InvalidTileIndex()
        {
            // A single-tile image cannot contain a tile-part for tile 5.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(5, 0, 1, new byte[0], new byte[] { 1 }),
                JpxTestCodestream.Eoc());

            var reader = CreateReader(codestream, out _);
            Assert.Throws<JpxException>(() => reader.ReadTilePart());
        }

        [Test]
        public void ReadTilePart_OutOfOrderTilePartIndexRecovers()
        {
            // TPsot starts at 1 instead of 0; decoding continues best-effort.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 1, 0, new byte[0], new byte[] { 1, 2 }),
                JpxTestCodestream.Eoc());

            var reader = CreateReader(codestream, out _);

            var tilePart = reader.ReadTilePart();
            Assert.IsNotNull(tilePart);
            Assert.AreEqual(1, tilePart!.TilePartIndex);
            Assert.AreEqual(true, tilePart.IsFirstTilePart);
            Assert.AreEqual(new byte[] { 1, 2 }, JpxTestCodestream.ToArray(tilePart.Data));
        }

        [Test]
        public void ReadTilePart_GarbageBetweenTileParts()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 1, new byte[0], new byte[] { 1 }),
                new byte[] { 0x00, 0x01 });

            var reader = CreateReader(codestream, out _);
            reader.ReadTilePart();

            Assert.Throws<JpxException>(() => reader.ReadTilePart());
        }

        [Test]
        public void ReadTilePart_MissingSod()
        {
            // EOC in the middle of a tile-part header is a structural error.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.U16(0xFF90),
                JpxTestCodestream.U16(10),
                JpxTestCodestream.U16(0),
                JpxTestCodestream.U32(0),
                new byte[] { 0, 1 },
                JpxTestCodestream.Eoc());

            var reader = CreateReader(codestream, out _);
            Assert.Throws<JpxException>(() => reader.ReadTilePart());
        }

        [Test]
        public void ReadTilePart_TilePartCodTriggersReResolution()
        {
            // Two tiles; the first tile's first tile-part header overrides the COD. The second
            // tile keeps the main header parameters (ITU-T T.800 (06/2019) Section A.6.1).
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(32, 16, 0, 0, 16, 16, 0, 0, new byte[] { 7, 1, 1 }),
                JpxTestCodestream.Cod(layers: 1),
                JpxTestCodestream.Qcd(),
                JpxTestCodestream.TilePart(0, 0, 1, JpxTestCodestream.Cod(layers: 5), new byte[] { 1 }),
                JpxTestCodestream.TilePart(1, 0, 1, new byte[0], new byte[] { 2 }),
                JpxTestCodestream.Eoc());

            var reader = CreateReader(codestream, out _);

            // Before any tile-part is read, the main header defaults apply.
            Assert.AreEqual(1, reader.ResolveTileParameters(0).NumberOfLayers);

            reader.ReadTilePart();
            Assert.AreEqual(5, reader.ResolveTileParameters(0).NumberOfLayers);

            reader.ReadTilePart();
            Assert.AreEqual(1, reader.ResolveTileParameters(1).NumberOfLayers);
        }

        [Test]
        public void ReadTilePart_LateMarkersAreIgnored()
        {
            // ITU-T T.800 (06/2019) Section A.6.1: COD is only allowed in the first tile-part.
            // A COD in a later tile-part is ignored with a warning.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 2, JpxTestCodestream.Cod(layers: 5), new byte[] { 1 }),
                JpxTestCodestream.TilePart(0, 1, 2, JpxTestCodestream.Cod(layers: 9), new byte[] { 2 }),
                JpxTestCodestream.Eoc());

            var reader = CreateReader(codestream, out _);

            reader.ReadTilePart();
            var second = reader.ReadTilePart();

            Assert.IsNotNull(second);
            Assert.AreEqual(false, second!.IsFirstTilePart);
            Assert.AreEqual(new byte[] { 2 }, JpxTestCodestream.ToArray(second.Data));
            Assert.AreEqual(5, reader.ResolveTileParameters(0).NumberOfLayers);
        }

        [Test]
        public void ReadTilePart_CollectsPptSegments()
        {
            // Two PPT segments in one tile-part header concatenate in Zppt order
            // (ITU-T T.800 (06/2019) Section A.7.5).
            var headerSegments = JpxTestCodestream.Concat(
                JpxTestCodestream.Segment(0xFF61, new byte[] { 0, 0x11, 0x22 }),
                JpxTestCodestream.Segment(0xFF61, new byte[] { 1, 0x33 }));

            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 1, headerSegments, new byte[] { 1 }),
                JpxTestCodestream.Eoc());

            var reader = CreateReader(codestream, out _);
            reader.ReadTilePart();

            var headerReader = reader.PackedPacketHeaders.GetTileHeaderReader(0);
            Assert.IsNotNull(headerReader);
            Assert.AreEqual(new byte[] { 0x11, 0x22, 0x33 }, JpxTestCodestream.ToArray(headerReader!.ReadBytes(3)));
        }

        [Test]
        public void ReadTilePart_PpmEntriesFollowTilePartOrder()
        {
            // PPM in the main header: the kth (Nppm, Ippm) entry belongs to the kth tile-part in
            // codestream order (ITU-T T.800 (06/2019) Section A.7.4). Two tiles, interleaved
            // tile-parts.
            var ppm = JpxTestCodestream.Segment(0xFF60, JpxTestCodestream.Concat(
                new byte[] { 0 },                              // Zppm
                JpxTestCodestream.U32(2), new byte[] { 0xA0, 0xA1 },  // tile 0, part 0
                JpxTestCodestream.U32(1), new byte[] { 0xB0 },        // tile 1, part 0
                JpxTestCodestream.U32(1), new byte[] { 0xA2 }));      // tile 0, part 1

            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(32, 16, 0, 0, 16, 16, 0, 0, new byte[] { 7, 1, 1 }),
                JpxTestCodestream.Cod(),
                JpxTestCodestream.Qcd(),
                ppm,
                JpxTestCodestream.TilePart(0, 0, 2, new byte[0], new byte[] { 1 }),
                JpxTestCodestream.TilePart(1, 0, 1, new byte[0], new byte[] { 2 }),
                JpxTestCodestream.TilePart(0, 1, 2, new byte[0], new byte[] { 3 }),
                JpxTestCodestream.Eoc());

            var reader = CreateReader(codestream, out _);

            while (reader.ReadTilePart() != null)
            {
            }

            var tile0 = reader.PackedPacketHeaders.GetTileHeaderReader(0);
            var tile1 = reader.PackedPacketHeaders.GetTileHeaderReader(1);

            Assert.IsNotNull(tile0);
            Assert.IsNotNull(tile1);
            Assert.AreEqual(new byte[] { 0xA0, 0xA1, 0xA2 }, JpxTestCodestream.ToArray(tile0!.ReadBytes(3)));
            Assert.AreEqual(new byte[] { 0xB0 }, JpxTestCodestream.ToArray(tile1!.ReadBytes(1)));
        }

        [Test]
        public void ReadTilePart_BeforeMainHeader()
        {
            var reader = new JpxCodestreamReader(new ArraySegment<byte>(JpxTestCodestream.MainHeader()));
            Assert.Throws<InvalidOperationException>(() => reader.ReadTilePart());
        }

        [Test]
        public void ResolveTileParameters_BeforeMainHeaderThrows()
        {
            var reader = new JpxCodestreamReader(new ArraySegment<byte>(JpxTestCodestream.MainHeader()));
            Assert.Throws<InvalidOperationException>(() => reader.ResolveTileParameters(0));
        }

        [Test]
        public void ReadMainHeader_CalledTwiceThrows()
        {
            var codestream = JpxTestCodestream.MainHeader();
            var reader = new JpxCodestreamReader(new ArraySegment<byte>(codestream));

            reader.ReadMainHeader(new JpxImageInfo());

            Assert.Throws<InvalidOperationException>(() => reader.ReadMainHeader(new JpxImageInfo()));
        }

        [Test]
        public void ReadTilePart_T800AnnexJ10Example()
        {
            // The example codestream of ITU-T T.800 (06/2019) Section J.10. The main header parse and the
            // tile-part walk must arrive at the structure decoded in Sections J.10.1 and J.10.2: a single-tile
            // 1x9 image whose only tile-part holds the 16 bytes of packet data.
            var reader = CreateReader(JpxAnnexJ10.Codestream, out var image);

            Assert.AreEqual(1, image.Xsiz);
            Assert.AreEqual(9, image.Ysiz);
            Assert.AreEqual(1, image.NumXTiles);
            Assert.AreEqual(1, image.NumYTiles);

            var tilePart = reader.ReadTilePart();
            Assert.IsNotNull(tilePart);
            Assert.AreEqual(0, tilePart!.TileIndex);
            Assert.AreEqual(0, tilePart.TilePartIndex);
            Assert.AreEqual(1, tilePart.TilePartCount);
            Assert.AreEqual(true, tilePart.IsFirstTilePart);
            Assert.AreEqual(JpxAnnexJ10.TilePartData, JpxTestCodestream.ToArray(tilePart.Data));

            Assert.IsNull(reader.ReadTilePart());

            // There were no COD, QCD or POC markers in the tile-part header, so all coding parameters are
            // determined from the main header (Section J.10.2).
            var parameters = reader.ResolveTileParameters(0);
            Assert.AreEqual(JpxCodingProgressionOrder.LayerResolutionLevelComponentPosition, parameters.ProgressionOrder);
            Assert.AreEqual(1, parameters.NumberOfLayers);
            Assert.AreEqual(false, parameters.MultipleComponentTransformation);
            Assert.AreEqual(1, parameters.ComponentCodingStyles[0].NumberOfDecompositionLevels);
            Assert.AreEqual(64, parameters.ComponentCodingStyles[0].CodeBlockWidth);
            Assert.AreEqual(64, parameters.ComponentCodingStyles[0].CodeBlockHeight);
            Assert.AreEqual(2, parameters.ComponentQuantizations[0].NumberOfGuardBits);
            Assert.AreEqual(
                new[] { 8, 9, 9, 10 },
                parameters.ComponentQuantizations[0].Values.Select(value => value.Exponent));
        }
    }
}
