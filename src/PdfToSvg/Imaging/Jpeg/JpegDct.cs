// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if NET8_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace PdfToSvg.Imaging.Jpeg
{
    internal static class JpegDct
    {
        // Discrete Cosine Transform implementation based on the DCT approximation algorithm described in 
        // "Practical fast 1-D DCT algorithms with 11 multiplications"
        // by C. Loeffler, A. Ligtenberg, G. Moschytz
        // in International Conference on Acoustics, Speech, and Signal Processing, 23 May 1989
        // https://www.semanticscholar.org/paper/Practical-fast-1-D-DCT-algorithms-with-11-Loeffler-Ligtenberg/6134d65dc1d01db1e3c4be6f675763a469a973f6

        // Note that the FDCT returns a transposed block and the IDCT expects a transposed block as input.

        // √2
        private const float sqrt2 = 1.4142135624f;

        // sin( 3π / 16 )
        private const float c3sin = 0.5555702330f;
        // cos( 3π / 16 )
        private const float c3cos = 0.8314696123f;

        // sin( π / 16 )
        private const float c1sin = 0.1950903220f;
        // cos( π / 16 )
        private const float c1cos = 0.9807852804f;

        // Note: Typo in paper: should be √2c6, not √2c1
        // See note here: https://unix4lyfe.org/dct-1d/

        // √2 * sin( 6π / 16 )
        private const float c6sin = 1.3065629649f;
        // √2 * cos( 6π / 16 )
        private const float c6cos = 0.5411961001f;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static void ForwardRow(
            ref float x0, ref float x1, ref float x2, ref float x3,
            ref float x4, ref float x5, ref float x6, ref float x7
            )
        {
            // Stage 1
            var stage1_0 = x0 + x7;
            var stage1_1 = x1 + x6;
            var stage1_2 = x2 + x5;
            var stage1_3 = x3 + x4;
            var stage1_4 = x3 - x4;
            var stage1_5 = x2 - x5;
            var stage1_6 = x1 - x6;
            var stage1_7 = x0 - x7;

            // Stage 2
            var stage2_0 = stage1_0 + stage1_3;
            var stage2_1 = stage1_1 + stage1_2;
            var stage2_2 = stage1_1 - stage1_2;
            var stage2_3 = stage1_0 - stage1_3;
            var stage2_c3 = c3cos * (stage1_7 + stage1_4);
            var stage2_c1 = c1cos * (stage1_6 + stage1_5);
            var stage2_4 = stage2_c3 + (c3sin - c3cos) * stage1_7;
            var stage2_5 = stage2_c1 + (c1sin - c1cos) * stage1_6;
            var stage2_6 = stage2_c1 - (c1sin + c1cos) * stage1_5;
            var stage2_7 = stage2_c3 - (c3sin + c3cos) * stage1_4;

            // Stage 3
            var stage3_0 = stage2_0 + stage2_1;
            var stage3_1 = stage2_0 - stage2_1;
            var stage3_c6 = c6cos * (stage2_3 + stage2_2);
            var stage3_2 = stage3_c6 + (c6sin - c6cos) * stage2_3;
            var stage3_3 = stage3_c6 - (c6sin + c6cos) * stage2_2;
            var stage3_4 = stage2_4 + stage2_6;
            var stage3_5 = stage2_7 - stage2_5;
            var stage3_6 = stage2_4 - stage2_6;
            var stage3_7 = stage2_7 + stage2_5;

            // Stage 4
            x0 = stage3_0;
            x4 = stage3_1;
            x2 = stage3_2;
            x6 = stage3_3;
            x7 = stage3_7 - stage3_4;
            x3 = stage3_5 * sqrt2;
            x5 = stage3_6 * sqrt2;
            x1 = stage3_7 + stage3_4;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static void InverseRow(
            ref float x0, ref float x1, ref float x2, ref float x3,
            ref float x4, ref float x5, ref float x6, ref float x7
            )
        {
            // Reverse stage 4
            var stage3_0 = x0;
            var stage3_1 = x4;
            var stage3_2 = x2;
            var stage3_3 = x6;
            var stage3_4 = x1 - x7;
            var stage3_5 = x3 * sqrt2;
            var stage3_6 = x5 * sqrt2;
            var stage3_7 = x1 + x7;

            // Reverse stage 3
            var stage2_0 = stage3_0 + stage3_1;
            var stage2_1 = stage3_0 - stage3_1;
            var stage2_c6 = c6cos * (stage3_3 + stage3_2);
            var stage2_2 = stage2_c6 + (-c6sin - c6cos) * stage3_3;
            var stage2_3 = stage2_c6 - (-c6sin + c6cos) * stage3_2;
            var stage2_4 = stage3_4 + stage3_6;
            var stage2_5 = stage3_7 - stage3_5;
            var stage2_6 = stage3_4 - stage3_6;
            var stage2_7 = stage3_7 + stage3_5;

            // Reverse stage 2
            var stage1_0 = stage2_0 + stage2_3;
            var stage1_1 = stage2_1 + stage2_2;
            var stage1_2 = stage2_1 - stage2_2;
            var stage1_3 = stage2_0 - stage2_3;
            var stage1_c3 = c3cos * (stage2_7 + stage2_4);
            var stage1_c1 = c1cos * (stage2_6 + stage2_5);
            var stage1_4 = stage1_c3 + (-c3sin - c3cos) * stage2_7;
            var stage1_5 = stage1_c1 + (-c1sin - c1cos) * stage2_6;
            var stage1_6 = stage1_c1 - (-c1sin + c1cos) * stage2_5;
            var stage1_7 = stage1_c3 - (-c3sin + c3cos) * stage2_4;

            // Reverse stage 1
            x0 = stage1_0 + stage1_7;
            x1 = stage1_1 + stage1_6;
            x2 = stage1_2 + stage1_5;
            x3 = stage1_3 + stage1_4;
            x4 = stage1_3 - stage1_4;
            x5 = stage1_2 - stage1_5;
            x6 = stage1_1 - stage1_6;
            x7 = stage1_0 - stage1_7;
        }

#if NET8_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ForwardRowAvx(
            ref Vector256<float> x0,
            ref Vector256<float> x1,
            ref Vector256<float> x2,
            ref Vector256<float> x3,
            ref Vector256<float> x4,
            ref Vector256<float> x5,
            ref Vector256<float> x6,
            ref Vector256<float> x7
            )
        {
            // Stage 1
            var stage1_0 = x0 + x7;
            var stage1_1 = x1 + x6;
            var stage1_2 = x2 + x5;
            var stage1_3 = x3 + x4;
            var stage1_4 = x3 - x4;
            var stage1_5 = x2 - x5;
            var stage1_6 = x1 - x6;
            var stage1_7 = x0 - x7;

            // Stage 2
            var stage2_0 = stage1_0 + stage1_3;
            var stage2_1 = stage1_1 + stage1_2;
            var stage2_2 = stage1_1 - stage1_2;
            var stage2_3 = stage1_0 - stage1_3;
            var stage2_c3 = c3cos * (stage1_7 + stage1_4);
            var stage2_c1 = c1cos * (stage1_6 + stage1_5);
            var stage2_4 = stage2_c3 + (c3sin - c3cos) * stage1_7;
            var stage2_5 = stage2_c1 + (c1sin - c1cos) * stage1_6;
            var stage2_6 = stage2_c1 - (c1sin + c1cos) * stage1_5;
            var stage2_7 = stage2_c3 - (c3sin + c3cos) * stage1_4;

            // Stage 3
            var stage3_0 = stage2_0 + stage2_1;
            var stage3_1 = stage2_0 - stage2_1;
            var stage3_c6 = c6cos * (stage2_3 + stage2_2);
            var stage3_2 = stage3_c6 + (c6sin - c6cos) * stage2_3;
            var stage3_3 = stage3_c6 - (c6sin + c6cos) * stage2_2;
            var stage3_4 = stage2_4 + stage2_6;
            var stage3_5 = stage2_7 - stage2_5;
            var stage3_6 = stage2_4 - stage2_6;
            var stage3_7 = stage2_7 + stage2_5;

            // Stage 4
            x0 = stage3_0;
            x4 = stage3_1;
            x2 = stage3_2;
            x6 = stage3_3;
            x7 = stage3_7 - stage3_4;
            x3 = stage3_5 * sqrt2;
            x5 = stage3_6 * sqrt2;
            x1 = stage3_7 + stage3_4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ForwardRowSse(
            ref Vector128<float> x0,
            ref Vector128<float> x1,
            ref Vector128<float> x2,
            ref Vector128<float> x3,
            ref Vector128<float> x4,
            ref Vector128<float> x5,
            ref Vector128<float> x6,
            ref Vector128<float> x7
            )
        {
            // Stage 1
            var stage1_0 = x0 + x7;
            var stage1_1 = x1 + x6;
            var stage1_2 = x2 + x5;
            var stage1_3 = x3 + x4;
            var stage1_4 = x3 - x4;
            var stage1_5 = x2 - x5;
            var stage1_6 = x1 - x6;
            var stage1_7 = x0 - x7;

            // Stage 2
            var stage2_0 = stage1_0 + stage1_3;
            var stage2_1 = stage1_1 + stage1_2;
            var stage2_2 = stage1_1 - stage1_2;
            var stage2_3 = stage1_0 - stage1_3;
            var stage2_c3 = c3cos * (stage1_7 + stage1_4);
            var stage2_c1 = c1cos * (stage1_6 + stage1_5);
            var stage2_4 = stage2_c3 + (c3sin - c3cos) * stage1_7;
            var stage2_5 = stage2_c1 + (c1sin - c1cos) * stage1_6;
            var stage2_6 = stage2_c1 - (c1sin + c1cos) * stage1_5;
            var stage2_7 = stage2_c3 - (c3sin + c3cos) * stage1_4;

            // Stage 3
            var stage3_0 = stage2_0 + stage2_1;
            var stage3_1 = stage2_0 - stage2_1;
            var stage3_c6 = c6cos * (stage2_3 + stage2_2);
            var stage3_2 = stage3_c6 + (c6sin - c6cos) * stage2_3;
            var stage3_3 = stage3_c6 - (c6sin + c6cos) * stage2_2;
            var stage3_4 = stage2_4 + stage2_6;
            var stage3_5 = stage2_7 - stage2_5;
            var stage3_6 = stage2_4 - stage2_6;
            var stage3_7 = stage2_7 + stage2_5;

            // Stage 4
            x0 = stage3_0;
            x4 = stage3_1;
            x2 = stage3_2;
            x6 = stage3_3;
            x7 = stage3_7 - stage3_4;
            x3 = stage3_5 * sqrt2;
            x5 = stage3_6 * sqrt2;
            x1 = stage3_7 + stage3_4;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static void InverseRowAvx(
            ref Vector256<float> x0,
            ref Vector256<float> x1,
            ref Vector256<float> x2,
            ref Vector256<float> x3,
            ref Vector256<float> x4,
            ref Vector256<float> x5,
            ref Vector256<float> x6,
            ref Vector256<float> x7
            )
        {
            // Implementation note about the IDCT:
            //
            // Not mentioned in the LLM paper, but the rotation gets a negative angle, so the sign for all sin
            // coefficients will change.
            //
            //   cos(-x) =  cos(x)
            //   sin(-x) = -sin(x)
            //
            // Documented in this article by Pepijn de Vos:
            // http://pepijndevos.nl/2018/07/04/loefflers-discrete-cosine-transform-algorithm-in-futhark.html

            // Reverse stage 4
            var stage3_0 = x0;
            var stage3_1 = x4;
            var stage3_2 = x2;
            var stage3_3 = x6;
            var stage3_4 = x1 - x7;
            var stage3_5 = x3 * sqrt2;
            var stage3_6 = x5 * sqrt2;
            var stage3_7 = x1 + x7;

            // Reverse stage 3
            var stage2_0 = stage3_0 + stage3_1;
            var stage2_1 = stage3_0 - stage3_1;
            var stage2_c6 = c6cos * (stage3_3 + stage3_2);
            var stage2_2 = stage2_c6 + (-c6sin - c6cos) * stage3_3;
            var stage2_3 = stage2_c6 - (-c6sin + c6cos) * stage3_2;
            var stage2_4 = stage3_4 + stage3_6;
            var stage2_5 = stage3_7 - stage3_5;
            var stage2_6 = stage3_4 - stage3_6;
            var stage2_7 = stage3_7 + stage3_5;

            // Reverse stage 2
            var stage1_0 = stage2_0 + stage2_3;
            var stage1_1 = stage2_1 + stage2_2;
            var stage1_2 = stage2_1 - stage2_2;
            var stage1_3 = stage2_0 - stage2_3;
            var stage1_c3 = c3cos * (stage2_7 + stage2_4);
            var stage1_c1 = c1cos * (stage2_6 + stage2_5);
            var stage1_4 = stage1_c3 + (-c3sin - c3cos) * stage2_7;
            var stage1_5 = stage1_c1 + (-c1sin - c1cos) * stage2_6;
            var stage1_6 = stage1_c1 - (-c1sin + c1cos) * stage2_5;
            var stage1_7 = stage1_c3 - (-c3sin + c3cos) * stage2_4;

            // Reverse stage 1
            x0 = stage1_0 + stage1_7;
            x1 = stage1_1 + stage1_6;
            x2 = stage1_2 + stage1_5;
            x3 = stage1_3 + stage1_4;
            x4 = stage1_3 - stage1_4;
            x5 = stage1_2 - stage1_5;
            x6 = stage1_1 - stage1_6;
            x7 = stage1_0 - stage1_7;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static void InverseRowSse(
            ref Vector128<float> x0,
            ref Vector128<float> x1,
            ref Vector128<float> x2,
            ref Vector128<float> x3,
            ref Vector128<float> x4,
            ref Vector128<float> x5,
            ref Vector128<float> x6,
            ref Vector128<float> x7
            )
        {
            // Implementation note about the IDCT:
            //
            // Not mentioned in the LLM paper, but the rotation gets a negative angle, so the sign for all sin
            // coefficients will change.
            //
            //   cos(-x) =  cos(x)
            //   sin(-x) = -sin(x)
            //
            // Documented in this article by Pepijn de Vos:
            // http://pepijndevos.nl/2018/07/04/loefflers-discrete-cosine-transform-algorithm-in-futhark.html

            // Reverse stage 4
            var stage3_0 = x0;
            var stage3_1 = x4;
            var stage3_2 = x2;
            var stage3_3 = x6;
            var stage3_4 = x1 - x7;
            var stage3_5 = x3 * sqrt2;
            var stage3_6 = x5 * sqrt2;
            var stage3_7 = x1 + x7;

            // Reverse stage 3
            var stage2_0 = stage3_0 + stage3_1;
            var stage2_1 = stage3_0 - stage3_1;
            var stage2_c6 = c6cos * (stage3_3 + stage3_2);
            var stage2_2 = stage2_c6 + (-c6sin - c6cos) * stage3_3;
            var stage2_3 = stage2_c6 - (-c6sin + c6cos) * stage3_2;
            var stage2_4 = stage3_4 + stage3_6;
            var stage2_5 = stage3_7 - stage3_5;
            var stage2_6 = stage3_4 - stage3_6;
            var stage2_7 = stage3_7 + stage3_5;

            // Reverse stage 2
            var stage1_0 = stage2_0 + stage2_3;
            var stage1_1 = stage2_1 + stage2_2;
            var stage1_2 = stage2_1 - stage2_2;
            var stage1_3 = stage2_0 - stage2_3;
            var stage1_c3 = c3cos * (stage2_7 + stage2_4);
            var stage1_c1 = c1cos * (stage2_6 + stage2_5);
            var stage1_4 = stage1_c3 + (-c3sin - c3cos) * stage2_7;
            var stage1_5 = stage1_c1 + (-c1sin - c1cos) * stage2_6;
            var stage1_6 = stage1_c1 - (-c1sin + c1cos) * stage2_5;
            var stage1_7 = stage1_c3 - (-c3sin + c3cos) * stage2_4;

            // Reverse stage 1
            x0 = stage1_0 + stage1_7;
            x1 = stage1_1 + stage1_6;
            x2 = stage1_2 + stage1_5;
            x3 = stage1_3 + stage1_4;
            x4 = stage1_3 - stage1_4;
            x5 = stage1_2 - stage1_5;
            x6 = stage1_1 - stage1_6;
            x7 = stage1_0 - stage1_7;
        }
#endif

        public static void ForwardScalar(short[] block)
        {
            var floatBlock = new float[block.Length];

            ForwardScalar(block, 0, floatBlock);

            for (var i = 0; i < block.Length; i++)
            {
                block[i] = (short)floatBlock[i];
            }
        }

        public static void ForwardScalar(short[] source, int sourceOffset, float[] block)
        {
            static void ProcessRows(float[] block)
            {
                for (var y = 0; y < 8; y++)
                {
                    var rowStartIndex = y * 8;

                    ForwardRow(
                        ref block[rowStartIndex + 0],
                        ref block[rowStartIndex + 1],
                        ref block[rowStartIndex + 2],
                        ref block[rowStartIndex + 3],
                        ref block[rowStartIndex + 4],
                        ref block[rowStartIndex + 5],
                        ref block[rowStartIndex + 6],
                        ref block[rowStartIndex + 7]
                        );
                }
            }

            for (var i = 0; i < block.Length; i++)
            {
                block[i] = source[sourceOffset + i] - 128f;
            }

            ProcessRows(block);
            JpegBlockUtils.TransposeScalar(block);
            ProcessRows(block);

            for (var i = 0; i < block.Length; i++)
            {
                block[i] *= 1f / 8;
            }
        }

        public static void InverseScalar(short[] block)
        {
            static void ProcessRows(float[] block)
            {
                for (var y = 0; y < 8; y++)
                {
                    InverseRow(
                        ref block[8 * y + 0],
                        ref block[8 * y + 1],
                        ref block[8 * y + 2],
                        ref block[8 * y + 3],
                        ref block[8 * y + 4],
                        ref block[8 * y + 5],
                        ref block[8 * y + 6],
                        ref block[8 * y + 7]
                        );
                }
            }

            var floatBlock = new float[block.Length];

            for (var i = 0; i < block.Length; i++)
            {
                floatBlock[i] = block[i];
            }

            // Note we process rows followed by columns in the scalar implementation, in contrast from the opposite
            // order in the vectorized variants. Th mathematicall output is the same, but the actual output can vary
            // on a fractional level.

            // 1D IDCT on rows
            ProcessRows(floatBlock);

            // 1D IDCT on columns
            JpegBlockUtils.TransposeScalar(floatBlock);
            ProcessRows(floatBlock);

            for (var i = 0; i < block.Length; i++)
            {
                // Values should be clamped to range [0, 255] according to T.81 Section A.3.1
                var shiftedValue = floatBlock[i] * (1f / 8) + 128f;
                block[i] = MathUtils.RoundToShort(MathUtils.Clamp(shiftedValue, 0f, 255f));
            }
        }

#if NET8_0_OR_GREATER

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void ForwardAvx(
            ref Vector256<float> row0,
            ref Vector256<float> row1,
            ref Vector256<float> row2,
            ref Vector256<float> row3,
            ref Vector256<float> row4,
            ref Vector256<float> row5,
            ref Vector256<float> row6,
            ref Vector256<float> row7
            )
        {
            var f128 = Vector256.Create(128f);
            row0 = row0 - f128;
            row1 = row1 - f128;
            row2 = row2 - f128;
            row3 = row3 - f128;
            row4 = row4 - f128;
            row5 = row5 - f128;
            row6 = row6 - f128;
            row7 = row7 - f128;

            ForwardRowAvx(ref row0, ref row1, ref row2, ref row3, ref row4, ref row5, ref row6, ref row7);
            JpegBlockUtils.TransposeAvx(ref row0, ref row1, ref row2, ref row3, ref row4, ref row5, ref row6, ref row7);
            ForwardRowAvx(ref row0, ref row1, ref row2, ref row3, ref row4, ref row5, ref row6, ref row7);

            var downscaler = Vector256.Create(1f / 8);
            row0 = row0 * downscaler;
            row1 = row1 * downscaler;
            row2 = row2 * downscaler;
            row3 = row3 * downscaler;
            row4 = row4 * downscaler;
            row5 = row5 * downscaler;
            row6 = row6 * downscaler;
            row7 = row7 * downscaler;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void ForwardSse(
            ref Vector128<float> row0_lo,
            ref Vector128<float> row0_hi,
            ref Vector128<float> row1_lo,
            ref Vector128<float> row1_hi,
            ref Vector128<float> row2_lo,
            ref Vector128<float> row2_hi,
            ref Vector128<float> row3_lo,
            ref Vector128<float> row3_hi,
            ref Vector128<float> row4_lo,
            ref Vector128<float> row4_hi,
            ref Vector128<float> row5_lo,
            ref Vector128<float> row5_hi,
            ref Vector128<float> row6_lo,
            ref Vector128<float> row6_hi,
            ref Vector128<float> row7_lo,
            ref Vector128<float> row7_hi
            )
        {
            var f128 = Vector128.Create(128f);
            row0_lo = row0_lo - f128;
            row0_hi = row0_hi - f128;
            row1_lo = row1_lo - f128;
            row1_hi = row1_hi - f128;
            row2_lo = row2_lo - f128;
            row2_hi = row2_hi - f128;
            row3_lo = row3_lo - f128;
            row3_hi = row3_hi - f128;
            row4_lo = row4_lo - f128;
            row4_hi = row4_hi - f128;
            row5_lo = row5_lo - f128;
            row5_hi = row5_hi - f128;
            row6_lo = row6_lo - f128;
            row6_hi = row6_hi - f128;
            row7_lo = row7_lo - f128;
            row7_hi = row7_hi - f128;

            ForwardRowSse(ref row0_lo, ref row1_lo, ref row2_lo, ref row3_lo, ref row4_lo, ref row5_lo, ref row6_lo, ref row7_lo);
            ForwardRowSse(ref row0_hi, ref row1_hi, ref row2_hi, ref row3_hi, ref row4_hi, ref row5_hi, ref row6_hi, ref row7_hi);

            JpegBlockUtils.TransposeSse(
                ref row0_lo, ref row0_hi,
                ref row1_lo, ref row1_hi,
                ref row2_lo, ref row2_hi,
                ref row3_lo, ref row3_hi,
                ref row4_lo, ref row4_hi,
                ref row5_lo, ref row5_hi,
                ref row6_lo, ref row6_hi,
                ref row7_lo, ref row7_hi);

            ForwardRowSse(ref row0_lo, ref row1_lo, ref row2_lo, ref row3_lo, ref row4_lo, ref row5_lo, ref row6_lo, ref row7_lo);
            ForwardRowSse(ref row0_hi, ref row1_hi, ref row2_hi, ref row3_hi, ref row4_hi, ref row5_hi, ref row6_hi, ref row7_hi);

            var multiplier = Vector128.Create(1f / 8);
            row0_lo = row0_lo * multiplier;
            row0_hi = row0_hi * multiplier;
            row1_lo = row1_lo * multiplier;
            row1_hi = row1_hi * multiplier;
            row2_lo = row2_lo * multiplier;
            row2_hi = row2_hi * multiplier;
            row3_lo = row3_lo * multiplier;
            row3_hi = row3_hi * multiplier;
            row4_lo = row4_lo * multiplier;
            row4_hi = row4_hi * multiplier;
            row5_lo = row5_lo * multiplier;
            row5_hi = row5_hi * multiplier;
            row6_lo = row6_lo * multiplier;
            row6_hi = row6_hi * multiplier;
            row7_lo = row7_lo * multiplier;
            row7_hi = row7_hi * multiplier;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void InverseAvx(
            ref Vector256<float> row0,
            ref Vector256<float> row1,
            ref Vector256<float> row2,
            ref Vector256<float> row3,
            ref Vector256<float> row4,
            ref Vector256<float> row5,
            ref Vector256<float> row6,
            ref Vector256<float> row7
            )
        {
            // 1D IDCT on columns
            InverseRowAvx(ref row0, ref row1, ref row2, ref row3, ref row4, ref row5, ref row6, ref row7);

            // 1D IDCT on rows
            JpegBlockUtils.TransposeAvx(ref row0, ref row1, ref row2, ref row3, ref row4, ref row5, ref row6, ref row7);
            InverseRowAvx(ref row0, ref row1, ref row2, ref row3, ref row4, ref row5, ref row6, ref row7);

            // Level shifting from range [-0.5, 0.5] to clamped range [0, 255] (see T.81 Section A.3.1)
            var multiplier = Vector256.Create(1f / 8);
            var offset = Vector256.Create(128f);
            var min = Vector256.Create(0f);
            var max = Vector256.Create(255f);

            Vector256<float> LevelShift(Vector256<float> input)
            {
                var mappedValue = input * multiplier + offset;
                return JpegVectorUtils.ClampNative(mappedValue, min, max);
            }

            row0 = LevelShift(row0);
            row1 = LevelShift(row1);
            row2 = LevelShift(row2);
            row3 = LevelShift(row3);
            row4 = LevelShift(row4);
            row5 = LevelShift(row5);
            row6 = LevelShift(row6);
            row7 = LevelShift(row7);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void InverseSse(
            ref Vector128<float> row0_lo,
            ref Vector128<float> row0_hi,
            ref Vector128<float> row1_lo,
            ref Vector128<float> row1_hi,
            ref Vector128<float> row2_lo,
            ref Vector128<float> row2_hi,
            ref Vector128<float> row3_lo,
            ref Vector128<float> row3_hi,
            ref Vector128<float> row4_lo,
            ref Vector128<float> row4_hi,
            ref Vector128<float> row5_lo,
            ref Vector128<float> row5_hi,
            ref Vector128<float> row6_lo,
            ref Vector128<float> row6_hi,
            ref Vector128<float> row7_lo,
            ref Vector128<float> row7_hi
            )
        {
            // 1D IDCT on columns
            InverseRowSse(ref row0_lo, ref row1_lo, ref row2_lo, ref row3_lo, ref row4_lo, ref row5_lo, ref row6_lo, ref row7_lo);
            InverseRowSse(ref row0_hi, ref row1_hi, ref row2_hi, ref row3_hi, ref row4_hi, ref row5_hi, ref row6_hi, ref row7_hi);

            // 1D IDCT on rows
            JpegBlockUtils.TransposeSse(
                ref row0_lo, ref row0_hi,
                ref row1_lo, ref row1_hi,
                ref row2_lo, ref row2_hi,
                ref row3_lo, ref row3_hi,
                ref row4_lo, ref row4_hi,
                ref row5_lo, ref row5_hi,
                ref row6_lo, ref row6_hi,
                ref row7_lo, ref row7_hi);

            InverseRowSse(ref row0_lo, ref row1_lo, ref row2_lo, ref row3_lo, ref row4_lo, ref row5_lo, ref row6_lo, ref row7_lo);
            InverseRowSse(ref row0_hi, ref row1_hi, ref row2_hi, ref row3_hi, ref row4_hi, ref row5_hi, ref row6_hi, ref row7_hi);

            // Level shifting from range [-0.5, 0.5] to clamped range [0, 255] (see T.81 Section A.3.1)
            var multiplier = Vector128.Create(1f / 8);
            var offset = Vector128.Create(128f);
            var min = Vector128.Create(0f);
            var max = Vector128.Create(255f);

            Vector128<float> LevelShift(Vector128<float> input)
            {
                var mappedValue = input * multiplier + offset;
                return JpegVectorUtils.ClampNative(mappedValue, min, max);
            }

            row0_lo = LevelShift(row0_lo);
            row0_hi = LevelShift(row0_hi);
            row1_lo = LevelShift(row1_lo);
            row1_hi = LevelShift(row1_hi);
            row2_lo = LevelShift(row2_lo);
            row2_hi = LevelShift(row2_hi);
            row3_lo = LevelShift(row3_lo);
            row3_hi = LevelShift(row3_hi);
            row4_lo = LevelShift(row4_lo);
            row4_hi = LevelShift(row4_hi);
            row5_lo = LevelShift(row5_lo);
            row5_hi = LevelShift(row5_hi);
            row6_lo = LevelShift(row6_lo);
            row6_hi = LevelShift(row6_hi);
            row7_lo = LevelShift(row7_lo);
            row7_hi = LevelShift(row7_hi);
        }
#endif
    }
}
