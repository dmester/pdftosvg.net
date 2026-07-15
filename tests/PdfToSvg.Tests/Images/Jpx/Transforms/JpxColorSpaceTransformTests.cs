// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx.Transforms;
using System;

namespace PdfToSvg.Tests.Images.Jpx.Transforms
{
    internal class JpxColorSpaceTransformTests
    {
        [Test]
        [TestCase(0)]
        [TestCase(3)]
        [TestCase(4)] // Breakpoint between one and two Vector128
        [TestCase(5)]
        [TestCase(7)]
        [TestCase(8)] // Breakpoint between one and two Vector256
        [TestCase(9)]
        [TestCase(200)]
        public void YccToRgb_PlanarMatchesPerSampleMethod(int count)
        {
            var random = new Random(1);

            var planes = new[] { new float[count], new float[count], new float[count] };
            var expected = new float[3][] { new float[count], new float[count], new float[count] };

            for (var i = 0; i < count; i++)
            {
                planes[0][i] = (float)random.NextDouble();
                planes[1][i] = (float)random.NextDouble();
                planes[2][i] = (float)random.NextDouble();

                JpxColorSpaceTransform.YccToRgb(planes[0][i], planes[1][i], planes[2][i],
                    out expected[0][i], out expected[1][i], out expected[2][i]);
            }

            JpxColorSpaceTransform.YccToRgb(planes, count);

            Assert.AreEqual(expected[0], planes[0]);
            Assert.AreEqual(expected[1], planes[1]);
            Assert.AreEqual(expected[2], planes[2]);
        }

        [Test]
        public void YccToRgb_TransformsOnlyCountSamples()
        {
            var planes = new[]
            {
                new[] { .5f, 42f },
                new[] { .5f, 42f },
                new[] { .5f, 42f },
            };

            JpxColorSpaceTransform.YccToRgb(planes, 1);

            Assert.AreEqual(42f, planes[0][1]);
            Assert.AreEqual(42f, planes[1][1]);
            Assert.AreEqual(42f, planes[2][1]);
        }

        [Test]
        public void YccToRgb_TooFewPlanesThrows()
        {
            var planes = new[] { new float[4], new float[4] };

            Assert.Throws<ArgumentException>(() => JpxColorSpaceTransform.YccToRgb(planes, 4));
        }
    }
}
