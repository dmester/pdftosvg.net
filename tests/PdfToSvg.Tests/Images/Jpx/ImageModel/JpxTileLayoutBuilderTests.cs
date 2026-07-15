// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx;
using PdfToSvg.Imaging.Jpx.Codestream;
using PdfToSvg.Imaging.Jpx.ImageModel;

namespace PdfToSvg.Tests.Images.Jpx.ImageModel
{
    // ITU-T T.800 (06/2019) Annex B: division of the image into tiles (Equations B-5 to B-10) and
    // tile-components (Equation B-12), and division of a tile-component into resolution levels
    // (B.5), precincts (B.6) and code-blocks (B.7). The tile grid tests use the worked example
    // from Section B.4, whose expected values are stated in the spec text. All other expected
    // values below are computed by hand from Equations B-14 to B-18; the derivations are given
    // in comments.
    internal class JpxTileLayoutBuilderTests
    {
        [Test]
        public void BuildResolutionLevels_ResolutionLevelCoordinates()
        {
            // Equation B-14: trx0 = ceil(tcx0 / 2^(NL - r)) etc., with NL = 3 and
            // (tcx0, tcy0, tcx1, tcy1) = (13, 5, 97, 62):
            //   r = 3: [13, 97) x [5, 62)
            //   r = 2: [ceil(13/2), ceil(97/2)) x [ceil(5/2), ceil(62/2)) = [7, 49) x [3, 31)
            //   r = 1: [ceil(13/4), ceil(97/4)) x [ceil(5/4), ceil(62/4)) = [4, 25) x [2, 16)
            //   r = 0: [ceil(13/8), ceil(97/8)) x [ceil(5/8), ceil(62/8)) = [2, 13) x [1, 8)
            var resolutions = JpxTileLayoutBuilder.BuildResolutionLevels(13, 5, 97, 62, Cod(3), Quant(3));

            Assert.AreEqual(4, resolutions.Length);

            Assert.AreEqual(0, resolutions[0].Level);

            Assert.AreEqual(2, resolutions[0].TrX0);
            Assert.AreEqual(1, resolutions[0].TrY0);
            Assert.AreEqual(13, resolutions[0].TrX1);
            Assert.AreEqual(8, resolutions[0].TrY1);

            Assert.AreEqual(4, resolutions[1].TrX0);
            Assert.AreEqual(2, resolutions[1].TrY0);
            Assert.AreEqual(25, resolutions[1].TrX1);
            Assert.AreEqual(16, resolutions[1].TrY1);

            Assert.AreEqual(7, resolutions[2].TrX0);
            Assert.AreEqual(3, resolutions[2].TrY0);
            Assert.AreEqual(49, resolutions[2].TrX1);
            Assert.AreEqual(31, resolutions[2].TrY1);

            Assert.AreEqual(13, resolutions[3].TrX0);
            Assert.AreEqual(5, resolutions[3].TrY0);
            Assert.AreEqual(97, resolutions[3].TrX1);
            Assert.AreEqual(62, resolutions[3].TrY1);
        }

