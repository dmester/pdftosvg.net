// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx.Codestream;
using PdfToSvg.Imaging.Jpx.ImageModel;
using PdfToSvg.Imaging.Jpx.Packets;
using System.Collections.Generic;

namespace PdfToSvg.Tests.Images.Jpx.Packets
{
    // ITU-T T.800 (06/2019) Section B.12: packet progression order. The expected sequences below are derived by hand
    // from the loop pseudocode of Sections B.12.1.1 to B.12.1.5; the derivations are given in comments at each test.
    internal class JpxPacketProgressionEnumeratorTests
    {
        private static JpxCodingStyleDefaults CodingStyle(int decompositionLevels, params int[] precinctExponents)
        {
            var precincts = new JpxPrecinctSize[decompositionLevels + 1];
            for (var r = 0; r < precincts.Length; r++)
            {
                var pp = precinctExponents.Length == 0 ? 15 : precinctExponents[r];
                precincts[r] = new JpxPrecinctSize { PPx = pp, PPy = pp };
            }

            return new JpxCodingStyleDefaults
            {
                NumberOfDecompositionLevels = decompositionLevels,
                CodeBlockWidth = 64,
                CodeBlockHeight = 64,
                PrecinctSize = precincts,
            };
        }

        private static JpxQuantizationDefaults Quantization(int decompositionLevels)
        {
            var values = new JpxQuantizationDefaultValues[1 + decompositionLevels * 3];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = new JpxQuantizationDefaultValues { Exponent = 9, Mantissa = 0 };
            }

            return new JpxQuantizationDefaults { NumberOfGuardBits = 2, Values = values };
        }

        private static JpxResolvedTileParameters Parameters(
            JpxCodingProgressionOrder progressionOrder,
            int layers,
            List<JpxProgressionVolume> progressionOrderChanges,
            params JpxCodingStyleDefaults[] componentCodingStyles)
        {
            var quantizations = new JpxQuantizationDefaults[componentCodingStyles.Length];
            for (var i = 0; i < quantizations.Length; i++)
            {
                quantizations[i] = Quantization(componentCodingStyles[i].NumberOfDecompositionLevels);
            }

            return new JpxResolvedTileParameters(
                progressionOrder,
                layers,
                multipleComponentTransformation: false,
                sopMarkerSegments: false,
                ephMarkerSegments: false,
                progressionOrderChanges,
                componentCodingStyles,
                quantizations,
                new int[componentCodingStyles.Length]);
        }

        /// <summary>Single-tile image; components are (XRsiz, YRsiz) pairs.</summary>
        private static JpxImageInfo Image(int xsiz, int ysiz, int xosiz, int yosiz, params int[] subSampling)
        {
            var components = new JpxComponent[subSampling.Length / 2];
            for (var i = 0; i < components.Length; i++)
            {
                components[i] = new JpxComponent
                {
                    Ssizi = 7,
                    XRsizi = subSampling[i * 2],
                    YRsizi = subSampling[i * 2 + 1],
                };
            }

            return new JpxImageInfo
            {
                Xsiz = xsiz,
                Ysiz = ysiz,
                XOsiz = xosiz,
                YOsiz = yosiz,
                XTsiz = xsiz,
                YTsiz = ysiz,
                XTOsiz = 0,
                YTOsiz = 0,
                Components = components,
            };
        }

        private static string[] Enumerate(JpxImageInfo image, JpxResolvedTileParameters parameters)
        {
            var tile = JpxTileLayoutBuilder.BuildTile(image, 0, parameters);
            var keys = new List<JpxPacketKey>(
                JpxPacketProgressionEnumerator.Enumerate(tile, image.Components, parameters));

            var result = new string[keys.Count];
            for (var i = 0; i < keys.Count; i++)
            {
                result[i] = "c" + keys[i].Component + "r" + keys[i].Resolution +
                    "p" + keys[i].Precinct + "l" + keys[i].Layer;
            }
            return result;
        }

        // Reference configuration used by the five progression order tests:
        //
        //   32x32 image, one tile, 2 layers.
        //   Component 0: XRsiz = YRsiz = 1, NL = 1, PPx = PPy = 3 for r = 0 and 4 for r = 1.
        //     tile-component [0,32)x[0,32)
        //     r = 0: [0,16)^2  (Equation B-14), precincts 2^3 = 8  => 2x2 = 4 precincts
        //     r = 1: [0,32)^2,                  precincts 2^4 = 16 => 2x2 = 4 precincts
        //   Component 1: XRsiz = YRsiz = 2, NL = 1, same precinct exponents.
        //     tile-component [0,16)x[0,16)
        //     r = 0: [0,8)^2,  precincts 8  => 1 precinct
        //     r = 1: [0,16)^2, precincts 16 => 1 precinct
        //
        // Reference grid positions used by B.12.1.3-B.12.1.5 (one r-level sample covers
        // XRsiz * 2^(NL - r) grid columns):
        //   c0 r0: precinct boundaries every 8 * 1 * 2 = 16  => (y, x) = (0,0) (0,16) (16,0) (16,16)
        //   c0 r1: every 16 * 1 * 1 = 16                     => (0,0) (0,16) (16,0) (16,16)
        //   c1 r0: every 8 * 2 * 2 = 32                      => (0,0)
        //   c1 r1: every 16 * 2 * 1 = 32                     => (0,0)
        private static JpxImageInfo TwoComponentImage() => Image(32, 32, 0, 0, 1, 1, 2, 2);

