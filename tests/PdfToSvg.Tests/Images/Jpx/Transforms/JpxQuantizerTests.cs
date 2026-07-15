// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx.Codestream;
using PdfToSvg.Imaging.Jpx.ImageModel;
using PdfToSvg.Imaging.Jpx.Transforms;

namespace PdfToSvg.Tests.Images.Jpx.Transforms
{
    // ITU-T T.800 (06/2019) Annex E inverse quantization and Annex H max-shift ROI realignment.
    // All expected values are computed by hand from Equations E-2 to E-5 and Section H.1; the derivations are given
    // in comments.
    internal class JpxQuantizerTests
    {
        // The quantizer consumes one code-block at a time together with its decoded coefficients, writing into
        // the caller supplied sample plane, so the tests only need the code-block geometry and a sub-band type.
        private static JpxCodeBlock CreateCodeBlock(
            JpxResolutionLevelSubBand subBand, int localX0, int localY0, int width, int height)
        {
            return new JpxCodeBlock(
                subBand,
                gridX: 0, gridY: 0,
                x0: 0, y0: 0, x1: width, y1: height,
                localX0: localX0, localY0: localY0);
        }

        [Test]
        public void GetStepSize_UnitStepWhenExponentEqualsDynamicRange()
        {
            // Equations E-3 and E-4: Rb = 8 + 0 for the LL band, so eb = 8 and ub = 0 give
            // delta = 2^(8-8) * (1 + 0/2^11) = 1.
            Assert.AreEqual(1f, JpxQuantizer.GetStepSize(JpxSubBandType.LL, 8, 8, 0));
        }

        [Test]
        public void GetStepSize_AppliesTableE1SubBandGains()
        {
            // Equation E-4 with Table E.1: log2(gain) is 0 for LL, 1 for HL/LH and 2 for HH.
            // With precision 8, eb = 8 and ub = 0, delta = 2^(8 + log2(gain) - 8).
            Assert.AreEqual(1f, JpxQuantizer.GetStepSize(JpxSubBandType.LL, 8, 8, 0));
            Assert.AreEqual(2f, JpxQuantizer.GetStepSize(JpxSubBandType.HL, 8, 8, 0));
            Assert.AreEqual(2f, JpxQuantizer.GetStepSize(JpxSubBandType.LH, 8, 8, 0));
            Assert.AreEqual(4f, JpxQuantizer.GetStepSize(JpxSubBandType.HH, 8, 8, 0));
        }

        [Test]
        public void GetStepSize_AppliesMantissa()
        {
            // Equation E-3: the mantissa scales the step by (1 + ub/2^11).
            Assert.AreEqual(1.5f, JpxQuantizer.GetStepSize(JpxSubBandType.LL, 8, 8, 1024));
            Assert.AreEqual(1f + 2047f / 2048, JpxQuantizer.GetStepSize(JpxSubBandType.LL, 8, 8, 2047));
        }

        [Test]
        public void GetStepSize_HandComputedCombination()
        {
            // HL band, precision 12: Rb = 12 + 1 = 13 (Equation E-4, Table E.1).
            // eb = 16, ub = 1779: delta = 2^(13-16) * (1 + 1779/2048) = 3827/16384 (Equation E-3).
            Assert.AreEqual(3827f / 16384, JpxQuantizer.GetStepSize(JpxSubBandType.HL, 12, 16, 1779));
        }

        [Test]
        public void GetStepSize_MalformedExponentStaysFinite()
        {
            var tiny = JpxQuantizer.GetStepSize(JpxSubBandType.HH, 38, 1000, 2047);
            var huge = JpxQuantizer.GetStepSize(JpxSubBandType.HH, 38, -1000, 2047);

            Assert.IsFalse(float.IsInfinity(tiny) || float.IsNaN(tiny));
            Assert.IsFalse(float.IsInfinity(huge) || float.IsNaN(huge));
            Assert.Greater(tiny, 0f);
            Assert.Greater(huge, 0f);
        }