        [Test]
        public void BuildResolutionLevels_SubBandCoordinatesAllBandTypes()
        {
            // NL = 1, (tcx0, tcy0, tcx1, tcy1) = (3, 5, 12, 13). Equation B-15 with nb = 1:
            //   low  x: [ceil(3/2), ceil(12/2))          = [2, 6)
            //   high x: [ceil((3-1)/2), ceil((12-1)/2))  = [1, 6)
            //   low  y: [ceil(5/2), ceil(13/2))          = [3, 7)
            //   high y: [ceil((5-1)/2), ceil((13-1)/2))  = [2, 6)
            var resolutions = JpxTileLayoutBuilder.BuildResolutionLevels(3, 5, 12, 13, Cod(1), Quant(1));

            var ll = resolutions[0].SubBands[0];
            Assert.AreEqual(JpxSubBandType.LL, ll.Type);
            Assert.AreEqual(1, ll.DecompositionLevel);
            Assert.AreEqual(2, ll.TbX0);
            Assert.AreEqual(3, ll.TbY0);
            Assert.AreEqual(6, ll.TbX1);
            Assert.AreEqual(7, ll.TbY1);
            Assert.AreEqual(0, ll.LocalX0);
            Assert.AreEqual(0, ll.LocalY0);

            Assert.AreEqual(3, resolutions[1].SubBands.Length);

            var hl = resolutions[1].SubBands[0];
            Assert.AreEqual(JpxSubBandType.HL, hl.Type);
            Assert.AreEqual(1, hl.DecompositionLevel);
            Assert.AreEqual(1, hl.TbX0);
            Assert.AreEqual(3, hl.TbY0);
            Assert.AreEqual(6, hl.TbX1);
            Assert.AreEqual(7, hl.TbY1);

            var lh = resolutions[1].SubBands[1];
            Assert.AreEqual(JpxSubBandType.LH, lh.Type);
            Assert.AreEqual(2, lh.TbX0);
            Assert.AreEqual(2, lh.TbY0);
            Assert.AreEqual(6, lh.TbX1);
            Assert.AreEqual(6, lh.TbY1);

            var hh = resolutions[1].SubBands[2];
            Assert.AreEqual(JpxSubBandType.HH, hh.Type);
            Assert.AreEqual(1, hh.TbX0);
            Assert.AreEqual(2, hh.TbY0);
            Assert.AreEqual(6, hh.TbX1);
            Assert.AreEqual(6, hh.TbY1);

            // Split layout offsets: the high-pass bands are placed after the low-pass part,
            // whose size is the resolution level 0 size 4x4.
            Assert.AreEqual(4, hl.LocalX0);
            Assert.AreEqual(0, hl.LocalY0);
            Assert.AreEqual(0, lh.LocalX0);
            Assert.AreEqual(4, lh.LocalY0);
            Assert.AreEqual(4, hh.LocalX0);
            Assert.AreEqual(4, hh.LocalY0);

            // Interleaving consistency: resolution level 1 is 9x8, low part 4x4,
            // so the high parts must be 5 and 4 samples wide/high.
            Assert.AreEqual(5, hl.Width);
            Assert.AreEqual(4, lh.Height);
        }

        [Test]
        public void BuildResolutionLevels_ExpoundedQuantizationOrder()
        {
            // ITU-T T.800 (06/2019) Section A.6.4: values are signalled in the order
            // NL-LL, then HL, LH, HH per decomposition level starting at the highest level
            // (resolution level 1). Equation E-2: Mb = G + eb - 1 with G = 2.
            var exponents = new[] { 9, 10, 11, 12, 6, 7, 8 };
            var resolutions = JpxTileLayoutBuilder.BuildResolutionLevels(0, 0, 64, 64, Cod(2), Quant(2, 2, exponents));

            Assert.AreEqual(9, resolutions[0].SubBands[0].Exponent);   // LL
            Assert.AreEqual(10, resolutions[1].SubBands[0].Exponent);  // 2HL
            Assert.AreEqual(11, resolutions[1].SubBands[1].Exponent);  // 2LH
            Assert.AreEqual(12, resolutions[1].SubBands[2].Exponent);  // 2HH
            Assert.AreEqual(6, resolutions[2].SubBands[0].Exponent);   // 1HL
            Assert.AreEqual(7, resolutions[2].SubBands[1].Exponent);   // 1LH
            Assert.AreEqual(8, resolutions[2].SubBands[2].Exponent);   // 1HH

            Assert.AreEqual(10, resolutions[0].SubBands[0].MagnitudeBitPlanes); // 2 + 9 - 1
            Assert.AreEqual(13, resolutions[1].SubBands[2].MagnitudeBitPlanes); // 2 + 12 - 1
            Assert.AreEqual(7, resolutions[2].SubBands[0].MagnitudeBitPlanes);  // 2 + 6 - 1
        }

