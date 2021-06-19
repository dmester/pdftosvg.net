// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Drawing
{
    public class SvgConversionTests
    {
        [TestCase(1, 0, 0, 1, 0, 0, "none")]
        [TestCase(1.000001, 0.000005, 0, 1, 0, 0, "none")]
        [TestCase(1, 0, 0, 1.000001, 1.100001, 1.111111111, "translate(1.1 1.1111)")]
        [TestCase(1, 0, 0, 2, 0, 0, "matrix(1 0 0 2 0 0)")]
        [TestCase(1, 2, 3, 4, 5, 6, "matrix(1 2 3 4 5 6)")]
        public void FormatMatrix(double a, double b, double c, double d, double e, double f, string expectedResult)
        {
            Assert.AreEqual(expectedResult, SvgConversion.Matrix(new Matrix(a, b, c, d, e, f)));
        }
    }
}
