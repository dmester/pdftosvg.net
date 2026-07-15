// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx.Codestream;
using PdfToSvg.Imaging.Jpx.ImageModel;
using PdfToSvg.Imaging.Jpx.Transforms;

namespace PdfToSvg.Tests.Images.Jpx.Transforms
{
    // ITU-T T.800 (06/2019) Annex F inverse discrete wavelet transform (5/3 reversible and 9/7 irreversible).
    // The expected sub-band inputs below were produced by an independent forward transform of a known signal
    // (per the Annex F lifting equations); a correct inverse must reproduce that signal.
    public class JpxWaveletTransformTests
    {
        [Test]
        public void Inverse_Reversible53RecoversSignalOriginZero()
        {
            // 4x4 tile, origin (0, 0). Sub-band coefficients are the exact forward 5/3 transform of
            // the expected signal below. The reversible transform must recover it bit-exactly.
            var split = new float[]
            {
                3, 8, 0, 2,
                1, 6, 0, 2,
                0, 0, 0, 0,
                -1, -1, 0, 0,
            };
            var expected = new float[]
            {
                3, 5, 7, 9,
                2, 4, 6, 8,
                1, 3, 5, 7,
                0, 2, 4, 6,
            };

            var component = BuildComponent(0, 0, 4, 4, 2, 2);
            JpxWaveletTransform.Inverse(component, split, decompositionLevels: 1, reversible: true);

            AssertExact(expected, split);
        }

        [Test]
        public void Inverse_Reversible53RecoversTwentyBitSignalExactly()
        {
            // Scale the independently transformed integer vector above by 2^16. The largest reconstructed sample is
            // 9 * 2^16 = 589824, which requires 20 bits and exercises the documented Float32 exactness boundary.
            const int Scale = 1 << 16;
            var split = new float[]
            {
                3 * Scale, 15 * Scale / 2, 0, 2 * Scale,
                3 * Scale / 4, 21 * Scale / 4, 0, 2 * Scale,
                0, 0, 0, 0,
                -1 * Scale, -1 * Scale, 0, 0,
            };
            var expected = new float[]
            {
                3 * Scale, 5 * Scale, 7 * Scale, 9 * Scale,
                2 * Scale, 4 * Scale, 6 * Scale, 8 * Scale,
                1 * Scale, 3 * Scale, 5 * Scale, 7 * Scale,
                0, 2 * Scale, 4 * Scale, 6 * Scale,
            };

            var component = BuildComponent(0, 0, 4, 4, 2, 2);
            JpxWaveletTransform.Inverse(component, split, decompositionLevels: 1, reversible: true);

            AssertExact(expected, split);
        }

        [Test]
        public void Inverse_Reversible53RecoversSignalOddWidth()
        {
            // 3x2 tile, origin (0, 0): odd width exercises the mismatched low/high band sizes.
            var split = new float[]
            {
                25, 45, 0,
                30, 30, 0,
            };
            var expected = new float[]
            {
                10, 20, 30,
                40, 50, 60,
            };

            var component = BuildComponent(0, 0, 3, 2, 2, 1);
            JpxWaveletTransform.Inverse(component, split, decompositionLevels: 1, reversible: true);

            AssertExact(expected, split);
        }

        [Test]
        public void Inverse_Reversible53RecoversSignalOddOrigin()
        {
            // 4x4 tile, origin (1, 1): odd origin flips the sub-sampling parity on both axes
            // (T.800 F.3.7), which changes which sub-band boundary samples are mirrored.
            var split = new float[]
            {
                7, 2, 2, 0,
                10, 5, 2, 0,
                1, 1, 0, 0,
                1, 1, 0, 0,
            };
            var expected = new float[]
            {
                8, 6, 4, 2,
                7, 5, 3, 1,
                9, 7, 5, 3,
                10, 8, 6, 4,
            };

            var component = BuildComponent(1, 1, 4, 4, 2, 2);
            JpxWaveletTransform.Inverse(component, split, decompositionLevels: 1, reversible: true);

            AssertExact(expected, split);
        }

        [Test]
        public void LiftingParameters_TableF4()
        {
            // The lifting parameters of the 9-7 irreversible filter, transcribed independently from the
            // approximate values column of ITU-T T.800 (06/2019) Table F.4 to guard against transcription errors
            // in either place. The constants are floats, so the comparison is exact.
            Assert.AreEqual(-1.586134342059924f, JpxWaveletTransform.Alpha);
            Assert.AreEqual(-0.052980118572961f, JpxWaveletTransform.Beta);
            Assert.AreEqual(0.882911075530934f, JpxWaveletTransform.Gamma);
            Assert.AreEqual(0.443506852043971f, JpxWaveletTransform.Delta);
            Assert.AreEqual(1.230174104914001f, JpxWaveletTransform.K);
        }