        [Test]
        public void BuildResolutionLevels_ScalarDerivedExponents()
        {
            // ITU-T T.800 (06/2019) Equation E-5: eb = e0 - NL + nb, ub = u0.
            // NL = 2, e0 = 13, u0 = 99, G = 1:
            //   LL (nb = 2):            e = 13 - 2 + 2 = 13, Mb = 1 + 13 - 1 = 13
            //   r = 1 bands (nb = 2):   e = 13,              Mb = 13
            //   r = 2 bands (nb = 1):   e = 13 - 2 + 1 = 12, Mb = 12
            var quantization = new JpxQuantizationDefaults
            {
                ScalarDerived = true,
                NumberOfGuardBits = 1,
                Values = new[] { new JpxQuantizationDefaultValues { Exponent = 13, Mantissa = 99 } },
            };

            var resolutions = JpxTileLayoutBuilder.BuildResolutionLevels(0, 0, 64, 64, Cod(2), quantization);

            Assert.AreEqual(13, resolutions[0].SubBands[0].Exponent);
            Assert.AreEqual(13, resolutions[0].SubBands[0].MagnitudeBitPlanes);
            Assert.AreEqual(13, resolutions[1].SubBands[0].Exponent);
            Assert.AreEqual(13, resolutions[1].SubBands[2].MagnitudeBitPlanes);
            Assert.AreEqual(12, resolutions[2].SubBands[1].Exponent);
            Assert.AreEqual(12, resolutions[2].SubBands[1].MagnitudeBitPlanes);
            Assert.AreEqual(99, resolutions[2].SubBands[1].Mantissa);
        }

        [Test]
        public void BuildResolutionLevels_InvalidQuantizationValueCountThrows()
        {
            // NL = 2 requires 1 + 3 * 2 = 7 values for non-derived quantization.
            var quantization = Quant(2);
            quantization.Values = new JpxQuantizationDefaultValues[5];

            Assert.Throws<JpxException>(() =>
                JpxTileLayoutBuilder.BuildResolutionLevels(0, 0, 64, 64, Cod(2), quantization));
        }

        [Test]
        public void BuildResolutionLevels_SharedQcdAllowsValuesForMoreDecompositions()
        {
            // A shared QCD has seven values for another component with NL = 2. This component has NL = 1 and uses
            // only the first four values (ITU-T T.800 (06/2019) Section A.6.4).
            var resolutions = JpxTileLayoutBuilder.BuildResolutionLevels(0, 0, 64, 64, Cod(1), Quant(2));

            Assert.AreEqual(2, resolutions.Length);
            Assert.AreEqual(1, resolutions[0].SubBands.Length);
            Assert.AreEqual(3, resolutions[1].SubBands.Length);
        }

        [Test]
        public void BuildResolutionLevels_ComponentQccRejectsExtraValues()
        {
            var quantization = Quant(2);
            quantization.ComponentSpecific = true;

            Assert.Throws<JpxException>(() =>
                JpxTileLayoutBuilder.BuildResolutionLevels(0, 0, 64, 64, Cod(1), quantization));
        }

