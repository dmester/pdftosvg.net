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
    public class ArrayUtilsTests
    {
        [Test]
        public void Concat()
        {
            var actual = ArrayUtils.Concat<byte>(
                null,
                new byte[0],
                new byte[] { 1, 2, 3, 4, 5 },
                null,
                new byte[] { 6, 7, 8, 9 },
                new byte[0],
                null);

            Assert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, actual);
        }
    }
}
