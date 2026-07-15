// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

#if NET8_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace PdfToSvg.Imaging.Jpeg
{
    internal struct JpegQuantizationTable
    {
        public const int Size = 64;

        public IEnumerable<ushort> Quantizers { get; }

        private ushort[] quantizers;
        private float[] quantizerMultipliers;
        private float[] quantizersTransposedZigZag;
        private float[] quantizerMultipliersZigZag;
        private float[] quantizerMultipliersTransposedZigZag;

        public int DCMultiplier => quantizers[0];
        public float DCReverseMultiplier => quantizerMultipliersZigZag[0];


        public JpegQuantizationTable(params ushort[] quantizers)
        {
            if (quantizers.Length != Size)
            {
                throw new ArgumentException("Expected quantization table with " + Size + " elements.", nameof(quantizers));
            }

            Quantizers = quantizers;
            this.quantizers = quantizers;

            var quantizersFloat = new float[Size];
            var quantizerMultipliers = new float[Size];
            for (var i = 0; i < Size; i++)
            {
                quantizersFloat[i] = quantizers[i];
                quantizerMultipliers[i] = quantizers[i] > 0 ? 1f / quantizers[i] : 0f;
            }

            quantizersTransposedZigZag = TransposedZigZag(quantizersFloat);
            quantizerMultipliersTransposedZigZag = TransposedZigZag(quantizerMultipliers);

            quantizerMultipliersZigZag = new float[Size];
            JpegZigZag.ReverseZigZag(quantizerMultipliers, quantizerMultipliersZigZag);

            this.quantizerMultipliers = quantizerMultipliers;
        }

        private static float[] TransposedZigZag(float[] source)
        {
            var result = new float[Size];
            JpegZigZag.ReverseZigZag(source, result);
            JpegBlockUtils.TransposeScalar(result);
            return result;
        }

        public static JpegQuantizationTable Identity { get; } = new JpegQuantizationTable(
            1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1
        );

        // Tables from ITU T.81

        // Table K.1 – Luminance quantization table
        public static JpegQuantizationTable Luminance { get; } = new JpegQuantizationTable(
            16, 11, 10, 16, 24, 40, 51, 61,
            12, 12, 14, 19, 26, 58, 60, 55,
            14, 13, 16, 24, 40, 57, 69, 56,
            14, 17, 22, 29, 51, 87, 80, 62,
            18, 22, 37, 56, 68, 109, 103, 77,
            24, 35, 55, 64, 81, 104, 113, 92,
            49, 64, 78, 87, 103, 121, 120, 101,
            72, 92, 95, 98, 112, 100, 103, 99
        );

        // Table K.2 – Chrominance quantization table
        public static JpegQuantizationTable Chrominance { get; } = new JpegQuantizationTable(
            17, 18, 24, 47, 99, 99, 99, 99,
            18, 21, 26, 66, 99, 99, 99, 99,
            24, 26, 56, 99, 99, 99, 99, 99,
            47, 66, 99, 99, 99, 99, 99, 99,
            99, 99, 99, 99, 99, 99, 99, 99,
            99, 99, 99, 99, 99, 99, 99, 99,
            99, 99, 99, 99, 99, 99, 99, 99,
            99, 99, 99, 99, 99, 99, 99, 99
        );

        public void DequantizeScalar(float[] block)
        {
            var quantizers = this.quantizers;

            for (var i = 0; i < block.Length; i++)
            {
                block[i] = block[i] * quantizers[i];
            }
        }

        public void DequantizeTransposedZigZagScalar(float[] block)
        {
            var quantizersTransposedZigZag = this.quantizersTransposedZigZag;

            for (var i = 0; i < block.Length; i++)
            {
                block[i] = block[i] * quantizersTransposedZigZag[i];
            }
        }

        public void QuantizeScalar(int[] block)
        {
            var quantizerMultipliers = this.quantizerMultipliers;

            for (var i = 0; i < block.Length; i++)
            {
                block[i] = (int)MathF.Round(block[i] * quantizerMultipliers[i]);
            }
        }

        public void QuantizeTransposedZigZagScalar(float[] source, int[] destination)
        {
            var multipliers = quantizerMultipliersTransposedZigZag;

            for (var i = 0; i < source.Length; i++)
            {
                // ITU T.81 section A.3.4 says we should round to nearest integer:
                destination[i] = (int)MathF.Round(source[i] * multipliers[i]);
            }
        }

#if NET8_0_OR_GREATER
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void DequantizeTransposedZigZag256(
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
            ref var pMultipliers = ref Unsafe.As<float, Vector256<float>>(
                ref MemoryMarshal.GetArrayDataReference(quantizersTransposedZigZag));

            row0 *= Unsafe.Add(ref pMultipliers, 0);
            row1 *= Unsafe.Add(ref pMultipliers, 1);
            row2 *= Unsafe.Add(ref pMultipliers, 2);
            row3 *= Unsafe.Add(ref pMultipliers, 3);
            row4 *= Unsafe.Add(ref pMultipliers, 4);
            row5 *= Unsafe.Add(ref pMultipliers, 5);
            row6 *= Unsafe.Add(ref pMultipliers, 6);
            row7 *= Unsafe.Add(ref pMultipliers, 7);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void DequantizeTransposedZigZag128(
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
            ref var pMultipliers = ref Unsafe.As<float, Vector128<float>>(
                ref MemoryMarshal.GetArrayDataReference(quantizersTransposedZigZag));

            row0_lo *= Unsafe.Add(ref pMultipliers, 0);
            row0_hi *= Unsafe.Add(ref pMultipliers, 1);
            row1_lo *= Unsafe.Add(ref pMultipliers, 2);
            row1_hi *= Unsafe.Add(ref pMultipliers, 3);
            row2_lo *= Unsafe.Add(ref pMultipliers, 4);
            row2_hi *= Unsafe.Add(ref pMultipliers, 5);
            row3_lo *= Unsafe.Add(ref pMultipliers, 6);
            row3_hi *= Unsafe.Add(ref pMultipliers, 7);
            row4_lo *= Unsafe.Add(ref pMultipliers, 8);
            row4_hi *= Unsafe.Add(ref pMultipliers, 9);
            row5_lo *= Unsafe.Add(ref pMultipliers, 10);
            row5_hi *= Unsafe.Add(ref pMultipliers, 11);
            row6_lo *= Unsafe.Add(ref pMultipliers, 12);
            row6_hi *= Unsafe.Add(ref pMultipliers, 13);
            row7_lo *= Unsafe.Add(ref pMultipliers, 14);
            row7_hi *= Unsafe.Add(ref pMultipliers, 15);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void QuantizeTransposedZigZag256(
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
            ref var pMultipliers = ref Unsafe.As<float, Vector256<float>>(
                ref MemoryMarshal.GetArrayDataReference(quantizerMultipliersTransposedZigZag));

            row0 *= Unsafe.Add(ref pMultipliers, 0);
            row1 *= Unsafe.Add(ref pMultipliers, 1);
            row2 *= Unsafe.Add(ref pMultipliers, 2);
            row3 *= Unsafe.Add(ref pMultipliers, 3);
            row4 *= Unsafe.Add(ref pMultipliers, 4);
            row5 *= Unsafe.Add(ref pMultipliers, 5);
            row6 *= Unsafe.Add(ref pMultipliers, 6);
            row7 *= Unsafe.Add(ref pMultipliers, 7);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void QuantizeTransposedZigZag128(
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
            ref var pMultipliers = ref Unsafe.As<float, Vector128<float>>(
                ref MemoryMarshal.GetArrayDataReference(quantizerMultipliersTransposedZigZag));

            row0_lo *= Unsafe.Add(ref pMultipliers, 0);
            row0_hi *= Unsafe.Add(ref pMultipliers, 1);
            row1_lo *= Unsafe.Add(ref pMultipliers, 2);
            row1_hi *= Unsafe.Add(ref pMultipliers, 3);
            row2_lo *= Unsafe.Add(ref pMultipliers, 4);
            row2_hi *= Unsafe.Add(ref pMultipliers, 5);
            row3_lo *= Unsafe.Add(ref pMultipliers, 6);
            row3_hi *= Unsafe.Add(ref pMultipliers, 7);
            row4_lo *= Unsafe.Add(ref pMultipliers, 8);
            row4_hi *= Unsafe.Add(ref pMultipliers, 9);
            row5_lo *= Unsafe.Add(ref pMultipliers, 10);
            row5_hi *= Unsafe.Add(ref pMultipliers, 11);
            row6_lo *= Unsafe.Add(ref pMultipliers, 12);
            row6_hi *= Unsafe.Add(ref pMultipliers, 13);
            row7_lo *= Unsafe.Add(ref pMultipliers, 14);
            row7_hi *= Unsafe.Add(ref pMultipliers, 15);
        }
#endif

        public JpegQuantizationTable Quality(int quality, bool forceBaseline = true)
        {
            // The quality concept does not have its origins in the standard, but was introduced by the Independent JPEG Group (IJG).
            //
            // It is documented in this paper:
            // https://dfrws.org/sites/default/files/session-files/2008_USA_paper-using_jpeg_quantization_tables_to_identify_imagery_processed_by_software.pdf
            //
            // And on Stack Overflow:
            // https://stackoverflow.com/a/29216609

            if (quality < 1) quality = 1;
            if (quality > 100) quality = 100;

            var result = new ushort[Size];

            var S = quality < 50
                ? 5000 / quality
                : 200 - quality * 2;

            // Baseline JPEG (ITU T.81 section B.2.4.1) only allows 8-bit quantizers. Since JpegEncoder writes
            // 8-bit DQT segments, quantizers must be clamped to 255, corresponding to force_baseline in libjpeg.
            // Otherwise the encoder would quantize with a larger value than declared in the file, corrupting the
            // image for all decoders.
            var maxQuantizer = forceBaseline ? byte.MaxValue : ushort.MaxValue;

            for (var i = 0; i < Size; i++)
            {
                result[i] = (ushort)MathUtils.Clamp((S * quantizers[i] + 50) / 100, 1, maxQuantizer);
            }

            return new JpegQuantizationTable(result);
        }

        public int EstimateQuality(JpegQuantizationTable standardTable)
        {
            var sumCurrentTable = 0;
            var sumStandardTable = 0;

            const int HorizontalSamples = 3;
            const int VerticalSamples = 3;
            const int SampleCount = HorizontalSamples * VerticalSamples;

            for (var y = 0; y < VerticalSamples; y++)
            {
                for (var x = 0; x < HorizontalSamples; x++)
                {
                    sumCurrentTable += quantizers[y * 8 + x];
                    sumStandardTable += standardTable.quantizers[y * 8 + x];
                }
            }

            if (sumCurrentTable == SampleCount)
            {
                return 100;
            }

            //                   Ts = (S * Tb + 50) / 100    =>
            //             100 * Ts = S * Tb + 50            =>
            //        100 * Ts - 50 = S * Tb                 =>
            // (100 * Ts - 50) / Tb = S                      =>

            var S = (100 * sumCurrentTable - 50 * SampleCount) / sumStandardTable;

            // If  Q < 50  then  S = 5000 / Q  =>  Q = 5000 / S
            // If  Q > 50  then  S = 200 - 2Q  =>  Q = (200 - S) / 2
            // If  Q = 50  then  S = 100
            //
            // =>
            //
            // If  S > 100  then  Q = 5000 / S
            // If  S < 100  then  Q = (200 - S) / 2

            var quality = S > 100
                ? 5000 / S
                : (200 - S) / 2;

            return quality;
        }
    }
}
