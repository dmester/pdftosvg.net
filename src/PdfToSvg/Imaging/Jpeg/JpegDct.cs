// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace PdfToSvg.Imaging.Jpeg
{
    internal static class JpegDct
    {
        // Discrete Cosine Transform implementation based on the DCT approximation algorithm described in 
        // "Practical fast 1-D DCT algorithms with 11 multiplications"
        // by C. Loeffler, A. Ligtenberg, G. Moschytz
        // in International Conference on Acoustics, Speech, and Signal Processing, 23 May 1989
        // https://www.semanticscholar.org/paper/Practical-fast-1-D-DCT-algorithms-with-11-Loeffler-Ligtenberg/6134d65dc1d01db1e3c4be6f675763a469a973f6

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
            ref short x0, ref short x1, ref short x2, ref short x3,
            ref short x4, ref short x5, ref short x6, ref short x7
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
            x0 = (short)stage3_0;
            x4 = (short)stage3_1;
            x2 = (short)stage3_2;
            x6 = (short)stage3_3;
            x7 = (short)(stage3_7 - stage3_4);
            x3 = (short)(stage3_5 * sqrt2);
            x5 = (short)(stage3_6 * sqrt2);
            x1 = (short)(stage3_7 + stage3_4);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static void InverseRow(
            ref short x0, ref short x1, ref short x2, ref short x3,
            ref short x4, ref short x5, ref short x6, ref short x7
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
            x0 = (short)(stage1_0 + stage1_7);
            x1 = (short)(stage1_1 + stage1_6);
            x2 = (short)(stage1_2 + stage1_5);
            x3 = (short)(stage1_3 + stage1_4);
            x4 = (short)(stage1_3 - stage1_4);
            x5 = (short)(stage1_2 - stage1_5);
            x6 = (short)(stage1_1 - stage1_6);
            x7 = (short)(stage1_0 - stage1_7);
        }

        public static void Inverse(short[] block)
        {
            for (var x = 0; x < 8; x++)
            {
                InverseRow(
                    ref block[8 * 0 + x],
                    ref block[8 * 1 + x],
                    ref block[8 * 2 + x],
                    ref block[8 * 3 + x],
                    ref block[8 * 4 + x],
                    ref block[8 * 5 + x],
                    ref block[8 * 6 + x],
                    ref block[8 * 7 + x]
                    );
            }

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

            for (var i = 0; i < block.Length; i++)
            {
                block[i] = (short)((block[i] >> 3) + 128);
            }
        }

        public static void Forward(short[] block)
        {
            for (var i = 0; i < block.Length; i++)
            {
                block[i] -= 128;
            }

            for (var y = 0; y < 8; y++)
            {
                ForwardRow(
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

            for (var x = 0; x < 8; x++)
            {
                ForwardRow(
                    ref block[8 * 0 + x],
                    ref block[8 * 1 + x],
                    ref block[8 * 2 + x],
                    ref block[8 * 3 + x],
                    ref block[8 * 4 + x],
                    ref block[8 * 5 + x],
                    ref block[8 * 6 + x],
                    ref block[8 * 7 + x]
                    );
            }

            for (var i = 0; i < block.Length; i++)
            {
                block[i] >>= 3;
            }
        }
    }
}