        [Test]
        public void BuildResolutionLevels_CodeBlockPartitionClippedToSubBand()
        {
            // NL = 0: the single LL band has the tile-component coordinates [5, 21) x [3, 11).
            // Code-blocks 4x4, anchored at (0, 0) (Section B.7):
            //   grid x: floor(5/4) = 1 .. ceil(21/4) = 6  => 5 columns
            //   grid y: floor(3/4) = 0 .. ceil(11/4) = 3  => 3 rows
            var resolutions = JpxTileLayoutBuilder.BuildResolutionLevels(
                5, 3, 21, 11, Cod(0, codeBlockWidth: 4, codeBlockHeight: 4), Quant(0));

            var ll = resolutions[0].SubBands[0];
            Assert.AreEqual(4, ll.CodeBlockWidth);
            Assert.AreEqual(4, ll.CodeBlockHeight);
            Assert.AreEqual(1, ll.CodeBlockGridX0);
            Assert.AreEqual(0, ll.CodeBlockGridY0);
            Assert.AreEqual(5, ll.CodeBlockCountX);
            Assert.AreEqual(3, ll.CodeBlockCountY);
            Assert.AreEqual(15, ll.CodeBlocks.Length);

            // First code-block (grid 1, 0) is clipped on the left and top: [5, 8) x [3, 4).
            var first = ll.CodeBlocks[0];
            Assert.AreEqual(1, first.GridX);
            Assert.AreEqual(0, first.GridY);
            Assert.AreEqual(5, first.X0);
            Assert.AreEqual(3, first.Y0);
            Assert.AreEqual(8, first.X1);
            Assert.AreEqual(4, first.Y1);
            Assert.AreEqual(3, first.Width);
            Assert.AreEqual(1, first.Height);
            Assert.AreEqual(0, first.LocalX0);
            Assert.AreEqual(0, first.LocalY0);

            // Second code-block (grid 2, 0) starts at the partition boundary x = 8.
            var second = ll.CodeBlocks[1];
            Assert.AreEqual(8, second.X0);
            Assert.AreEqual(12, second.X1);
            Assert.AreEqual(3, second.LocalX0);

            // Last code-block (grid 5, 2) is clipped on the right: [20, 21) x [8, 11).
            var last = ll.CodeBlocks[14];
            Assert.AreEqual(5, last.GridX);
            Assert.AreEqual(2, last.GridY);
            Assert.AreEqual(20, last.X0);
            Assert.AreEqual(8, last.Y0);
            Assert.AreEqual(21, last.X1);
            Assert.AreEqual(11, last.Y1);
            Assert.AreEqual(15, last.LocalX0);
            Assert.AreEqual(5, last.LocalY0);
        }

        [Test]
        public void BuildResolutionLevels_CodeBlockSizeBoundedByPrecinctSize()
        {
            // Equations B-17/B-18: xcb' = min(xcb, PPx) for r = 0 and min(xcb, PPx - 1) for
            // r > 0. With 64x64 code-blocks (xcb = 6) and PPx = PPy = 3:
            //   r = 0: 2^min(6, 3) = 8
            //   r = 1: 2^min(6, 2) = 4
            var resolutions = JpxTileLayoutBuilder.BuildResolutionLevels(
                0, 0, 40, 40, Cod(1, ppx: new[] { 3, 3 }, ppy: new[] { 3, 3 }), Quant(1));

            Assert.AreEqual(8, resolutions[0].SubBands[0].CodeBlockWidth);
            Assert.AreEqual(8, resolutions[0].SubBands[0].CodeBlockHeight);
            Assert.AreEqual(4, resolutions[1].SubBands[0].CodeBlockWidth);
            Assert.AreEqual(4, resolutions[1].SubBands[2].CodeBlockHeight);
        }