        private static JpxResolvedTileParameters TwoComponentParameters(JpxCodingProgressionOrder order) =>
            Parameters(order, layers: 2, null, CodingStyle(1, 3, 4), CodingStyle(1, 3, 4));

        [Test]
        public void Enumerate_LayerResolutionComponentPosition()
        {
            // B.12.1.1: for l, for r, for i, for k (precincts in raster order).
            var keys = Enumerate(
                TwoComponentImage(),
                TwoComponentParameters(JpxCodingProgressionOrder.LayerResolutionLevelComponentPosition));

            Assert.AreEqual(new[]
            {
                "c0r0p0l0", "c0r0p1l0", "c0r0p2l0", "c0r0p3l0", "c1r0p0l0",
                "c0r1p0l0", "c0r1p1l0", "c0r1p2l0", "c0r1p3l0", "c1r1p0l0",
                "c0r0p0l1", "c0r0p1l1", "c0r0p2l1", "c0r0p3l1", "c1r0p0l1",
                "c0r1p0l1", "c0r1p1l1", "c0r1p2l1", "c0r1p3l1", "c1r1p0l1",
            }, keys);
        }

        [Test]
        public void Enumerate_ResolutionLayerComponentPosition()
        {
            // B.12.1.2: for r, for l, for i, for k.
            var keys = Enumerate(
                TwoComponentImage(),
                TwoComponentParameters(JpxCodingProgressionOrder.ResolutionLevelLayerComponentPosition));

            Assert.AreEqual(new[]
            {
                "c0r0p0l0", "c0r0p1l0", "c0r0p2l0", "c0r0p3l0", "c1r0p0l0",
                "c0r0p0l1", "c0r0p1l1", "c0r0p2l1", "c0r0p3l1", "c1r0p0l1",
                "c0r1p0l0", "c0r1p1l0", "c0r1p2l0", "c0r1p3l0", "c1r1p0l0",
                "c0r1p0l1", "c0r1p1l1", "c0r1p2l1", "c0r1p3l1", "c1r1p0l1",
            }, keys);
        }

        [Test]
        public void Enumerate_ResolutionPositionComponentLayer()
        {
            // B.12.1.3: for r, for y, for x, for i, then all layers of the precinct given by
            // Equation B-20. With the positions listed at TwoComponentImage, for each r the scan
            // hits (0,0) where both c0 p0 and c1 p0 are emitted (component order), then (0,16)
            // c0 p1, (16,0) c0 p2, (16,16) c0 p3.
            var keys = Enumerate(
                TwoComponentImage(),
                TwoComponentParameters(JpxCodingProgressionOrder.ResolutionLevelPositionComponentLayer));

            Assert.AreEqual(new[]
            {
                "c0r0p0l0", "c0r0p0l1", "c1r0p0l0", "c1r0p0l1",
                "c0r0p1l0", "c0r0p1l1",
                "c0r0p2l0", "c0r0p2l1",
                "c0r0p3l0", "c0r0p3l1",
                "c0r1p0l0", "c0r1p0l1", "c1r1p0l0", "c1r1p0l1",
                "c0r1p1l0", "c0r1p1l1",
                "c0r1p2l0", "c0r1p2l1",
                "c0r1p3l0", "c0r1p3l1",
            }, keys);
        }

        [Test]
        public void Enumerate_PositionComponentResolutionLayer()
        {
            // B.12.1.4: for y, for x, for i, for r, then all layers. At (0,0) all four
            // (component, resolution) pairs have a precinct boundary; component is the outer of
            // the two remaining loops.
            var keys = Enumerate(
                TwoComponentImage(),
                TwoComponentParameters(JpxCodingProgressionOrder.PositionComponentResolutionLevelLayer));

            Assert.AreEqual(new[]
            {
                "c0r0p0l0", "c0r0p0l1", "c0r1p0l0", "c0r1p0l1",
                "c1r0p0l0", "c1r0p0l1", "c1r1p0l0", "c1r1p0l1",
                "c0r0p1l0", "c0r0p1l1", "c0r1p1l0", "c0r1p1l1",
                "c0r0p2l0", "c0r0p2l1", "c0r1p2l0", "c0r1p2l1",
                "c0r0p3l0", "c0r0p3l1", "c0r1p3l0", "c0r1p3l1",
            }, keys);
        }

