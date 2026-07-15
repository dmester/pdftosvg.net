// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx;
using PdfToSvg.Imaging.Jpx.Codestream;
using PdfToSvg.Imaging.Jpx.IO;
using System.Collections.Generic;

namespace PdfToSvg.Tests.Images.Jpx.Codestream
{
    // All payloads follow ITU-T T.800 (06/2019) Annex A; the reader is positioned right after the two marker code
    // bytes, so each test buffer starts with the segment length field.
    internal class JpxMarkerSegmentReaderTests
    {
        [Test]
        public void ReadCod_ParsesScodSgcodSpcod()
        {
            // Scod = 0b111: user-defined precincts + SOP + EPH (Table A.13).
            var reader = Segment(
                0x07,             // Scod
                0x01,             // SGcod: progression order = RLCP (Table A.16)
                0x00, 0x03,       // SGcod: number of layers
                0x01,             // SGcod: multiple component transformation
                0x02,             // SPcod: decomposition levels
                0x03,             // SPcod: xcb - 2 (code-block width 2^5 = 32)
                0x02,             // SPcod: ycb - 2 (code-block height 2^4 = 16)
                0x20,             // SPcod: code-block style (segmentation symbols)
                0x01,             // SPcod: transformation (5/3 reversible)
                0x21, 0x42, 0x77  // SPcod: precinct sizes (Table A.21)
            );

            var cod = JpxMarkerSegmentReader.ReadCod(reader);

            Assert.AreEqual(true, cod.CodingStyle.UserDefinedPrecincts);
            Assert.AreEqual(true, cod.CodingStyle.SopMarkerSegments);
            Assert.AreEqual(true, cod.CodingStyle.EphMarkerSegments);
            Assert.AreEqual(JpxCodingProgressionOrder.ResolutionLevelLayerComponentPosition, cod.ProgressionOrder);
            Assert.AreEqual(3, cod.NumberOfLayers);
            Assert.AreEqual(true, cod.MultipleComponentTransformation);
            Assert.AreEqual(2, cod.NumberOfDecompositionLevels);
            Assert.AreEqual(32, cod.CodeBlockWidth);
            Assert.AreEqual(16, cod.CodeBlockHeight);
            Assert.AreEqual(true, cod.CodeBlockStyle.SegmentationSymbolsAreUsed);
            Assert.AreEqual(false, cod.CodeBlockStyle.VerticallyCausalContext);
            Assert.AreEqual(true, cod.ReversibleFilter);

            Assert.AreEqual(3, cod.PrecinctSize.Length);
            Assert.AreEqual(1, cod.PrecinctSize[0].PPx);
            Assert.AreEqual(2, cod.PrecinctSize[0].PPy);
            Assert.AreEqual(2, cod.PrecinctSize[1].PPx);
            Assert.AreEqual(4, cod.PrecinctSize[1].PPy);
            Assert.AreEqual(7, cod.PrecinctSize[2].PPx);
            Assert.AreEqual(7, cod.PrecinctSize[2].PPy);
        }

        [Test]
        public void ReadCod_InvalidProgressionOrder()
        {
            var reader = Segment(
                0x00,             // Scod
                0x05,             // SGcod: invalid progression order (valid range 0-4, Table A.16)
                0x00, 0x01,
                0x00,
                0x00, 0x04, 0x04, 0x00, 0x01);

            Assert.Throws<JpxException>(() => JpxMarkerSegmentReader.ReadCod(reader));
        }

        [Test]
        public void ReadCod_ZeroLayers()
        {
            var reader = Segment(
                0x00,             // Scod
                0x00,
                0x00, 0x00,       // SGcod: zero layers is invalid (Table A.14)
                0x00,
                0x00, 0x04, 0x04, 0x00, 0x01);

            Assert.Throws<JpxException>(() => JpxMarkerSegmentReader.ReadCod(reader));
        }

        [Test]
        public void ReadCod_HtCodeBlocksRejected()
        {
            var reader = Segment(
                0x00,             // Scod
                0x00,
                0x00, 0x01,
                0x00,
                0x00, 0x04, 0x04,
                0x40,             // SPcod: code-block style with the ITU-T T.814 HT code-block bit set
                0x01);

            Assert.Throws<JpxException>(() => JpxMarkerSegmentReader.ReadCod(reader));
        }

