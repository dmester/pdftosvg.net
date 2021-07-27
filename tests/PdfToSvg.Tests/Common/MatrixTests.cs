// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Common
{
    public class MatrixTests
    {
        [Test]
        public void Matrix3x3_Multiply()
        {
            Assert.AreEqual(new Matrix3x3(10, 10, 6, 11, 11, 9, 13, 11, 9),
                new Matrix3x3(1, 2, 1, 2, 1, 2, 2, 2, 1) * new Matrix3x3(3, 1, 3, 3, 3, 1, 1, 3, 1));
        }

        [Test]
        public void Matrix3x3_1x3_Multiply()
        {
            Assert.AreEqual(new Matrix1x3(14, 32, 50),
                new Matrix3x3(1, 2, 3, 4, 5, 6, 7, 8, 9) * new Matrix1x3(1, 2, 3));
        }

        [Test]
        public void Matrix3x3_Transpose()
        {
            Assert.AreEqual(new Matrix3x3(1, 4, 7, 2, 5, 8, 3, 6, 9),
                new Matrix3x3(1, 2, 3, 4, 5, 6, 7, 8, 9).Transpose());
        }
    }
}