        [Test]
        public void Enumerate_ComponentPositionResolutionLayer()
        {
            // B.12.1.5: for i, for y, for x, for r, then all layers.
            var keys = Enumerate(
                TwoComponentImage(),
                TwoComponentParameters(JpxCodingProgressionOrder.ComponentPositionResolutionLevelLayer));

            Assert.AreEqual(new[]
            {
                "c0r0p0l0", "c0r0p0l1", "c0r1p0l0", "c0r1p0l1",
                "c0r0p1l0", "c0r0p1l1", "c0r1p1l0", "c0r1p1l1",
                "c0r0p2l0", "c0r0p2l1", "c0r1p2l0", "c0r1p2l1",
                "c0r0p3l0", "c0r0p3l1", "c0r1p3l0", "c0r1p3l1",
                "c1r0p0l0", "c1r0p0l1", "c1r1p0l0", "c1r1p0l1",
            }, keys);
        }

        [Test]
        public void Enumerate_UnalignedPrecinctsEmitAtTileEdge()
        {
            // B.12.1.3 second loop condition: when trx0 is not aligned to the precinct grid, the
            // first precinct column is emitted at x = tx0 rather than at its (out-of-tile)
            // boundary projection.
            //
            //   24x4 image with XOsiz = 4, one tile [4,24)x[0,4), 1 layer, NL = 0 for both
            //   components (XRsiz = YRsiz = 1).
            //   Component 0: PPx = PPy = 3 => precincts of 8: grid columns 0 [4,8), 1 [8,16),
            //     2 [16,24) => positions x = max(4, 0) = 4, 8, 16.
            //   Component 1: PPx = PPy = 4 => precincts of 16: grid columns 0 [4,16), 1 [16,24)
            //     => positions x = max(4, 0) = 4, 16.
            //
            // Scan order for r = 0: x = 4 (c0 p0, c1 p0), x = 8 (c0 p1), x = 16 (c0 p2, c1 p1).
            var keys = Enumerate(
                Image(24, 4, 4, 0, 1, 1, 1, 1),
                Parameters(
                    JpxCodingProgressionOrder.ResolutionLevelPositionComponentLayer,
                    layers: 1,
                    null,
                    CodingStyle(0, 3),
                    CodingStyle(0, 4)));

            Assert.AreEqual(new[]
            {
                "c0r0p0l0", "c1r0p0l0", "c0r0p1l0", "c0r0p2l0", "c1r0p1l0",
            }, keys);
        }

        [Test]
        public void Enumerate_ComponentsWithDifferentResolutionCounts()
        {
            // B.12: r = 0 corresponds to the NL-LL band of every component; components without a
            // given resolution level contribute no packets there. Component 0 has NL = 2,
            // component 1 has NL = 0, both with one precinct per resolution level (PPx = 15).
            var keys = Enumerate(
                Image(16, 16, 0, 0, 1, 1, 1, 1),
                Parameters(
                    JpxCodingProgressionOrder.LayerResolutionLevelComponentPosition,
                    layers: 1,
                    null,
                    CodingStyle(2),
                    CodingStyle(0)));

            Assert.AreEqual(new[]
            {
                "c0r0p0l0", "c1r0p0l0",
                "c0r1p0l0",
                "c0r2p0l0",
            }, keys);
        }

        [Test]
        public void EnumerateProgressionVolumes_DefaultVolumeWithoutPoc()
        {
            var image = TwoComponentImage();
            var parameters = TwoComponentParameters(JpxCodingProgressionOrder.ResolutionLevelLayerComponentPosition);
            var tile = JpxTileLayoutBuilder.BuildTile(image, 0, parameters);

            var volumes = JpxPacketProgressionEnumerator.EnumerateProgressionVolumes(tile, parameters);

            Assert.AreEqual(1, volumes.Count);
            Assert.AreEqual(0, volumes[0].ResolutionStart);
            Assert.AreEqual(2, volumes[0].ResolutionEnd);
            Assert.AreEqual(0, volumes[0].ComponentStart);
            Assert.AreEqual(2, volumes[0].ComponentEnd);
            Assert.AreEqual(2, volumes[0].LayerEnd);
            Assert.AreEqual(
                JpxCodingProgressionOrder.ResolutionLevelLayerComponentPosition,
                volumes[0].ProgressionOrder);
        }

