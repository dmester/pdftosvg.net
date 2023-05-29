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
    internal class JpegDctTests
    {
        [Test]
        public void Dct()
        {
            var input = new short[64];
            var dcted = new short[64];
            var output = new short[64];

            var random = new Random(0);

            for (var i = 0; i < 64; i++)
            {
                input[i] = (short)random.Next(-128, 128);
            }

            JpegDct.Forward(input, dcted);
            JpegDct.Inverse(dcted, output);

            for (var i = 0; i < 64; i++)
            {
                Assert.AreEqual(input[i], output[i], 5d, "Index {0}", i);
            }
        }
    }
}
