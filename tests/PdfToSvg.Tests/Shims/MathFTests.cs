// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

#if !NETCOREAPP2_0_OR_GREATER
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Shims
{
    public class MathFTests
    {
        [Test]
        [TestCase(float.MinValue)]
        [TestCase(float.MaxValue)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(-2.4f)]
        [TestCase(-2.5f)]
        [TestCase(-2.6f)]
        [TestCase(-1.4f)]
        [TestCase(-1.5f)]
        [TestCase(-1.6f)]
        [TestCase(-0f)]
        [TestCase(0f)]
        [TestCase(1.4f)]
        [TestCase(1.5f)]
        [TestCase(1.6f)]
        [TestCase(2.4f)]
        [TestCase(2.5f)]
        [TestCase(2.6f)]
        public void Floor(float value)
        {
            var result = MathF.Floor(value);
            var expected = (float)Math.Floor(value);

            Assert.AreEqual(expected, result);
        }

        [Test]
        [TestCase(float.MinValue)]
        [TestCase(float.MaxValue)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(-2.4f)]
        [TestCase(-2.5f)]
        [TestCase(-2.6f)]
        [TestCase(-1.4f)]
        [TestCase(-1.5f)]
        [TestCase(-1.6f)]
        [TestCase(-0f)]
        [TestCase(0f)]
        [TestCase(1.4f)]
        [TestCase(1.5f)]
        [TestCase(1.6f)]
        [TestCase(2.4f)]
        [TestCase(2.5f)]
        [TestCase(2.6f)]
        public void Ceiling(float value)
        {
            var expected = (float)Math.Ceiling(value);

            var result = MathF.Ceiling(value);

            Assert.AreEqual(expected, result);
        }

        [Test]
        [TestCase(float.MinValue)]
        [TestCase(float.MaxValue)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(-2.4f)]
        [TestCase(-2.5f)]
        [TestCase(-2.6f)]
        [TestCase(-1.4f)]
        [TestCase(-1.5f)]
        [TestCase(-1.6f)]
        [TestCase(-0f)]
        [TestCase(0f)]
        [TestCase(1.4f)]
        [TestCase(1.5f)]
        [TestCase(1.6f)]
        [TestCase(2.4f)]
        [TestCase(2.5f)]
        [TestCase(2.6f)]
        public void Round(float value)
        {
            var expected = (float)Math.Round(value);

            var result = MathF.Round(value);

            Assert.AreEqual(expected, result);
        }

        [Test]
        [TestCase(14f, 0f)]
        [TestCase(14f, 2f)]
        [TestCase(14.5f, 2.5f)]
        public void Pow(float x, float y)
        {
            var expected = (float)Math.Pow(x, y);

            var result = MathF.Pow(x, y);

            Assert.AreEqual(expected, result);
        }
    }
}
#endif
