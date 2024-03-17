// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Drawing;
using PdfToSvg.Drawing.Paths;
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
        [TestCase(1, 0, 0, 2, 0, 0, "scale(1 2)")]
        [TestCase(2, 0, 0, 2, 0, 0, "scale(2)")]
        [TestCase(1, 2, 3, 4, 5, 6, "matrix(1 2 3 4 5 6)")]
        public void FormatMatrix(double a, double b, double c, double d, double e, double f, string expectedResult)
        {
            Assert.AreEqual(expectedResult, SvgConversion.Matrix(new Matrix(a, b, c, d, e, f)));
        }

        [TestCase(12341.234, "12341")]
        [TestCase(1234.1234, "1234")]
        [TestCase(123.41234, "123")]
        [TestCase(12.341234, "12.34")]
        [TestCase(1.2341234, "1.234")]
        [TestCase(.12341234, ".1234")]
        [TestCase(.02341234, ".0234")]
        public void FormatFontMetric(double input, string expectedResult)
        {
            Assert.AreEqual(expectedResult, SvgConversion.FormatFontMetric(input));
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("\r")]
        [TestCase("\n")]
        [TestCase("🙂")]
        [TestCase("a")]
        [TestCase("abc")]
        [TestCase("你好")]
        public void ReplaceInvalidChars_NoInvalidChars(string s)
        {
            var result = SvgConversion.ReplaceInvalidChars(s);
            Assert.IsTrue(ReferenceEquals(result, s));
        }

        [TestCase("\f", "#")]
        [TestCase(" \f", " #")]
        [TestCase("\f ", "# ")]
        [TestCase(" \f ", " # ")]
        [TestCase(" \f\f ", " ## ")]
        public void ReplaceInvalidChars_InvalidChars(string input, string expectedResult)
        {
            var actualResult = SvgConversion.ReplaceInvalidChars(input, '#');
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestCase("00 01 02 03 04 05 06 07", "########")]
        [TestCase("08 09 0A 0B 0C 0D 0E 0F", "#\t\n##\r##")]
        [TestCase("10 11 12 13 14 15 16 17", "########")]
        [TestCase("18 19 1A 1B 1C 1D 1E 1F", "########")]
        [TestCase("20", " ")]
        public void ReplaceInvalidChars_InvalidHexChars(string input, string expectedResult)
        {
            var decodedInput = Encoding.ASCII.GetString(Hex.Decode(input));
            var actualResult = SvgConversion.ReplaceInvalidChars(decodedInput, '#');
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestCase(123.45612321, 0.0123456, "M123.4561 .0123")]
        public void PathData_MoveTo(double x, double y, string expectedResult)
        {
            var path = new PathData();

            path.MoveTo(x, y);

            Assert.AreEqual(expectedResult, SvgConversion.PathData(path));
        }

        [TestCase(123.45612321, 10.123456, "M0 10l123.4561 .1235")]
        [TestCase(123.45612321, 10.000123456, "M0 10l123.4561 .0001")]
        [TestCase(123.45612321, 10.0000123456, "M0 10h123.4561")]
        [TestCase(0, 12, "M0 10v2")]
        public void PathData_LineTo(double x, double y, string expectedResult)
        {
            var path = new PathData();

            path.MoveTo(0, 10);
            path.LineTo(x, y);

            Assert.AreEqual(expectedResult, SvgConversion.PathData(path));
        }

        [TestCase(0, 1, "L0 1")]
        [TestCase(123.45612321, 0.00123456, "L123.4561 .0012")]
        [TestCase(123.45612321, 10.00123456, "L123.4561 10.0012")]
        public void PathData_LineTo_Init(double x, double y, string expectedResult)
        {
            var path = new PathData();

            path.LineTo(x, y);

            Assert.AreEqual(expectedResult, SvgConversion.PathData(path));
        }

        [Test]
        public void PathData_CurveTo()
        {
            var path = new PathData();

            path.MoveTo(10, 10);
            path.CurveTo(11, 12, 11, 13, 13.5, 14.5);

            Assert.AreEqual("M10 10c1 2,1 3,3.5 4.5", SvgConversion.PathData(path));
        }

        [Test]
        public void PathData_CurveTo_Init()
        {
            var path = new PathData();

            path.CurveTo(11, 12, 11, 13, 13.5, 14.5);

            Assert.AreEqual("C11 12,11 13,13.5 14.5", SvgConversion.PathData(path));
        }

        [Test]
        public void PathData_Rectangle()
        {
            var path = new PathData();

            path.MoveTo(0, 0);
            path.LineTo(1200.42312, 0);
            path.LineTo(1200.42312, 1300.9456789);
            path.LineTo(0, 1300.9456789);
            path.ClosePath();

            Assert.AreEqual("M0 0h1200.4231v1300.9457h-1200.4231z", SvgConversion.PathData(path));
        }

        [Test]
        public void PathData_ClosePath()
        {
            var path = new PathData();

            path.MoveTo(10, 10);
            path.LineTo(10, 15);
            path.LineTo(15, 10);
            path.ClosePath();

            path.MoveTo(15, 15);
            path.LineTo(20, 15);
            path.LineTo(15, 20);
            path.ClosePath();

            Assert.AreEqual("M10 10v5l5 -5zm5 5h5l-5 5z", SvgConversion.PathData(path));
        }
    }
}