        [Test]
        public void ReadExtendedCapabilities_Htj2kRejected()
        {
            // Pcap bit 15, counted from the most significant bit, signals that ITU-T T.814 (HTJ2K)
            // capabilities are required (ITU-T T.800 (06/2019) Table A.11 ter).
            var reader = Segment(
                0x00, 0x02, 0x00, 0x00, // Pcap with bit 15 set
                0x00, 0x00);            // Ccap15

            Assert.Throws<JpxException>(() => JpxMarkerSegmentReader.ReadExtendedCapabilities(reader));
        }

        [Test]
        public void ReadExtendedCapabilities_OtherPartsBestEffort()
        {
            // Capabilities of parts other than 15 are logged and decoding continues best-effort.
            var reader = Segment(
                0x40, 0x00, 0x00, 0x00, // Pcap with bit 2 set (ITU-T T.801 extensions)
                0x00, 0x00);            // Ccap2

            Assert.AreEqual(0x40000000u, JpxMarkerSegmentReader.ReadExtendedCapabilities(reader));
        }

        [Test]
        public void ReadCoc_ParsesComponentIndexAndSpcoc()
        {
            var reader = Segment(
                0x02,             // Ccoc (one byte since Csiz < 257, Table A.22)
                0x00,             // Scoc: maximum precincts
                0x01,             // SPcoc: decomposition levels
                0x02,             // SPcoc: xcb - 2 (code-block width 2^4 = 16)
                0x04,             // SPcoc: ycb - 2 (code-block height 2^6 = 64)
                0x00,             // SPcoc: code-block style
                0x00              // SPcoc: transformation (9/7 irreversible)
            );

            var coc = JpxMarkerSegmentReader.ReadCoc(reader, componentCount: 3, out var componentIndex);

            Assert.AreEqual(2, componentIndex);
            Assert.AreEqual(false, coc.UserDefinedPrecincts);
            Assert.AreEqual(1, coc.NumberOfDecompositionLevels);
            Assert.AreEqual(16, coc.CodeBlockWidth);
            Assert.AreEqual(64, coc.CodeBlockHeight);
            Assert.AreEqual(false, coc.ReversibleFilter);

            // ITU-T T.800 (06/2019) Table A.13: maximum precincts means PPx = PPy = 15.
            Assert.AreEqual(2, coc.PrecinctSize.Length);
            Assert.AreEqual(15, coc.PrecinctSize[0].PPx);
            Assert.AreEqual(15, coc.PrecinctSize[1].PPy);
        }

        [Test]
        public void ReadCoc_InvalidComponentIndex()
        {
            var reader = Segment(0x05, 0x00, 0x01, 0x02, 0x02, 0x00, 0x00);

            Assert.Throws<JpxException>(() => JpxMarkerSegmentReader.ReadCoc(reader, componentCount: 3, out _));
        }

        [Test]
        public void ReadQcd_ReversibleValues()
        {
            var reader = Segment(
                0x40,             // Sqcd: guard bits = 2, style = no quantization (Table A.28)
                0x40, 0x48, 0x50  // SPqcd: exponents 8, 9, 10 in the upper 5 bits (Table A.29)
            );

            var qcd = JpxMarkerSegmentReader.ReadQcd(reader);

            Assert.AreEqual(2, qcd.NumberOfGuardBits);
            Assert.AreEqual(false, qcd.ScalarDerived);
            Assert.AreEqual(false, qcd.ScalarExpounded);
            Assert.AreEqual(3, qcd.Values.Length);
            Assert.AreEqual(8, qcd.Values[0].Exponent);
            Assert.AreEqual(9, qcd.Values[1].Exponent);
            Assert.AreEqual(10, qcd.Values[2].Exponent);
        }

