// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

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

        public JpegQuantizationTable Quality(int quality)
        {
            if (quality < 0) quality = 0;
            if (quality > 100) quality = 100;

            var result = new ushort[Size];

            for (var i = 0; i < Size; i++)
            {
                // Quality 100 = Identity quantization table
                // Quality  50 = Default table
                // Quality   0 = Default table * 2

                result[i] = (ushort)((Quantizers[i] * (200 - quality * 2) + quality) / 100);
            }

            return new JpegQuantizationTable(result);
        }
    }
}
