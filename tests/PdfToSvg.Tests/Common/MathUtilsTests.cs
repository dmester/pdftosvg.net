// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Common
{
    public class MathUtilsTests
    {
        [TestCase(11999, 3, 2)]
        [TestCase(119999, 3, 2)]
        [TestCase(1199999, 3, 2)]
        [TestCase(1200000, 3, 0)]
        [TestCase(1200001, 3, 1)]
        [TestCase(1200002, 3, 2)]
        [TestCase(1200003, 3, 0)]
        [TestCase(7000000, 7, 0)]
        [TestCase(7000001, 7, 1)]
        [TestCase(7000006, 7, 6)]
        [TestCase(7000007, 7, 0)]
        public void ModBE(int dividend, byte divisor, int expectedResult)
        {
            var binaryDividend = new byte[]
            {
                unchecked((byte)(dividend >> 24)),
                unchecked((byte)(dividend >> 16)),
                unchecked((byte)(dividend >> 8)),
                unchecked((byte)dividend)
            };

            Assert.AreEqual(expectedResult, MathUtils.ModBE(binaryDividend, divisor));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(15)]
        [TestCase(16)]
        [TestCase(17)]
        [TestCase(1023)]
        [TestCase(1024)]
        [TestCase(1025)]
        public void IntLog2(int value)
        {
            var expected = (int)Math.Floor(Math.Log(value) / Math.Log(2));
            Assert.AreEqual(expected, MathUtils.IntLog2(value));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(15)]
        [TestCase(16)]
        [TestCase(17)]
        [TestCase(1023)]
        [TestCase(1024)]
        [TestCase(1025)]
        public void IntLog2Ceil(int value)
        {
            var expected = (int)Math.Ceiling(Math.Log(value) / Math.Log(2));
            Assert.AreEqual(expected, MathUtils.IntLog2Ceil(value));
        }

        [TestCase(-3, 3, -1)]
        [TestCase(-2, 3, -1)]
        [TestCase(-1, 3, -1)]
        [TestCase(0, 3, 0)]
        [TestCase(1, 3, 0)]
        [TestCase(2, 3, 0)]
        [TestCase(3, 3, 1)]
        [TestCase(-3, -3, 1)]
        [TestCase(-2, -3, 0)]
        [TestCase(-1, -3, 0)]
        [TestCase(0, -3, 0)]
        [TestCase(1, -3, -1)]
        [TestCase(2, -3, -1)]
        [TestCase(3, -3, -1)]
        public void FloorDiv(int x, int y, int result)
        {
            var actual = MathUtils.FloorDiv(x, y);
            Assert.AreEqual(result, actual);
        }

        [TestCase(int.MinValue, 10, 20, 10)]
        [TestCase(9, 10, 20, 10)]
        [TestCase(10, 10, 20, 10)]
        [TestCase(11, 10, 20, 11)]
        [TestCase(19, 10, 20, 19)]
        [TestCase(20, 10, 20, 20)]
        [TestCase(21, 10, 20, 20)]
        [TestCase(int.MaxValue, 10, 20, 20)]
        public void Clamp_Int32(int value, int min, int max, int expectedResult)
        {
            Assert.AreEqual(expectedResult, MathUtils.Clamp(value, min, max));
        }

        [TestCase(long.MinValue, 10, 20, 10)]
        [TestCase(9, 10, 20, 10)]
        [TestCase(10, 10, 20, 10)]
        [TestCase(11, 10, 20, 11)]
        [TestCase(19, 10, 20, 19)]
        [TestCase(20, 10, 20, 20)]
        [TestCase(21, 10, 20, 20)]
        [TestCase(long.MaxValue, 10, 20, 20)]
        public void Clamp_Int64(long value, long min, long max, long expectedResult)
        {
            Assert.AreEqual(expectedResult, MathUtils.Clamp(value, min, max));
        }

        [TestCase(float.NaN, 10, 20, float.NaN)]
        [TestCase(float.NegativeInfinity, 10, 20, 10)]
        [TestCase(float.MinValue, 10, 20, 10)]
        [TestCase(9, 10, 20, 10)]
        [TestCase(10, 10, 20, 10)]
        [TestCase(11, 10, 20, 11)]
        [TestCase(19, 10, 20, 19)]
        [TestCase(20, 10, 20, 20)]
        [TestCase(21, 10, 20, 20)]
        [TestCase(float.MaxValue, 10, 20, 20)]
        [TestCase(float.PositiveInfinity, 10, 20, 20)]
        public void Clamp_Single(float value, float min, float max, float expectedResult)
        {
            Assert.AreEqual(expectedResult, MathUtils.Clamp(value, min, max));
        }

        [TestCase(double.NaN, 10, 20, double.NaN)]
        [TestCase(double.NegativeInfinity, 10, 20, 10)]
        [TestCase(double.MinValue, 10, 20, 10)]
        [TestCase(9, 10, 20, 10)]
        [TestCase(10, 10, 20, 10)]
        [TestCase(11, 10, 20, 11)]
        [TestCase(19, 10, 20, 19)]
        [TestCase(20, 10, 20, 20)]
        [TestCase(21, 10, 20, 20)]
        [TestCase(double.MaxValue, 10, 20, 20)]
        [TestCase(double.PositiveInfinity, 10, 20, 20)]
        public void Clamp_Double(double value, double min, double max, double expectedResult)
        {
            Assert.AreEqual(expectedResult, MathUtils.Clamp(value, min, max));
        }
    }
}