        [Test]
        public void BuildResolutionLevels_PrecinctPartition()
        {
            // NL = 1, (tcx0, tcy0, tcx1, tcy1) = (3, 5, 12, 13), code-blocks 4x4 (xcb = 2),
            // precinct sizes PPx = PPy = 1 for r = 0 and PPx = PPy = 2 for r = 1.
            //
            // r = 1 is [3, 12) x [5, 13) with 4x4 precincts (Equation B-16):
            //   grid x: floor(3/4) = 0 .. ceil(12/4) = 3 => 3 columns
            //   grid y: floor(5/4) = 1 .. ceil(13/4) = 4 => 3 rows
            var resolutions = JpxTileLayoutBuilder.BuildResolutionLevels(
                3, 5, 12, 13,
                Cod(1, codeBlockWidth: 4, codeBlockHeight: 4, ppx: new[] { 1, 2 }, ppy: new[] { 1, 2 }),
                Quant(1));

            var r1 = resolutions[1];
            Assert.AreEqual(4, r1.PrecinctWidth);
            Assert.AreEqual(4, r1.PrecinctHeight);
            Assert.AreEqual(0, r1.PrecinctGridX0);
            Assert.AreEqual(1, r1.PrecinctGridY0);
            Assert.AreEqual(3, r1.PrecinctCountX);
            Assert.AreEqual(3, r1.PrecinctCountY);
            Assert.AreEqual(9, r1.Precincts.Length);

            // Precinct 0 is grid cell (0, 1) = [0, 4) x [4, 8), clipped to [3, 4) x [5, 8).
            var precinct0 = r1.Precincts[0];
            Assert.AreEqual(0, precinct0.Index);
            Assert.AreEqual(0, precinct0.GridX);
            Assert.AreEqual(1, precinct0.GridY);
            Assert.AreEqual(3, precinct0.X0);
            Assert.AreEqual(5, precinct0.Y0);
            Assert.AreEqual(4, precinct0.X1);
            Assert.AreEqual(8, precinct0.Y1);

            // For r = 1 the 4x4 precinct projects to 2x2 in the sub-band domain, and code-blocks
            // are 2x2 (xcb' = min(2, PPx - 1) = 1). The HL band is [1, 6) x [3, 7) with grid
            // x: floor(1/2) = 0 .. ceil(6/2) = 3, y: floor(3/2) = 1 .. ceil(7/2) = 4.
            var hl = r1.SubBands[0];
            Assert.AreEqual(2, hl.CodeBlockWidth);
            Assert.AreEqual(0, hl.CodeBlockGridX0);
            Assert.AreEqual(1, hl.CodeBlockGridY0);
            Assert.AreEqual(3, hl.CodeBlockCountX);
            Assert.AreEqual(3, hl.CodeBlockCountY);

            // Precinct grid cell (1, 2) covers [2, 4) x [4, 6) in the HL band domain, which is
            // code-block grid range [1, 2) x [2, 3).
            var precinct4 = r1.Precincts[4];
            Assert.AreEqual(1, precinct4.GridX);
            Assert.AreEqual(2, precinct4.GridY);
            Assert.AreEqual(1, precinct4.SubBands[0].CodeBlockX0);
            Assert.AreEqual(2, precinct4.SubBands[0].CodeBlockY0);
            Assert.AreEqual(2, precinct4.SubBands[0].CodeBlockX1);
            Assert.AreEqual(3, precinct4.SubBands[0].CodeBlockY1);
            Assert.AreEqual(1, precinct4.SubBands[0].CodeBlockCountX);
            Assert.AreEqual(1, precinct4.SubBands[0].CodeBlockCountY);

            // r = 0 is the LL band [2, 6) x [3, 7) with 2x2 precincts:
            //   grid x: floor(2/2) = 1 .. ceil(6/2) = 3 => 2 columns
            //   grid y: floor(3/2) = 1 .. ceil(7/2) = 4 => 3 rows
            var r0 = resolutions[0];
            Assert.AreEqual(2, r0.PrecinctCountX);
            Assert.AreEqual(3, r0.PrecinctCountY);
            Assert.AreEqual(6, r0.Precincts.Length);

            // At r = 0 the precinct is not halved in the band domain. Precinct grid cell (1, 1)
            // covers [2, 4) x [2, 4), which with 2x2 code-blocks (xcb' = min(2, PPx) = 1) is
            // code-block grid range [1, 2) x [1, 2).
            var r0Precinct0 = r0.Precincts[0];
            Assert.AreEqual(1, r0Precinct0.GridX);
            Assert.AreEqual(1, r0Precinct0.GridY);
            Assert.AreEqual(1, r0Precinct0.SubBands[0].CodeBlockX0);
            Assert.AreEqual(1, r0Precinct0.SubBands[0].CodeBlockY0);
            Assert.AreEqual(2, r0Precinct0.SubBands[0].CodeBlockX1);
            Assert.AreEqual(2, r0Precinct0.SubBands[0].CodeBlockY1);
        }

