// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Drawing
{
    public class BezierTests
    {
        [TestCase(0, 0, 5, 5)]
        [TestCase(0, 1, 4, 5)]
        [TestCase(0, 10, -5, 5)]
        [TestCase(0, 10, 2, 5)]
        [TestCase(0, 2, -5, 5)]
        [TestCase(0, -5, 2, 5)]
        [TestCase(0, 2, 10, 5)]
        [TestCase(0, 10, 10, 0)]
        [TestCase(0, 10, 5, 0)]
        public void GetCubicBounds(double p0, double p1, double p2, double p3)
        {
            var sampleCount = 100;

            var samples = Enumerable
                .Range(0, sampleCount)
                .Select(i => (double)i / (sampleCount - 1))
                .Select(t => Bezier.ComputeCubic(p0, p1, p2, p3, t))
                .ToList();

            var estimatedMin = samples.Min();
            var estimatedMax = samples.Max();

            Bezier.GetCubicBounds(p0, p1, p2, p3, out var computedMin, out var computedMax);

            Assert.AreEqual(estimatedMin, computedMin, 0.01, "min");
            Assert.AreEqual(estimatedMax, computedMax, 0.01, "max");
        }
    }
}
