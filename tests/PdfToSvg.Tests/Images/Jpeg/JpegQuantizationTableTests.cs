// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Images.Jpeg
{
    internal class JpegQuantizationTableTests
    {
        private readonly JpegQuantizationTable table;

        public JpegQuantizationTableTests()
        {
            var arr = new ushort[JpegQuantizationTable.Size];

            arr[0] = 1;
            arr[1] = 100;
            arr[2] = 200;

            table = new JpegQuantizationTable(arr);
        }

        [Test]
        public void Quality0_Overflow()
        {
            var arr = new ushort[JpegQuantizationTable.Size];

            arr[0] = 1;
            arr[1] = 100;
            arr[2] = ushort.MaxValue;

            var table = new JpegQuantizationTable(arr);

            var quality0 = table.Quality(0, forceBaseline: false);
            Assert.AreEqual(50, quality0.Quantizers.ElementAt(0));
            Assert.AreEqual(5000, quality0.Quantizers.ElementAt(1));
            Assert.AreEqual(ushort.MaxValue, quality0.Quantizers.ElementAt(2));
        }

        [Test]
        public void Quality0()
        {
            var quality0 = table.Quality(1, forceBaseline: false);
            Assert.AreEqual(50, quality0.Quantizers.ElementAt(0));
            Assert.AreEqual(5000, quality0.Quantizers.ElementAt(1));
            Assert.AreEqual(10000, quality0.Quantizers.ElementAt(2));

            // Since S = 5000 / Q for low Q values, quality cannot be 0
            Assert.AreEqual(1, quality0.EstimateQuality(table));
        }

        [Test]
        public void Quality1()
        {
            var quality1 = table.Quality(1, forceBaseline: false);
            Assert.AreEqual(50, quality1.Quantizers.ElementAt(0));
            Assert.AreEqual(5000, quality1.Quantizers.ElementAt(1));
            Assert.AreEqual(10000, quality1.Quantizers.ElementAt(2));

            Assert.AreEqual(1, quality1.EstimateQuality(table));
        }

        [Test]
        public void Quality_ForceBaseline_ClampsQuantizersTo255()
        {
            // Baseline JPEG only supports 8-bit quantizers. Quantizers exceeding 255 would be truncated when
            // written to an 8-bit DQT segment, desynchronizing the declared table from the applied quantization.
            foreach (var standardTable in new[] { JpegQuantizationTable.Luminance, JpegQuantizationTable.Chrominance })
            {
                for (var quality = 1; quality <= 100; quality++)
                {
                    var quantized = standardTable.Quality(quality);

                    foreach (var quantizer in quantized.Quantizers)
                    {
                        Assert.LessOrEqual((int)quantizer, byte.MaxValue,
                            "Quantizer out of baseline range at quality " + quality);
                        Assert.GreaterOrEqual((int)quantizer, 1,
                            "Quantizer must be positive at quality " + quality);
                    }
                }
            }
        }

        [Test]
        public void Quality50()
        {
            var quality50 = table.Quality(50);

            Assert.AreEqual(1, quality50.Quantizers.ElementAt(0));
            Assert.AreEqual(100, quality50.Quantizers.ElementAt(1));
            Assert.AreEqual(200, quality50.Quantizers.ElementAt(2));

            Assert.AreEqual(50, quality50.EstimateQuality(table));
        }

        [Test]
        public void Quality100()
        {
            var quality100 = table.Quality(100);
            Assert.AreEqual(1, quality100.Quantizers.ElementAt(0));
            Assert.AreEqual(1, quality100.Quantizers.ElementAt(1));
            Assert.AreEqual(1, quality100.Quantizers.ElementAt(2));

            Assert.AreEqual(100, quality100.EstimateQuality(table));
        }

        [Test]
        public void Quantize()
        {
            var block = new int[64];
            block[0] = 100;
            block[1] = 200;
            block[2] = 200;
            block[3] = 200;
            block[4] = 200;
            block[5] = 200;
            block[6] = 200;
            block[7] = 200;
            block[8] = 200;
            block[9] = 200;

            var quantizers = new ushort[JpegQuantizationTable.Size];
            quantizers[0] = 1;
            quantizers[1] = 2;
            quantizers[2] = 3;
            quantizers[3] = 4;
            quantizers[4] = 5;
            quantizers[5] = 6;
            quantizers[6] = 7;
            quantizers[7] = 8;
            quantizers[8] = 9;
            quantizers[9] = 10;

            var table = new JpegQuantizationTable(quantizers);
            table.QuantizeScalar(block);

            Assert.AreEqual(100, block[0]);
            Assert.AreEqual(100, block[1]);
            Assert.AreEqual(67, block[2]);
            Assert.AreEqual(50, block[3]);
            Assert.AreEqual(40, block[4]);
            Assert.AreEqual(33, block[5]);
            Assert.AreEqual(29, block[6]);
            Assert.AreEqual(25, block[7]);
            Assert.AreEqual(22, block[8]);
            Assert.AreEqual(20, block[9]);
        }

        [Test]
        public void Dequantize()
        {
            var block = new float[64];
            block[0] = 100;
            block[1] = 100;
            block[2] = 67;
            block[3] = 50;
            block[4] = 40;
            block[5] = 33;
            block[6] = 29;
            block[7] = 25;
            block[8] = 22;
            block[9] = 2000;

            var quantizers = new ushort[JpegQuantizationTable.Size];
            quantizers[0] = 1;
            quantizers[1] = 2;
            quantizers[2] = 3;
            quantizers[3] = 4;
            quantizers[4] = 5;
            quantizers[5] = 6;
            quantizers[6] = 7;
            quantizers[7] = 8;
            quantizers[8] = 9;
            quantizers[9] = 10;

            var table = new JpegQuantizationTable(quantizers);
            table.DequantizeScalar(block);

            Assert.AreEqual(100f, block[0]);
            Assert.AreEqual(200f, block[1]);
            Assert.AreEqual(201f, block[2]);
            Assert.AreEqual(200f, block[3]);
            Assert.AreEqual(200f, block[4]);
            Assert.AreEqual(198f, block[5]);
            Assert.AreEqual(203f, block[6]);
            Assert.AreEqual(200f, block[7]);
            Assert.AreEqual(198f, block[8]);
            Assert.AreEqual(20000f, block[9]);
        }

#if NET7_0_OR_GREATER
        private void QuantizeTransposedZigZag(Action<float[]> body)
        {
            var random = new Random(0);

            var originalData = new int[64];
            for (var i = 0; i < originalData.Length; i++)
            {
                originalData[i] = random.Next(0, 512);
            }

            // Reference
            var reference = new int[64];
            {
                var transposed = (int[])originalData.Clone();

                // Transpose in DCT
                JpegBlockUtils.TransposeScalar(transposed);

                JpegZigZag.ZigZag(transposed, reference);

                JpegQuantizationTable.Luminance.QuantizeScalar(reference);
            }

            // Actual
            int[] actual;
            {
                var floatData = originalData.Select(x => (float)x).ToArray();

                body(floatData);

                actual = floatData.Select(x => (int)(0.5f + x)).ToArray();

                // To make actual comparable with reference
                JpegBlockUtils.TransposeScalar(actual);
                JpegTestUtils.Apply(JpegZigZag.ZigZag, actual);
            }

            Assert.AreEqual(reference, actual);
        }

        [Test]
        public void QuantizeTransposedZigZag128()
        {
            QuantizeTransposedZigZag(floatData =>
                JpegTestUtils.Apply(JpegQuantizationTable.Luminance.QuantizeTransposedZigZag128, floatData));
        }

        [Test]
        public void QuantizeTransposedZigZag256()
        {
            QuantizeTransposedZigZag(floatData =>
                JpegTestUtils.Apply(JpegQuantizationTable.Luminance.QuantizeTransposedZigZag256, floatData));
        }

        private void DequantizeTransposedZigZag(Action<float[]> body)
        {
            var random = new Random(0);

            var originalData = new float[64];
            for (var i = 0; i < originalData.Length; i++)
            {
                originalData[i] = random.Next(-2048, 2048);
            }

            // Reference
            var reference = (float[])originalData.Clone();
            {
                JpegQuantizationTable.Luminance.DequantizeScalar(reference);

                // ZigZag and transpose after quantization
                JpegTestUtils.Apply(JpegZigZag.ReverseZigZag, reference);
                JpegBlockUtils.TransposeScalar(reference);
            }

            // Actual
            var actual = (float[])originalData.Clone();
            {
                // Transpose and zigzag when reading from file
                JpegTestUtils.Apply(JpegZigZag.ReverseZigZag, actual);
                JpegBlockUtils.TransposeScalar(actual);

                body(actual);
            }

            Assert.AreEqual(reference, actual);
        }

        [Test]
        public void DequantizeTransposedZigZag128()
        {
            DequantizeTransposedZigZag(actual => JpegTestUtils.Apply(JpegQuantizationTable.Luminance.DequantizeTransposedZigZag128, actual));
        }

        [Test]
        public void DequantizeTransposedZigZag256()
        {
            DequantizeTransposedZigZag(actual => JpegTestUtils.Apply(JpegQuantizationTable.Luminance.DequantizeTransposedZigZag256, actual));
        }

#endif
    }
}