        [Test]
        public void Inverse_Irreversible97RecoversSignalWithinTolerance()
        {
            // 4x4 tile, origin (0, 0). Sub-band coefficients are the exact forward 9/7 irreversible
            // transform of the expected signal. Floating-point rounding means the recovery is only
            // approximate, so a small tolerance is used.
            var split = new float[]
            {
                3.226645f, 7.046709f, 0.134913f, 1.730174f,
                1.316614f, 5.136677f, 0.134913f, 1.730174f,
                -0.067456f, -0.067456f, 0.000000f, 0.000000f,
                -0.865087f, -0.865087f, 0.000000f, 0.000000f,
            };
            var expected = new float[]
            {
                3, 5, 7, 9,
                2, 4, 6, 8,
                1, 3, 5, 7,
                0, 2, 4, 6,
            };

            var component = BuildComponent(0, 0, 4, 4, 2, 2);
            JpxWaveletTransform.Inverse(component, split, decompositionLevels: 1, reversible: false);

            AssertClose(expected, split, 1e-3f);
        }

        [Test]
        public void Inverse_Reversible53T800AnnexJ10Example()
        {
            // The worked decoding example in ITU-T T.800 (06/2019) Section J.10: a 1x9 tile with one
            // decomposition level. The LL and 1LH coefficients are given in Section J.10.4, and Section J.10.5
            // gives the samples after the inverse 5-3 filter and level shifting. The level shift for the 8-bit
            // unsigned component adds 2^7 = 128 (Section G.1.2) and is applied outside the wavelet transform, so
            // it is subtracted from the J.10.5 samples here.
            var split = new float[]
            {
                -26, -22, -30, -32, -19, // LL
                1, 5, 1, 0,              // 1LH
            };
            var expected = new float[]
            {
                101 - 128, 103 - 128, 104 - 128, 105 - 128, 96 - 128, 97 - 128, 96 - 128, 102 - 128, 109 - 128,
            };

            var component = BuildComponent(0, 0, 1, 9, 1, 5);
            JpxWaveletTransform.Inverse(component, split, decompositionLevels: 1, reversible: true);

            AssertExact(expected, split);
        }

        // The example of discrete wavelet transformation in ITU-T T.800 (06/2019) Section J.4: a 13x17 tile
        // component (Table J.3) decomposed two levels with the 9-7 irreversible (Tables J.4 to J.10) and the 5-3
        // reversible (Tables J.11 to J.17) transformations. The inverse transformation of the spec sub-band
        // coefficients must reproduce the source samples.

        // Table J.3 - Source tile component samples (13x17)
        private static readonly float[] tableJ3SourceSamples =
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
            1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
            2, 2, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
            3, 3, 3, 4, 5, 5, 6, 7, 8, 9, 10, 11, 12,
            4, 4, 4, 5, 5, 6, 7, 8, 8, 9, 10, 11, 12,
            5, 5, 5, 5, 6, 7, 7, 8, 9, 10, 11, 12, 13,
            6, 6, 6, 6, 7, 7, 8, 9, 10, 10, 11, 12, 13,
            7, 7, 7, 7, 8, 8, 9, 9, 10, 11, 12, 13, 13,
            8, 8, 8, 8, 8, 9, 10, 10, 11, 12, 12, 13, 14,
            9, 9, 9, 9, 9, 10, 10, 11, 12, 12, 13, 14, 15,
            10, 10, 10, 10, 10, 11, 11, 12, 12, 13, 14, 14, 15,
            11, 11, 11, 11, 11, 12, 12, 13, 13, 14, 14, 15, 16,
            12, 12, 12, 12, 12, 13, 13, 13, 14, 15, 15, 16, 16,
            13, 13, 13, 13, 13, 13, 14, 14, 15, 15, 16, 17, 17,
            14, 14, 14, 14, 14, 14, 15, 15, 16, 16, 17, 17, 18,
            15, 15, 15, 15, 15, 15, 16, 16, 17, 17, 18, 18, 19,
            16, 16, 16, 16, 16, 16, 17, 17, 17, 18, 18, 19, 20,
        };