        [Test]
        public void GetStepSize_ConsistentWithResolvedScalarDerivedValues()
        {
            // Scalar derived quantization with NL = 2, e0 = 10, u0 = 512, G = 2 and precision 8.
            // Equation E-5 gives (eb, ub) = (e0 - NL + nb, u0), Equation E-2 Mb = G + eb - 1 and
            // Equation E-3 the step size with the mantissa factor 1 + 512/2048 = 1.25:
            //   LL   (nb = 2): eb = 10, Mb = 11, delta = 2^(8+0-10) * 1.25 = 0.3125
            //   2HH  (nb = 2): eb = 10, Mb = 11, delta = 2^(8+2-10) * 1.25 = 1.25
            //   1HL  (nb = 1): eb = 9,  Mb = 10, delta = 2^(8+1-9)  * 1.25 = 1.25
            var codingStyle = new JpxCodingStyleDefaults
            {
                NumberOfDecompositionLevels = 2,
                CodeBlockWidth = 64,
                CodeBlockHeight = 64,
                PrecinctSize = new[]
                {
                    new JpxPrecinctSize { PPx = 15, PPy = 15 },
                    new JpxPrecinctSize { PPx = 15, PPy = 15 },
                    new JpxPrecinctSize { PPx = 15, PPy = 15 },
                },
            };
            var quantization = new JpxQuantizationDefaults
            {
                ScalarDerived = true,
                NumberOfGuardBits = 2,
                Values = new[] { new JpxQuantizationDefaultValues { Exponent = 10, Mantissa = 512 } },
            };

            var resolutions = JpxTileLayoutBuilder.BuildResolutionLevels(0, 0, 64, 64, codingStyle, quantization);

            var ll = resolutions[0].SubBands[0];
            var hh2 = resolutions[1].SubBands[2];
            var hl1 = resolutions[2].SubBands[0];

            Assert.AreEqual(11, ll.MagnitudeBitPlanes);
            Assert.AreEqual(11, hh2.MagnitudeBitPlanes);
            Assert.AreEqual(10, hl1.MagnitudeBitPlanes);

            Assert.AreEqual(0.3125f, JpxQuantizer.GetStepSize(ll.Type, 8, ll.Exponent, ll.Mantissa));
            Assert.AreEqual(1.25f, JpxQuantizer.GetStepSize(hh2.Type, 8, hh2.Exponent, hh2.Mantissa));
            Assert.AreEqual(1.25f, JpxQuantizer.GetStepSize(hl1.Type, 8, hl1.Exponent, hl1.Mantissa));
        }

        [Test]
        public void DequantizeCodeBlock_PlacesCodeBlocksInSplitLayout()
        {
            // Two 2x3 code-blocks side by side in a 4x3 sample plane. The second block starts at
            // LocalX0 = 2, so its coefficients must be interleaved with the first block's rows.
            var subBand = new JpxResolutionLevelSubBand { Type = JpxSubBandType.LL };
            var samples = new float[4 * 3];

            JpxQuantizer.DequantizeCodeBlock(
                CreateCodeBlock(subBand, 0, 0, 2, 3),
                new[] { 2, -4, 6, -8, 10, -12 }, samples, stride: 4, stepSize: 1f, roiShift: 0);
            JpxQuantizer.DequantizeCodeBlock(
                CreateCodeBlock(subBand, 2, 0, 2, 3),
                new[] { 20, 40, 60, 80, 100, 120 }, samples, stride: 4, stepSize: 1f, roiShift: 0);

            var expected = new float[]
            {
                1, -2, 10, 20,
                3, -4, 30, 40,
                5, -6, 50, 60,
            };
            Assert.AreEqual(expected, samples);
        }

        [Test]
        public void DequantizeCodeBlock_OffsetCodeBlockUsesLocalPosition()
        {
            // A single 2x2 code-block at LocalX0 = 1, LocalY0 = 1 in a 4x4 sample plane. All other
            // samples must remain zero.
            var subBand = new JpxResolutionLevelSubBand { Type = JpxSubBandType.LL };
            var samples = new float[4 * 4];

            JpxQuantizer.DequantizeCodeBlock(
                CreateCodeBlock(subBand, 1, 1, 2, 2),
                new[] { 14, -16, 18, -20 }, samples, stride: 4, stepSize: 1f, roiShift: 0);

            var expected = new float[]
            {
                0, 0, 0, 0,
                0, 7, -8, 0,
                0, 9, -10, 0,
                0, 0, 0, 0,
            };
            Assert.AreEqual(expected, samples);
        }

        [Test]
        public void DequantizeCodeBlock_IrreversibleScalesByStepSize()
        {
            // HL band with precision 8, eb = 8 and ub = 0: delta = 2^(8+1-8) = 2 (Equation E-3).
            // The coefficients are multiplied by delta with no additional reconstruction offset:
            // the mid-point reconstruction of partially decoded coefficients (Equation E-6 with
            // r = 1/2) is already applied by the Tier-1 decoder.
            var subBand = new JpxResolutionLevelSubBand { Type = JpxSubBandType.HL };
            var samples = new float[2 * 2];
            var stepSize = JpxQuantizer.GetStepSize(subBand.Type, precision: 8, exponent: 8, mantissa: 0);

            JpxQuantizer.DequantizeCodeBlock(
                CreateCodeBlock(subBand, 0, 0, 2, 2),
                new[] { 8, -8, 0, 200 }, samples, stride: 2, stepSize, roiShift: 0);

            Assert.AreEqual(new float[] { 8, -8, 0, 200 }, samples);
        }

