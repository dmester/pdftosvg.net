// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpeg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Images.Jpeg
{
    public class JpegTestDataTests
    {
        [Test]
        public void Roundtrip()
        {
            var original = new JpegTestData();
            var loaded = new JpegTestData();

            var random = new Random(0);
            original.Samples = new short[100];
            for (var i = 0; i < original.Samples.Length; i++)
            {
                original.Samples[i] = unchecked((short)random.Next());
            }

            original.Width = 123;
            original.Height = 456;
            original.ColorSpace = JpegColorSpace.Cmyk;

            var tempFile = Path.GetTempFileName();
            try
            {
                original.Save(tempFile);
                loaded.Load(tempFile);
            }
            finally
            {
                File.Delete(tempFile);
            }

            Assert.AreEqual(original.Width, loaded.Width);
            Assert.AreEqual(original.Height, loaded.Height);
            Assert.AreEqual(original.ColorSpace, loaded.ColorSpace);
            Assert.AreEqual(original.Samples, loaded.Samples);
        }
    }
}
