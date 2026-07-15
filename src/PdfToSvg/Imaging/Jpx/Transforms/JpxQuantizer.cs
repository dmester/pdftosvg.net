// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.ImageModel;
using System;

namespace PdfToSvg.Imaging.Jpx.Transforms
{
    /// <summary>
    /// Inverse quantization (ITU-T T.800 (06/2019) Annex E) and max-shift ROI realignment (Annex H).
    /// </summary>
    internal static class JpxQuantizer
    {
        /// <summary>
        /// Dequantizes the decoded coefficients of one code-block, writing the reconstructed transform coefficients
        /// into the tile-component's sample plane at the code-block's position in the split sub-band layout.
        /// Code-blocks without decoded coefficients should not be dequantized, keeping their zeroes in the sample
        /// plane.
        /// </summary>
        /// <param name="codeBlock">Code-block whose coefficients should be dequantized.</param>
        /// <param name="inputCoefficients">
        /// Signed quantized coefficients with one fractional bit (value * 2; ITU-T T.800 (06/2019) Equation E-1),
        /// row-major over the Width * Height area of the code-block, as returned by
        /// <see cref="Coding.JpxTier1Decoder.Decode"/>. May be larger than the code-block area.
        /// </param>
        /// <param name="outputSamples">Output sample buffer of the tile-component.</param>
        /// <param name="stride">Width of the tile-component.</param>
        /// <param name="stepSize">
        /// Quantization step size Δb of the sub-band from <see cref="GetStepSize"/>, or one with the no quantization
        /// style (ITU-T T.800 (06/2019) Section E.1.2.1).
        /// </param>
        /// <param name="roiShift">Resolved RGN max-shift of the component (Annex H), or zero when no ROI applies.</param>
        /// <remarks>
        /// Mid-point reconstruction of partially decoded coefficients (reconstruction parameter r = 1/2 in ITU-T
        /// T.800 (06/2019) Equations E-6 and E-8) is already applied by <see cref="Coding.JpxTier1Decoder"/> and
        /// retained as one fractional bit. The coefficients are realigned per Annex H, divided by two and scaled by
        /// the quantization step here.
        /// </remarks>
        public static void DequantizeCodeBlock(
            JpxCodeBlock codeBlock, int[] inputCoefficients, float[] outputSamples,
            int stride, float stepSize, int roiShift)
        {
            var width = codeBlock.Width;
            var height = codeBlock.Height;

            var sourceIndex = 0;
            var targetRowIndex = codeBlock.LocalY0 * stride + codeBlock.LocalX0;
            var fixedPointStepSize = 0.5f * stepSize;

            // Note: Benchmark showed the following code is not worth vectorizing
            for (var y = 0; y < height; y++, targetRowIndex += stride)
            {
                for (var x = 0; x < width; x++)
                {
                    var coefficientTimesTwo = ApplyRoiShift(inputCoefficients[sourceIndex++], roiShift, fractionalBits: 1);
                    outputSamples[targetRowIndex + x] = coefficientTimesTwo * fixedPointStepSize;
                }
            }
        }

        /// <summary>
        /// Computes the quantization step size Δb of a sub-band for the scalar quantization styles
        /// (ITU-T T.800 (06/2019) Equation E-3).
        /// </summary>
        public static float GetStepSize(JpxSubBandType subBandType, int precision, int exponent, int mantissa)
        {
            // Equation E-4 and Table E.1:
            // The dynamic range Rb is the component precision plus the base 2 exponent of the sub-band gain.
            var gainLog2 = subBandType switch
            {
                JpxSubBandType.LL => 0,
                JpxSubBandType.HH => 2,
                _ => 1,
            };

            // For valid streams:
            //
            //           Rb <= 40   (Table A.11 plus the HH gain)
            //    -31 <= εb <= 31   (Table A.30, at worst lowered by Equation E-5)
            //
            // So the scale exponent stays well within ±63. The clamp keeps a malformed exponent from producing an
            // infinite or denormal step size.
            var scaleExponent = MathUtils.Clamp(precision + gainLog2 - exponent, -63, 63);

            // Equation E-3:
            // The mantissa is an 11 bit codestream field (Table A.30); masking keeps the (1 + μb / 2^11) factor
            // within [1, 2) for malformed model values.
            return (float)(Math.Pow(2, scaleExponent) * (1 + (mantissa & 0b111_1111_1111) / 2048d));
        }

        /// <summary>
        /// Realigns a quantized coefficient of a component whose ROI is signalled with the max-shift method
        /// (ITU-T T.800 (06/2019) Section H.1), returning the realigned coefficient. A
        /// <paramref name="roiShift"/> of zero leaves the coefficient unchanged.
        /// </summary>
        /// <remarks>
        /// With max-shift, the code-blocks are entropy coded with Mb' = Mb + s magnitude bit-planes (ITU-T T.800
        /// (06/2019) Equation H-3), where the ROI coefficients occupy the upper Mb bit-planes and the background
        /// coefficients the lower bit-planes. A decoded coefficient with a non-zero bit among its first Mb MSBs
        /// thus has a magnitude of at least 2^s; per Section H.1 step 3 only its first Mb bit-planes are kept,
        /// discarding the s least significant bits that were added by the encoder (Equation H-4). Any other
        /// coefficient is unmodified per steps 2 and 4, whose bit renumbering (Equation H-1) does not change the
        /// numeric coefficient value. No region logic is involved: the realignment depends only on the coefficient
        /// magnitude.
        /// </remarks>
        public static int ApplyRoiShift(int coefficient, int roiShift, int fractionalBits = 0)
        {
            if (roiShift <= 0 || coefficient == 0)
            {
                return coefficient;
            }

            var thresholdShift = roiShift + fractionalBits;
            if (thresholdShift >= 32)
            {
                // No supported Int32 magnitude can reach the fixed-point ROI threshold. SPrgn values are capped at
                // JpxConstraints.MaxRegionOfInterestShift (37) by the marker segment reader.
                return coefficient;
            }

            var magnitude = Math.Abs((long)coefficient);
            if (magnitude < (1L << thresholdShift))
            {
                return coefficient;
            }

            magnitude >>= roiShift;
            return (int)(coefficient < 0 ? -magnitude : magnitude);
        }
    }
}
