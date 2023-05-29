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
        public void Quality0()
        {
            var quality0 = table.Quality(0);
            Assert.AreEqual(2, quality0.Quantizers[0]);
            Assert.AreEqual(200, quality0.Quantizers[1]);
            Assert.AreEqual(20000, quality0.Quantizers[2]);
        }

        [Test]
        public void Quality50()
        {
            var quality0 = table.Quality(50);
            Assert.AreEqual(1, quality0.Quantizers[0]);
            Assert.AreEqual(100, quality0.Quantizers[1]);
            Assert.AreEqual(10000, quality0.Quantizers[2]);
        }

        [Test]
        public void Quality100()
        {
            var quality0 = table.Quality(100);
            Assert.AreEqual(1, quality0.Quantizers[0]);
            Assert.AreEqual(1, quality0.Quantizers[1]);
            Assert.AreEqual(1, quality0.Quantizers[2]);
        }
    }
}
