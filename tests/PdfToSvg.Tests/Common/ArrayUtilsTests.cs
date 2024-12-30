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

        [Test]
        public void Add()
        {
            var actual = ArrayUtils.Add<byte>(new byte[] { 1, 2, 3, 4 }, 5, 6, 7);

            Assert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6, 7 }, actual);
        }

        [Test]
        public void StartsWith_EmptyHaystack()
        {
            var haystack = new byte[0];
            var needle = new byte[] { 1, 2, 3 };

            Assert.IsFalse(ArrayUtils.StartsWith(haystack, 0, 0, needle));
        }

        [Test]
        public void StartsWith_True()
        {
            var haystack = new byte[] { 0, 0, 1, 2, 3, 4, 5 };
            var needle = new byte[] { 1, 2, 3 };

            Assert.IsTrue(ArrayUtils.StartsWith(haystack, 2, 5, needle));
        }

        [Test]
        public void StartsWith_False()
        {
            var haystack = new byte[] { 0, 0, 1, 2, 4, 4, 5 };
            var needle = new byte[] { 1, 2, 3 };

            Assert.IsFalse(ArrayUtils.StartsWith(haystack, 2, 5, needle));
        }

        [Test]
        public void StartsWith_Exact()
        {
            var haystack = new byte[] { 0, 0, 1, 2, 3, 4, 5 };
            var needle = new byte[] { 1, 2, 3 };

            Assert.IsTrue(ArrayUtils.StartsWith(haystack, 2, 3, needle));
        }

        [Test]
        public void StartsWith_OutsideSpan()
        {
            var haystack = new byte[] { 0, 0, 1, 2, 3, 4, 5 };
            var needle = new byte[] { 1, 2, 3 };

            Assert.IsFalse(ArrayUtils.StartsWith(haystack, 2, 2, needle));
        }
    }
}