        [Test]
        public void BuildResolutionLevels_EmptyResolutionHasNoPrecincts()
        {
            // NL = 2, (tcx0, tcy0, tcx1, tcy1) = (7, 0, 8, 4). Equation B-14:
            //   r = 0: x [ceil(7/4), ceil(8/4)) = [2, 2) => empty
            //   r = 1: x [ceil(7/2), ceil(8/2)) = [4, 4) => empty
            //   r = 2: x [7, 8) => 1 sample wide
            // Equation B-16 gives numprecincts = 0 for the empty resolution levels.
            var resolutions = JpxTileLayoutBuilder.BuildResolutionLevels(7, 0, 8, 4, Cod(2), Quant(2));

            Assert.AreEqual(0, resolutions[0].PrecinctCountX);
            Assert.AreEqual(0, resolutions[0].Precincts.Length);
            Assert.AreEqual(0, resolutions[0].SubBands[0].CodeBlocks.Length);

            Assert.AreEqual(0, resolutions[1].PrecinctCountX);
            Assert.AreEqual(0, resolutions[1].Precincts.Length);

            Assert.AreEqual(1, resolutions[2].PrecinctCountX);
            Assert.AreEqual(1, resolutions[2].PrecinctCountY);
            Assert.AreEqual(1, resolutions[2].Precincts.Length);

            // Equation B-15 at r = 2 (nb = 1):
            //   HL x: [ceil((7-1)/2), ceil((8-1)/2)) = [3, 4), LH x: [ceil(7/2), ceil(8/2)) = [4, 4)
            // The one sample wide column ends up in the HL/HH bands; LH is empty.
            var hl = resolutions[2].SubBands[0];
            Assert.AreEqual(1, hl.CodeBlocks.Length);
            Assert.AreEqual(3, hl.CodeBlocks[0].X0);
            Assert.AreEqual(4, hl.CodeBlocks[0].X1);
            Assert.AreEqual(0, hl.CodeBlocks[0].Y0);
            Assert.AreEqual(2, hl.CodeBlocks[0].Y1);

            var lh = resolutions[2].SubBands[1];
            Assert.AreEqual(0, lh.Width);
            Assert.AreEqual(0, lh.CodeBlocks.Length);
        }

        [Test]
        public void BuildResolutionLevels_TooManyCodeBlocksThrows()
        {
            // PPx = PPy = 0 is allowed for r = 0 and forces 1x1 code-blocks
            // (Equation B-17), giving 600 * 600 = 360000 code-blocks > the guard limit.
            Assert.Throws<JpxException>(() =>
                JpxTileLayoutBuilder.BuildResolutionLevels(
                    0, 0, 600, 600, Cod(0, ppx: new[] { 0 }, ppy: new[] { 0 }), Quant(0)));
        }

        [Test]
        public void Build_SpecExampleTileGrid()
        {
            // Equation B-5: numXtiles = ceil(1432/396) = 4, numYtiles = ceil(954/297) = 4.
            // Section B.4 gives tx0(0:3, *) = {152, 396, 792, 1188}, tx1(0:3, *) = {396, 792,
            // 1188, 1432}, ty0(*, 0:3) = {234, 297, 594, 891}, ty1(*, 0:3) = {297, 594, 891, 954}.
            var tiles = JpxTileLayoutBuilder.Build(SpecExampleImage(), Parameters(2));

            Assert.AreEqual(16, tiles.Length);

            Assert.AreEqual(0, tiles[0].Index);
            Assert.AreEqual(152, tiles[0].TX0);
            Assert.AreEqual(234, tiles[0].TY0);
            Assert.AreEqual(396, tiles[0].TX1);
            Assert.AreEqual(297, tiles[0].TY1);

            // Tile 5 is grid position (p, q) = (1, 1) per Equation B-6.
            Assert.AreEqual(396, tiles[5].TX0);
            Assert.AreEqual(297, tiles[5].TY0);
            Assert.AreEqual(792, tiles[5].TX1);
            Assert.AreEqual(594, tiles[5].TY1);

            // The last tile is clipped by the image area (Equations B-7 to B-10).
            Assert.AreEqual(1188, tiles[15].TX0);
            Assert.AreEqual(891, tiles[15].TY0);
            Assert.AreEqual(1432, tiles[15].TX1);
            Assert.AreEqual(954, tiles[15].TY1);
        }