        [Test]
        public void ReadQcc_ScalarDerived()
        {
            var reader = Segment(
                0x01,             // Cqcc (one byte since Csiz < 257, Table A.31)
                0x21,             // Sqcc: guard bits = 1, style = scalar derived
                0x69, 0x55        // SPqcc: exponent = 13, mantissa = 0x155 (Table A.30)
            );

            var qcc = JpxMarkerSegmentReader.ReadQcc(reader, componentCount: 3, out var componentIndex);

            Assert.AreEqual(1, componentIndex);
            Assert.AreEqual(1, qcc.NumberOfGuardBits);
            Assert.AreEqual(true, qcc.ScalarDerived);
            Assert.AreEqual(false, qcc.ScalarExpounded);
            Assert.AreEqual(true, qcc.ComponentSpecific);
            Assert.AreEqual(1, qcc.Values.Length);
            Assert.AreEqual(13, qcc.Values[0].Exponent);
            Assert.AreEqual(0x155, qcc.Values[0].Mantissa);
        }

        [Test]
        public void ReadPoc_ParsesVolumes()
        {
            var reader = Segment(
                // Volume 1 (7 bytes since Csiz < 257, Table A.32)
                0x00,             // RSpoc
                0x00,             // CSpoc
                0x00, 0x02,       // LYEpoc
                0x03,             // REpoc
                0x00,             // CEpoc = 0 is shorthand for the full component count
                0x04,             // Ppoc = CPRL
                                  // Volume 2
                0x01,             // RSpoc
                0x01,             // CSpoc
                0x00, 0x05,       // LYEpoc
                0x02,             // REpoc
                0x02,             // CEpoc
                0x00              // Ppoc = LRCP
            );

            var volumes = JpxMarkerSegmentReader.ReadPoc(reader, componentCount: 3);

            Assert.AreEqual(2, volumes.Count);

            Assert.AreEqual(0, volumes[0].ResolutionStart);
            Assert.AreEqual(3, volumes[0].ResolutionEnd);
            Assert.AreEqual(0, volumes[0].ComponentStart);
            Assert.AreEqual(3, volumes[0].ComponentEnd);
            Assert.AreEqual(2, volumes[0].LayerEnd);
            Assert.AreEqual(JpxCodingProgressionOrder.ComponentPositionResolutionLevelLayer, volumes[0].ProgressionOrder);

            Assert.AreEqual(1, volumes[1].ResolutionStart);
            Assert.AreEqual(2, volumes[1].ResolutionEnd);
            Assert.AreEqual(1, volumes[1].ComponentStart);
            Assert.AreEqual(2, volumes[1].ComponentEnd);
            Assert.AreEqual(5, volumes[1].LayerEnd);
            Assert.AreEqual(JpxCodingProgressionOrder.LayerResolutionLevelComponentPosition, volumes[1].ProgressionOrder);
        }

        [Test]
        public void ReadPoc_ZeroLayerEnd()
        {
            var reader = Segment(
                0x00, 0x00,
                0x00, 0x00,       // LYEpoc = 0 is invalid (Table A.32: 1 to 65535)
                0x03, 0x00, 0x00);

            Assert.Throws<JpxException>(() => JpxMarkerSegmentReader.ReadPoc(reader, componentCount: 3));
        }

        [Test]
        public void ReadSot_ParsesFields()
        {
            var data = new byte[]
            {
                0x00, 0x0a,             // Lsot = 10 (Table A.5)
                0x00, 0x05,             // Isot
                0x00, 0x00, 0x01, 0x00, // Psot = 256
                0x01,                   // TPsot
                0x02,                   // TNsot
            };
            var reader = new JpxDataReader(data, 0, data.Length);

            var tilePart = JpxMarkerSegmentReader.ReadSot(reader);

            Assert.AreEqual(5, tilePart.TileIndex);
            Assert.AreEqual(256, tilePart.TileLength);
            Assert.AreEqual(1, tilePart.TilePartIndex);
            Assert.AreEqual(2, tilePart.TilePartCount);
        }

        private static JpxDataReader Segment(params byte[] payload)
        {
            var length = payload.Length + 2;
            var buffer = new byte[length];
            buffer[0] = (byte)(length >> 8);
            buffer[1] = (byte)length;
            payload.CopyTo(buffer, 2);
            return new JpxDataReader(buffer, 0, buffer.Length);
        }
    }
}