        [Test]
        public void EnumerateProgressionVolumes_ClampsToTileAndResolvedLayerCount()
        {
            // Equation B-21: the POC bounds are additionally bounded by what exists in the tile.
            // The layer bound comes from the resolved per-tile layer count (2), not from the
            // volume's LYEpoc = 5.
            var image = TwoComponentImage();
            var poc = new List<JpxProgressionVolume>
            {
                new JpxProgressionVolume
                {
                    ResolutionStart = 1,
                    ResolutionEnd = 99,
                    ComponentStart = 0,
                    ComponentEnd = 99,
                    LayerEnd = 5,
                    ProgressionOrder = JpxCodingProgressionOrder.LayerResolutionLevelComponentPosition,
                },
            };
            var parameters = Parameters(
                JpxCodingProgressionOrder.ResolutionLevelLayerComponentPosition,
                layers: 2,
                poc,
                CodingStyle(1, 3, 4),
                CodingStyle(1, 3, 4));
            var tile = JpxTileLayoutBuilder.BuildTile(image, 0, parameters);

            var volumes = JpxPacketProgressionEnumerator.EnumerateProgressionVolumes(tile, parameters);

            Assert.AreEqual(1, volumes.Count);
            Assert.AreEqual(1, volumes[0].ResolutionStart);
            Assert.AreEqual(2, volumes[0].ResolutionEnd);
            Assert.AreEqual(2, volumes[0].ComponentEnd);
            Assert.AreEqual(2, volumes[0].LayerEnd);
        }

        [Test]
        public void Enumerate_SinglePocVolumeBoundsTheLoops()
        {
            // One volume covering only r = 0 in LRCP order; no packets for r = 1 are emitted.
            var poc = new List<JpxProgressionVolume>
            {
                new JpxProgressionVolume
                {
                    ResolutionStart = 0,
                    ResolutionEnd = 1,
                    ComponentStart = 0,
                    ComponentEnd = 2,
                    LayerEnd = 2,
                    ProgressionOrder = JpxCodingProgressionOrder.LayerResolutionLevelComponentPosition,
                },
            };

            var keys = Enumerate(
                TwoComponentImage(),
                Parameters(
                    JpxCodingProgressionOrder.ResolutionLevelLayerComponentPosition,
                    layers: 2,
                    poc,
                    CodingStyle(1, 3, 4),
                    CodingStyle(1, 3, 4)));

            Assert.AreEqual(new[]
            {
                "c0r0p0l0", "c0r0p1l0", "c0r0p2l0", "c0r0p3l0", "c1r0p0l0",
                "c0r0p0l1", "c0r0p1l1", "c0r0p2l1", "c0r0p3l1", "c1r0p0l1",
            }, keys);
        }

        [Test]
        public void Enumerate_OverlappingPocVolumesDeduplicateByValue()
        {
            // B.12.2: no packet is ever repeated. The first volume sends layer 0 of everything in
            // RLCP order; the second volume covers layers 0-1 of everything in LRCP order, but
            // its layer 0 packets were already emitted, so only its layer 1 packets appear.
            var poc = new List<JpxProgressionVolume>
            {
                new JpxProgressionVolume
                {
                    ResolutionStart = 0,
                    ResolutionEnd = 2,
                    ComponentStart = 0,
                    ComponentEnd = 2,
                    LayerEnd = 1,
                    ProgressionOrder = JpxCodingProgressionOrder.ResolutionLevelLayerComponentPosition,
                },
                new JpxProgressionVolume
                {
                    ResolutionStart = 0,
                    ResolutionEnd = 2,
                    ComponentStart = 0,
                    ComponentEnd = 2,
                    LayerEnd = 2,
                    ProgressionOrder = JpxCodingProgressionOrder.LayerResolutionLevelComponentPosition,
                },
            };

            var keys = Enumerate(
                TwoComponentImage(),
                Parameters(
                    JpxCodingProgressionOrder.ResolutionLevelLayerComponentPosition,
                    layers: 2,
                    poc,
                    CodingStyle(1, 3, 4),
                    CodingStyle(1, 3, 4)));

            Assert.AreEqual(new[]
            {
                // Volume 1, RLCP with L = 1:
                "c0r0p0l0", "c0r0p1l0", "c0r0p2l0", "c0r0p3l0", "c1r0p0l0",
                "c0r1p0l0", "c0r1p1l0", "c0r1p2l0", "c0r1p3l0", "c1r1p0l0",
                // Volume 2, LRCP with L = 2; the l = 0 packets are duplicates and are skipped:
                "c0r0p0l1", "c0r0p1l1", "c0r0p2l1", "c0r0p3l1", "c1r0p0l1",
                "c0r1p0l1", "c0r1p1l1", "c0r1p2l1", "c0r1p3l1", "c1r1p0l1",
            }, keys);
        }
    }
}