        [Test]
        public void Inverse_Reversible53T800AnnexJ4Example()
        {
            // The 5-3 reversible transformation produces integer coefficients, so Tables J.11 to J.17 are exact
            // and the inverse must reproduce Table J.3 bit-exactly.
            var split = ComposeTwoLevelSplit(
                ll2: new float[]
                {
                    0, 4, 8, 12,
                    4, 5, 8, 12,
                    8, 8, 11, 15,
                    12, 12, 14, 18,
                    16, 16, 18, 20,
                },
                hl2: new float[]
                {
                    0, 0, 0,
                    0, 1, 0,
                    0, 1, 0,
                    0, 0, 1,
                    0, 0, 0,
                },
                lh2: new float[]
                {
                    0, 0, 0, 0,
                    0, 1, 1, 1,
                    0, 0, 0, 0,
                    0, 0, 0, 0,
                },
                hh2: new float[]
                {
                    -1, 0, 0,
                    0, -1, 0,
                    0, 1, 0,
                    0, 0, 0,
                },
                hl1: new float[]
                {
                    0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0,
                    0, 1, 0, 1, 0, 0,
                    0, 0, 0, 0, -1, 1,
                    0, 0, 0, 0, 1, 1,
                    0, 0, 1, 1, 0, -1,
                    0, 0, 1, 0, 1, 1,
                    0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0,
                },
                lh1: new float[]
                {
                    0, 0, 0, 0, 0, 0, 0,
                    0, 0, 1, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 1, 1,
                    0, 0, 1, 0, 0, 1, 1,
                    0, 0, 0, 0, 1, 0, 2,
                    0, 0, 0, 0, 0, 0, 1,
                    0, 0, 0, 0, 0, 0, 1,
                    0, 0, 0, 0, 1, 1, 0,
                },
                hh1: new float[]
                {
                    0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0,
                    0, 0, 1, 0, 1, 0,
                    0, 0, 0, 0, 0, 1,
                    0, 0, 0, 0, 0, 1,
                    0, 0, 0, 1, 0, 0,
                    0, 0, 0, 0, 0, 1,
                    0, 0, 0, 0, -1, 0,
                });

            var component = BuildAnnexJ4Component();
            JpxWaveletTransform.Inverse(component, split, decompositionLevels: 2, reversible: true);

            AssertExact(tableJ3SourceSamples, split);
        }

        [Test]
        public void Inverse_Irreversible97T800AnnexJ4Example()
        {
            // The coefficients in Tables J.4 to J.10 are rounded to the nearest integer, so the reconstruction is
            // only approximate: each coefficient carries a rounding error of up to 0.5, which the synthesis
            // filtering sums over neighbouring coefficients across both levels. The largest deviation (about 1.55
            // in the bottom-right corner, where the boundary extension doubles the contributions) stays below 2.
            var split = ComposeTwoLevelSplit(
                ll2: new float[]
                {
                    1, 4, 8, 11,
                    4, 5, 8, 11,
                    8, 9, 11, 13,
                    12, 12, 14, 16,
                    15, 15, 17, 18,
                },
                hl2: new float[3 * 5],
                lh2: new float[4 * 4],
                hh2: new float[]
                {
                    -1, 0, 0,
                    0, -1, 0,
                    0, 0, 0,
                    0, 0, 0,
                },
                hl1: new float[]
                {
                    0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0,
                    0, -1, 0, 0, 0, 0,
                    0, 0, 0, -1, 0, 0,
                    0, 0, 1, 1, 0, -1,
                    0, 0, 0, 0, 0, 0,
                    0, 0, -1, -1, -1, 0,
                    0, 0, 0, 0, 0, 0,
                },
                lh1: new float[]
                {
                    0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, -1, 0, 0, 0,
                    0, 0, 0, 0, 0, 1, 1,
                    0, 0, 0, 0, -1, 1, 0,
                    0, 0, 0, 0, 0, 0, 1,
                    0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0,
                },
                hh1: new float[]
                {
                    -1, 0, 0, 0, 0, 0,
                    0, 0, -1, 0, 0, 0,
                    0, -1, 1, 0, 0, 0,
                    0, 0, 0, 0, 0, 1,
                    0, 0, 0, 0, -1, 0,
                    0, 0, 0, 0, 0, 0,
                    0, 0, -1, 0, -1, 1,
                    0, 0, 0, 0, -1, 0,
                });

            var component = BuildAnnexJ4Component();
            JpxWaveletTransform.Inverse(component, split, decompositionLevels: 2, reversible: false);

            AssertClose(tableJ3SourceSamples, split, 2.0f);
        }

