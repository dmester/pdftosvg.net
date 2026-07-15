// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Shims
{
    public class ArraySegmentExtensionsTests
    {
        [Test]
        public void ToArray()
        {
            var segment = new ArraySegment<int>(new[] { 1, 2, 3, 4, 5 }, 1, 3);
            var array = segment.ToArray();
            Assert.AreEqual(new[] { 2, 3, 4 }, array);
        }

        [Test]
        public void CopyTo_ArrayIndex()
        {
            var segment = new ArraySegment<int>(new[] { 1, 2, 3, 4, 5 }, 1, 3);
            var destination = new int[7];

            segment.CopyTo(destination, 2);

            Assert.AreEqual(new[] { 0, 0, 2, 3, 4, 0, 0 }, destination);
        }

        [Test]
        public void CopyTo_Array()
        {
            var segment = new ArraySegment<int>(new[] { 1, 2, 3, 4, 5 }, 1, 3);
            var destination = new int[7];

            segment.CopyTo(destination);

            Assert.AreEqual(new[] { 2, 3, 4, 0, 0, 0, 0 }, destination);
        }
    }
}