        [Test]
        public void DequantizeCodeBlock_ReadsOnlyBlockAreaOfOversizedBuffer()
        {
            // The Tier-1 decoder returns a reused buffer that may be larger than the code-block
            // area; only the first Width * Height coefficients belong to the block.
            var subBand = new JpxResolutionLevelSubBand { Type = JpxSubBandType.LL };
            var samples = new float[3 * 3];
            var coefficients = new[] { 2, 4, 6, 8, /* stale tail of a reused buffer: */ 99, 99, 99 };

            JpxQuantizer.DequantizeCodeBlock(
                CreateCodeBlock(subBand, 0, 0, 2, 2),
                coefficients, samples, stride: 3, stepSize: 1f, roiShift: 0);

            var expected = new float[]
            {
                1, 2, 0,
                3, 4, 0,
                0, 0, 0,
            };
            Assert.AreEqual(expected, samples);
        }

        [Test]
        public void DequantizeCodeBlock_PreservesTier1FractionalBit()
        {
            // Tier-1 stores the midpoint-reconstructed values +1.5 and -1.5 as signed fixed-point integers +3 and
            // -3. The dequantizer consumes that fractional bit without relying on code-block-wide decode state.
            var subBand = new JpxResolutionLevelSubBand { Type = JpxSubBandType.LL };
            var samples = new float[2 * 1];
            var stepSize = JpxQuantizer.GetStepSize(subBand.Type, precision: 8, exponent: 8, mantissa: 0);

            JpxQuantizer.DequantizeCodeBlock(
                CreateCodeBlock(subBand, 0, 0, 2, 1),
                new[] { 3, -3 }, samples, stride: 2, stepSize, roiShift: 0);

            Assert.AreEqual(new float[] { 1.5f, -1.5f }, samples);
        }

        [Test]
        public void DequantizeCodeBlock_AppliesRoiShiftBeforeScaling()
        {
            // Max-shift ROI with s = 2 (Section H.1): coefficients with magnitude >= 2^2 are ROI
            // coefficients and are shifted down, smaller coefficients are background and kept.
            var subBand = new JpxResolutionLevelSubBand { Type = JpxSubBandType.LL };
            var samples = new float[2 * 2];

            JpxQuantizer.DequantizeCodeBlock(
                CreateCodeBlock(subBand, 0, 0, 2, 2),
                new[] { 32, -32, 6, -6 }, samples, stride: 2, stepSize: 1f, roiShift: 2);

            Assert.AreEqual(new float[] { 4, -4, 3, -3 }, samples);
        }

        [Test]
        public void ApplyRoiShift_ZeroShiftIsNoOp()
        {
            Assert.AreEqual(12345, JpxQuantizer.ApplyRoiShift(12345, 0));
            Assert.AreEqual(-12345, JpxQuantizer.ApplyRoiShift(-12345, 0));
            Assert.AreEqual(0, JpxQuantizer.ApplyRoiShift(0, 0));
        }

        [Test]
        public void ApplyRoiShift_BelowThresholdUnchanged()
        {
            // Section H.1 steps 2 and 4: a coefficient whose first Mb MSBs are all zero, i.e.,
            // whose magnitude is below 2^s = 32, is a background coefficient and keeps its value.
            Assert.AreEqual(31, JpxQuantizer.ApplyRoiShift(31, 5));
            Assert.AreEqual(-31, JpxQuantizer.ApplyRoiShift(-31, 5));
            Assert.AreEqual(0, JpxQuantizer.ApplyRoiShift(0, 5));
        }

        [Test]
        public void ApplyRoiShift_AtOrAboveThresholdShiftsDownMagnitude()
        {
            // Section H.1 step 3: a coefficient with magnitude >= 2^s = 32 is an ROI coefficient
            // whose s least significant bit-planes are discarded. The shift applies to the
            // magnitude, so negative coefficients round towards zero.
            Assert.AreEqual(1, JpxQuantizer.ApplyRoiShift(32, 5));
            Assert.AreEqual(-1, JpxQuantizer.ApplyRoiShift(-32, 5));
            Assert.AreEqual(3, JpxQuantizer.ApplyRoiShift(100, 5));
            Assert.AreEqual(-3, JpxQuantizer.ApplyRoiShift(-100, 5));
            Assert.AreEqual(int.MaxValue >> 5, JpxQuantizer.ApplyRoiShift(int.MaxValue, 5));
        }

        [Test]
        public void ApplyRoiShift_LargeShiftIsNoOp()
        {
            // SPrgn can signal shifts up to 37, above any possible Int32 coefficient magnitude.
            Assert.AreEqual(int.MaxValue, JpxQuantizer.ApplyRoiShift(int.MaxValue, 37));
            Assert.AreEqual(int.MinValue, JpxQuantizer.ApplyRoiShift(int.MinValue, 37));
        }
    }
}
