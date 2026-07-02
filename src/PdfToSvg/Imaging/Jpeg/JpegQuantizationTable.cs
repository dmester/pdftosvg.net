// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpeg
{
    internal struct JpegQuantizationTable
    {
        public const int Size = 64;

        public ushort[] Quantizers { get; }

        public JpegQuantizationTable(params ushort[] quantizers)
        {
            if (quantizers.Length != Size)
            {
                throw new ArgumentException("Expected quantization table with " + Size + " elements.", nameof(quantizers));
            }

            Quantizers = quantizers;
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

        public void Dequantize(short[] block)
        {
            var quantizers = Quantizers;

            for (var i = 0; i < block.Length; i++)
            {
                block[i] = (short)(block[i] * quantizers[i]);
            }
        }

        public void Quantize(short[] block)
        {
            var quantizers = Quantizers;

            for (var i = 0; i < block.Length; i++)
            {
                block[i] = (short)(0.5f + (float)block[i] / quantizers[i]);
            }
        }

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
                result[i] = (ushort)MathUtils.Clamp((S * Quantizers[i] + 50) / 100, 1, maxQuantizer);
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
                    sumCurrentTable += Quantizers[y * 8 + x];
                    sumStandardTable += standardTable.Quantizers[y * 8 + x];
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
