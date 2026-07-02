// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpeg;

namespace PdfToSvg.Tests.Images.Jpeg
{
    internal class JpegZigZagTests
    {
        [Test]
        public void ReverseZigZag_RoundtripsZigZag()
        {
            var original = new short[64];

            for (var i = 0; i < original.Length; i++)
            {
                original[i] = (short)i;
            }

            var zigzagged = new short[64];
            var roundtripped = new short[64];

            JpegZigZag.ZigZag(original, zigzagged);
            JpegZigZag.ReverseZigZag(zigzagged, roundtripped);

            Assert.AreEqual(original, roundtripped);
        }
    }
}