        [Test]
        public void Build_SpecExampleTileComponents()
        {
            var tiles = JpxTileLayoutBuilder.Build(SpecExampleImage(), Parameters(2));

            // Component 0 (XRsiz = 1): tile-component coordinates equal the tile coordinates.
            var tile0Component0 = tiles[0].Components[0];
            Assert.AreEqual(152, tile0Component0.TcX0);
            Assert.AreEqual(234, tile0Component0.TcY0);
            Assert.AreEqual(396, tile0Component0.TcX1);
            Assert.AreEqual(297, tile0Component0.TcY1);
            Assert.AreEqual(244, tile0Component0.Width);
            Assert.AreEqual(63, tile0Component0.Height);

            // Component 1 (XRsiz = 2), Equation B-12: tile 0 is
            // [ceil(152/2), ceil(396/2)) x [ceil(234/2), ceil(297/2)) = [76, 198) x [117, 149).
            var tile0Component1 = tiles[0].Components[1];
            Assert.AreEqual(76, tile0Component1.TcX0);
            Assert.AreEqual(117, tile0Component1.TcY0);
            Assert.AreEqual(198, tile0Component1.TcX1);
            Assert.AreEqual(149, tile0Component1.TcY1);

            // Section B.4: in component 1, interior tiles (1, 1) and (2, 1) are 198x148 while
            // tiles (1, 2) and (2, 2) are 198x149 due to the sub-sampling.
            var tile5Component1 = tiles[5].Components[1];
            Assert.AreEqual(198, tile5Component1.Width);
            Assert.AreEqual(148, tile5Component1.Height);

            var tile9Component1 = tiles[9].Components[1];
            Assert.AreEqual(198, tile9Component1.Width);
            Assert.AreEqual(149, tile9Component1.Height);
        }

        [Test]
        public void BuildTile_SingleTileMatchesBuild()
        {
            var image = SpecExampleImage();
            var parameters = Parameters(2, decompositionLevels: 2);

            var all = JpxTileLayoutBuilder.Build(image, parameters);
            var single = JpxTileLayoutBuilder.BuildTile(image, 5, parameters);

            Assert.AreEqual(all[5].TX0, single.TX0);
            Assert.AreEqual(all[5].TY0, single.TY0);
            Assert.AreEqual(all[5].TX1, single.TX1);
            Assert.AreEqual(all[5].TY1, single.TY1);
            Assert.AreEqual(all[5].Components[1].TcX0, single.Components[1].TcX0);
            Assert.AreEqual(all[5].Components[1].ResolutionLevels.Length, single.Components[1].ResolutionLevels.Length);
        }

        [Test]
        public void BuildTile_PropagatesResolvedParameters()
        {
            var parameters = Parameters(2);
            parameters.RegionOfInterestShifts[1] = 11;

            var tile = JpxTileLayoutBuilder.BuildTile(SpecExampleImage(), 0, parameters);

            Assert.AreSame(parameters.ComponentCodingStyles[0], tile.Components[0].CodingStyle);
            Assert.AreSame(parameters.ComponentQuantizations[1], tile.Components[1].Quantization);
            Assert.AreEqual(0, tile.Components[0].RegionOfInterestShift);
            Assert.AreEqual(11, tile.Components[1].RegionOfInterestShift);
        }

        [Test]
        public void BuildTile_InvalidTileIndexThrows()
        {
            Assert.Throws<JpxException>(() =>
                JpxTileLayoutBuilder.BuildTile(SpecExampleImage(), 16, Parameters(2)));
        }

        [Test]
        public void BuildTile_ComponentCountMismatchThrows()
        {
            // Two components in the SIZ data, but parameters resolved for a single component.
            Assert.Throws<JpxException>(() =>
                JpxTileLayoutBuilder.BuildTile(SpecExampleImage(), 0, Parameters(1)));
        }

        [Test]
        public void Build_TooManyTilesThrows()
        {
            // 2x2 tiles over a 10000x2 image area give 5000 tiles, above the guard limit.
            var image = Image(10000, 2, 0, 0, 2, 2, 0, 0, 1);

            Assert.Throws<JpxException>(() =>
                JpxTileLayoutBuilder.Build(image, Parameters(1)));
        }

