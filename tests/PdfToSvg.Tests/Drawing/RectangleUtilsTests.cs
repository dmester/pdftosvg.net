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
    internal class RectangleUtilsTests
    {
        [Test]
        public void GetBoundingRectangle_Array()
        {
            var pointsArray = new[]
            {
                new Point(4, 6),
                new Point(2, 5),
                new Point(3, 4),
            };

            var bbox = pointsArray.GetBoundingRectangle();

            Assert.AreEqual(new Rectangle(2, 4, 4, 6), bbox);
        }

        [Test]
        public void GetBoundingRectangle_Enumerable()
        {
            var pointsArray = new[]
            {
                new Point(4, 6),
                new Point(2, 5),
                new Point(3, 4),
            };

            var bbox = ((IEnumerable<Point>)pointsArray).GetBoundingRectangle();

            Assert.AreEqual(new Rectangle(2, 4, 4, 6), bbox);
        }
    }
}
