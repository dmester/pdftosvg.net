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
    [Parallelizable(ParallelScope.Children)]
    public class JpegEncoderTests
    {
        [Test]
        public void Normal()
        {
            Encode("jpegencoder-normal.jpg", encoder =>
            {
                // No settings
            });
        }

        [Test]
        [TestCase("jpegencoder-420.jpg", (int)JpegChromaSubSampling.Ratio420)]
        [TestCase("jpegencoder-422.jpg", (int)JpegChromaSubSampling.Ratio422)]
        [TestCase("jpegencoder-440.jpg", (int)JpegChromaSubSampling.Ratio440)]
        [TestCase("jpegencoder-444.jpg", (int)JpegChromaSubSampling.Ratio444)]
        public void ChromaSubsampling(string name, int value)
        {
            Encode(name, encoder =>
            {
                encoder.ChromaSubSampling = (JpegChromaSubSampling)value;
            });
        }

        [Test]
        [TestCase("jpegencoder-quality10.jpg", 10)]
        [TestCase("jpegencoder-quality50.jpg", 50)]
        [TestCase("jpegencoder-quality99.jpg", 99)]
        [TestCase("jpegencoder-quality100.jpg", 100)]
        public void Quality(string name, int quality)
        {
            Encode(name, encoder =>
            {
                encoder.Quality = quality;
            });
        }

        [Test]
        public void RestartInterval()
        {
            Encode("jpegencoder-restartinterval.jpg", encoder =>
            {
                encoder.RestartInterval = 30;
            });
        }

        [Test]
        public void WriteBlocks_SourceBlocksNull()
        {
            var encoder = new JpegEncoder { Width = 10, Height = 10 };

            Assert.Throws<ArgumentNullException>(() =>
                encoder.WriteBlocks(null, 1));
        }

        [Test]
        public void WriteBlocks_NegativeBlockCount()
        {
            var sourceBlocks = new float[64];
            var encoder = new JpegEncoder { Width = 10, Height = 10 };

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                encoder.WriteBlocks(sourceBlocks, -1));
        }

        [Test]
        public void WriteBlocks_TooManyBlockCount()
        {
            var sourceBlocks = new float[127];
            var encoder = new JpegEncoder { Width = 10, Height = 10 };

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                encoder.WriteBlocks(sourceBlocks, 2));
        }

        [Test]
        public void WriteBlocks_BeforeMetadata()
        {
            var sourceBlocks = new float[128];
            var encoder = new JpegEncoder { Width = 10, Height = 10 };

            Assert.Throws<InvalidOperationException>(() =>
                encoder.WriteBlocks(sourceBlocks, 2));
        }

        private void Encode(string filename, Action<JpegEncoder> setup)
        {
            var testData = new JpegTestData();
            testData.Load(Path.Combine(TestFiles.InputDirectory, "jpegsamples-ycc.raw"));

            var expectedPath = Path.Combine(TestFiles.ExpectedDirectory, filename);
            var actualPath = Path.Combine(TestFiles.OutputDirectory(sync: true), filename);

            var encoder = new JpegEncoder
            {
                Width = testData.Width,
                Height = testData.Height,
                ColorSpace = testData.ColorSpace,
            };

            setup(encoder);

            encoder.WriteMetadata();
            encoder.WriteImageData(testData.Samples);
            encoder.WriteEndImage();

            var actualData = encoder.ToByteArray();
            File.WriteAllBytes(actualPath, actualData);

            var expectedData = File.ReadAllBytes(expectedPath);
            Assert.AreEqual(expectedData, actualData);
        }
    }
}
