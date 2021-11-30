// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Fonts.CharStrings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Fonts.CharStrings
{
    internal class CharStringPathTests
    {
        [Test]
        public void Curve1()
        {
            var path = new CharStringPath();

            path.RMoveTo(42, 96);
            path.RRCurveTo(-1, -1, 2, 2, 0, 0);

            Assert.AreEqual(41, path.MinX);
            Assert.AreEqual(43, path.MaxX);
            Assert.AreEqual(95, path.MinY);
            Assert.AreEqual(97, path.MaxY);
        }

        [Test]
        public void Curve2()
        {
            var path = new CharStringPath();

            path.RMoveTo(42, 96);
            path.RRCurveTo(-1, -1, 0, 0, 2, 2);

            Assert.AreEqual(41, path.MinX);
            Assert.AreEqual(43, path.MaxX);
            Assert.AreEqual(95, path.MinY);
            Assert.AreEqual(97, path.MaxY);
        }

        [Test]
        public void Line()
        {
            var path = new CharStringPath();

            path.RMoveTo(42, 96);

            path.RLineTo(-1, 1);

            Assert.AreEqual(41, path.MinX);
            Assert.AreEqual(42, path.MaxX);
            Assert.AreEqual(96, path.MinY);
            Assert.AreEqual(97, path.MaxY);

            path.RLineTo(2, -2);

            Assert.AreEqual(41, path.MinX);
            Assert.AreEqual(43, path.MaxX);
            Assert.AreEqual(95, path.MinY);
            Assert.AreEqual(97, path.MaxY);
        }

        [Test]
        public void Move()
        {
            var path = new CharStringPath();

            path.RMoveTo(100, 100);
            path.RMoveTo(42, 96);

            Assert.AreEqual(double.MaxValue, path.MinX);
            Assert.AreEqual(double.MinValue, path.MaxX);
            Assert.AreEqual(double.MaxValue, path.MinY);
            Assert.AreEqual(double.MinValue, path.MaxY);
        }
    }
}
