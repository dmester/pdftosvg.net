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
    }
}