        [Test]
        public void BuildTile_NoComponentsThrows()
        {
            var image = Image(10, 10, 0, 0, 10, 10, 0, 0); // zero components

            Assert.Throws<JpxException>(() =>
                JpxTileLayoutBuilder.BuildTile(image, 0, Parameters(0)));
        }

        private static JpxImageInfo Image(
            int xsiz, int ysiz, int xosiz, int yosiz,
            int xtsiz, int ytsiz, int xtosiz, int ytosiz,
            params int[] subSampling)
        {
            var components = new JpxComponent[subSampling.Length];
            for (var c = 0; c < components.Length; c++)
            {
                components[c] = new JpxComponent
                {
                    Ssizi = 7, // 8 bit unsigned
                    XRsizi = subSampling[c],
                    YRsizi = subSampling[c],
                };
            }

            return new JpxImageInfo
            {
                Xsiz = xsiz,
                Ysiz = ysiz,
                XOsiz = xosiz,
                YOsiz = yosiz,
                XTsiz = xtsiz,
                YTsiz = ytsiz,
                XTOsiz = xtosiz,
                YTOsiz = ytosiz,
                Components = components,
            };
        }

        private static JpxResolvedTileParameters Parameters(int componentCount, int decompositionLevels = 0)
        {
            var codingStyles = new JpxCodingStyleDefaults[componentCount];
            var quantizations = new JpxQuantizationDefaults[componentCount];
            var roiShifts = new int[componentCount];

            for (var c = 0; c < componentCount; c++)
            {
                codingStyles[c] = Cod(decompositionLevels);
                quantizations[c] = Quant(decompositionLevels);
            }

            return new JpxResolvedTileParameters(
                progressionOrder: JpxCodingProgressionOrder.LayerResolutionLevelComponentPosition,
                numberOfLayers: 1,
                multipleComponentTransformation: false,
                sopMarkerSegments: false,
                ephMarkerSegments: false,
                progressionOrderChanges: null,
                componentCodingStyles: codingStyles,
                componentQuantizations: quantizations,
                regionOfInterestShifts: roiShifts);
        }

        // The example from ITU-T T.800 (06/2019) Section B.4:
        // (Xsiz, Ysiz) = (1432, 954),
        // (XOsiz, YOsiz) = (152, 234),
        // (XTsiz, YTsiz) = (396, 297),
        // (XTOsiz, YTOsiz) = (0, 0),
        // two components with (XRsiz, YRsiz) = (1, 1) and (2, 2).
        private static JpxImageInfo SpecExampleImage() =>
            Image(1432, 954, 152, 234, 396, 297, 0, 0, 1, 2);

        private static JpxCodingStyleDefaults Cod(
            int decompositionLevels,
            int codeBlockWidth = 64,
            int codeBlockHeight = 64,
            int[] ppx = null,
            int[] ppy = null)
        {
            var precincts = new JpxPrecinctSize[decompositionLevels + 1];
            for (var r = 0; r < precincts.Length; r++)
            {
                precincts[r] = new JpxPrecinctSize
                {
                    PPx = ppx == null ? 15 : ppx[r],
                    PPy = ppy == null ? 15 : ppy[r],
                };
            }

            return new JpxCodingStyleDefaults
            {
                NumberOfDecompositionLevels = decompositionLevels,
                CodeBlockWidth = codeBlockWidth,
                CodeBlockHeight = codeBlockHeight,
                PrecinctSize = precincts,
            };
        }

        // Expounded style quantization (one value for the NL-LL band plus three values per
        // decomposition level, ITU-T T.800 (06/2019) Section A.6.4).
        private static JpxQuantizationDefaults Quant(int decompositionLevels, int guardBits = 2, int[] exponents = null)
        {
            var values = new JpxQuantizationDefaultValues[1 + decompositionLevels * 3];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = new JpxQuantizationDefaultValues
                {
                    Exponent = exponents == null ? 9 : exponents[i],
                    Mantissa = 0,
                };
            }

            return new JpxQuantizationDefaults
            {
                NumberOfGuardBits = guardBits,
                Values = values,
            };
        }
    }
}
