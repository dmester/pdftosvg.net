// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using NUnit.Framework.Internal;
using PdfToSvg.Imaging.Png;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Images.Png
{
    internal class PngEncoderTests
    {
        [TestCase(PngFilter.None, "png-truecolour-none.png")]
        [TestCase(PngFilter.Sub, "png-truecolour-sub.png")]
        [TestCase(PngFilter.Up, "png-truecolour-up.png")]
        [TestCase(PngFilter.Average, "png-truecolour-average.png")]
        [TestCase(PngFilter.Paeth, "png-truecolour-paeth.png")]
        public void EncodeTruecolour(PngFilter filter, string expectedFilename)
        {
            Test(expectedFilename, (inputRgba32, width, height) =>
                PngEncoder.Truecolour(inputRgba32, width, height, filter, 255, 255, 0));
        }

        [TestCase(PngFilter.None, "png-truecolouralpha-none.png")]
        [TestCase(PngFilter.Sub, "png-truecolouralpha-sub.png")]
        [TestCase(PngFilter.Up, "png-truecolouralpha-up.png")]
        [TestCase(PngFilter.Average, "png-truecolouralpha-average.png")]
        [TestCase(PngFilter.Paeth, "png-truecolouralpha-paeth.png")]
        public void EncodeTruecolourWithAlpha(PngFilter filter, string expectedFilename)
        {
            Test(expectedFilename, (inputRgba32, width, height) =>
                PngEncoder.TruecolourWithAlpha(inputRgba32, width, height, filter));
        }

        private delegate byte[] Encoder(byte[] inputRgba32, int width, int height);

        private void Test(string expectedFilename, Encoder encoder)
        {
            var inputPath = Path.Combine(TestFiles.InputDirectory, "alphabitmap.bmp");
            var expectedPath = Path.Combine(TestFiles.ExpectedDirectory, expectedFilename);

            var input = new BitmapReader(File.ReadAllBytes(inputPath));
            var inputRgba32 = input.ReadRgba32();

            var expected = File.Exists(expectedPath) ? PngTestUtils.Recompress(File.ReadAllBytes(expectedPath)) : null;
            var actual = encoder(inputRgba32, input.Width, input.Height);

            var outputDirectory = TestFiles.OutputDirectory(true);
            Directory.CreateDirectory(outputDirectory);
            File.WriteAllBytes(Path.Combine(outputDirectory, expectedFilename), actual);

            Assert.AreEqual(expected, actual);
        }
    }
}
