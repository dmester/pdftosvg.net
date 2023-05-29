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
    internal class JpegZigZagTests
    {
        [Test]
        public void Roundtrip()
        {
            var original = new byte[64];

            for (var i = 0; i < original.Length; i++)
            {
                original[i] = (byte)i;
            }

            var zigzagged = new byte[64];
            var roundtripped = new byte[64];

            JpegZigZag.ZigZag(original, zigzagged);
            JpegZigZag.ReverseZigZag(zigzagged, roundtripped);

            Assert.AreEqual(original, roundtripped);
        }
    }
}