        /// <summary>
        /// Composes the split sample buffer of the 13x17 tile component of ITU-T T.800 (06/2019) Section J.4 from
        /// its seven sub-bands. In the split layout the low-pass coefficients precede the high-pass coefficients
        /// both horizontally and vertically, recursively for each decomposition level: the level-1 bands split the
        /// 13x17 buffer at column 7 and row 9, and the level-2 bands split the remaining 7x9 low-pass region at
        /// column 4 and row 5.
        /// </summary>
        private static float[] ComposeTwoLevelSplit(
            float[] ll2, float[] hl2, float[] lh2, float[] hh2, float[] hl1, float[] lh1, float[] hh1)
        {
            var buffer = new float[13 * 17];

            InsertBand(buffer, ll2, x0: 0, y0: 0, width: 4);
            InsertBand(buffer, hl2, x0: 4, y0: 0, width: 3);
            InsertBand(buffer, lh2, x0: 0, y0: 5, width: 4);
            InsertBand(buffer, hh2, x0: 4, y0: 5, width: 3);
            InsertBand(buffer, hl1, x0: 7, y0: 0, width: 6);
            InsertBand(buffer, lh1, x0: 0, y0: 9, width: 7);
            InsertBand(buffer, hh1, x0: 7, y0: 9, width: 6);

            return buffer;
        }

        private static void InsertBand(float[] buffer, float[] band, int x0, int y0, int width)
        {
            for (var i = 0; i < band.Length; i++)
            {
                buffer[(y0 + i / width) * 13 + x0 + i % width] = band[i];
            }
        }

        /// <summary>
        /// A 13x17 tile component at origin (0, 0) with two decomposition levels. The resolution level dimensions
        /// are the ceil-halved dimensions of the level above (T.800 Equation B-14): 13x17, 7x9 and 4x5.
        /// </summary>
        private static JpxTileComponent BuildAnnexJ4Component()
        {
            var resolutionLevels = new JpxResolutionLevel[]
            {
                new JpxResolutionLevel { Level = 0, TrX0 = 0, TrY0 = 0, TrX1 = 4, TrY1 = 5 },
                new JpxResolutionLevel { Level = 1, TrX0 = 0, TrY0 = 0, TrX1 = 7, TrY1 = 9 },
                new JpxResolutionLevel { Level = 2, TrX0 = 0, TrY0 = 0, TrX1 = 13, TrY1 = 17 },
            };

            return new JpxTileComponent
            {
                TcX0 = 0,
                TcY0 = 0,
                TcX1 = 13,
                TcY1 = 17,
                ResolutionLevels = resolutionLevels,
                CodingStyle = new JpxCodingStyleDefaults(),
                Quantization = new JpxQuantizationDefaults(),
            };
        }

        [Test]
        public void Inverse_NoDecompositionLevelsLeavesSamplesUnchanged()
        {
            var samples = new float[] { 5, 6, 7, 8 };
            var component = BuildComponent(0, 0, 2, 2, 1, 1);

            JpxWaveletTransform.Inverse(component, samples, decompositionLevels: 0, reversible: true);

            AssertExact(new float[] { 5, 6, 7, 8 }, samples);
        }

        // Builds a single-decomposition-level tile component whose sample buffer is expected to hold the fully-split
        // 2D sub-band layout (LL top-left, HL top-right, LH bottom-left, HH bottom-right) expected by
        // JpxWaveletTransform.Inverse. tileX0/tileY0 are the tile-component coordinates (TcX0/TcY0), which set the
        // sub-sampling parity. lowW/lowH are the LL band dimensions.
        private static JpxTileComponent BuildComponent(
            int tileX0, int tileY0, int width, int height, int lowW, int lowH)
        {
            var full = new JpxResolutionLevel
            {
                Level = 1,
                TrX0 = tileX0,
                TrY0 = tileY0,
                TrX1 = tileX0 + width,
                TrY1 = tileY0 + height,
            };

            // The LL band coordinates are the ceil-halved tile coordinates (T.800 Equation B-15).
            var ll = new JpxResolutionLevel
            {
                Level = 0,
                TrX0 = (tileX0 + 1) >> 1,
                TrY0 = (tileY0 + 1) >> 1,
                TrX1 = ((tileX0 + 1) >> 1) + lowW,
                TrY1 = ((tileY0 + 1) >> 1) + lowH,
            };

            return new JpxTileComponent
            {
                TcX0 = tileX0,
                TcY0 = tileY0,
                TcX1 = tileX0 + width,
                TcY1 = tileY0 + height,
                ResolutionLevels = new[] { ll, full },
                CodingStyle = new JpxCodingStyleDefaults(),
                Quantization = new JpxQuantizationDefaults(),
            };
        }

        private static void AssertExact(float[] expected, float[] actual)
        {
            Assert.AreEqual(expected.Length, actual.Length);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i], "Sample " + i);
            }
        }

        private static void AssertClose(float[] expected, float[] actual, float tolerance)
        {
            Assert.AreEqual(expected.Length, actual.Length);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i], tolerance, "Sample " + i);
            }
        }
    }
}
