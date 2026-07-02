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
            arr[2] = 10000;

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
    }
}
