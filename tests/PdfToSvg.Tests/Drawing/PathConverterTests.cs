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
    class PathConverterTests
    {
        [Test]
        public void ConvertToRectangle_CounterClockwise_Closed()
        {
            {
                var path = new PathData();
                path.MoveTo(1, 10);
                path.LineTo(1, 20);
                path.LineTo(2, 20);
                path.LineTo(2, 10);
                path.LineTo(1, 10);
                path.ClosePath();

                Assert.IsTrue(PathConverter.TryConvertToRectangle(path, out var rect));
                Assert.AreEqual(1, rect.X1);
                Assert.AreEqual(2, rect.X2);
                Assert.AreEqual(10, rect.Y1);
                Assert.AreEqual(20, rect.Y2);
            }

            {
                var path = new PathData();
                path.MoveTo(1, 10);
                path.LineTo(1, 20);
                path.LineTo(2, 20);
                path.LineTo(2, 10);
                path.LineTo(1, 10);

                Assert.IsTrue(PathConverter.TryConvertToRectangle(path, out var rect));
                Assert.AreEqual(1, rect.X1);
                Assert.AreEqual(2, rect.X2);
                Assert.AreEqual(10, rect.Y1);
                Assert.AreEqual(20, rect.Y2);
            }
        }

        [Test]
        public void ConvertToRectangle_Clockwise_Closed()
        {
            {
                var path = new PathData();
                path.MoveTo(1, 10);
                path.LineTo(2, 10);
                path.LineTo(2, 20);
                path.LineTo(1, 20);
                path.LineTo(1, 10);
                path.ClosePath();

                Assert.IsTrue(PathConverter.TryConvertToRectangle(path, out var rect));
                Assert.AreEqual(1, rect.X1);
                Assert.AreEqual(2, rect.X2);
                Assert.AreEqual(10, rect.Y1);
                Assert.AreEqual(20, rect.Y2);
            }

            {
                var path = new PathData();
                path.MoveTo(1, 10);
                path.LineTo(2, 10);
                path.LineTo(2, 20);
                path.LineTo(1, 20);
                path.LineTo(1, 10);

                Assert.IsTrue(PathConverter.TryConvertToRectangle(path, out var rect));
                Assert.AreEqual(1, rect.X1);
                Assert.AreEqual(2, rect.X2);
                Assert.AreEqual(10, rect.Y1);
                Assert.AreEqual(20, rect.Y2);
            }
        }

        [Test]
        public void ConvertToRectangle_Clockwise_Unclosed()
        {
            var path = new PathData();
            path.MoveTo(1, 10);
            path.LineTo(2, 10);
            path.LineTo(2, 20);
            path.LineTo(1, 20);

            Assert.IsTrue(PathConverter.TryConvertToRectangle(path, out var rect));
            Assert.AreEqual(1, rect.X1);
            Assert.AreEqual(2, rect.X2);
            Assert.AreEqual(10, rect.Y1);
            Assert.AreEqual(20, rect.Y2);
        }

        [Test]
        public void ConvertToRectangle_NotARect()
        {
            {
                var path = new PathData();
                path.MoveTo(1, 10);
                path.LineTo(2, 20);
                path.LineTo(2, 10);
                path.LineTo(1, 20);

                Assert.IsFalse(PathConverter.TryConvertToRectangle(path, out var rect));
            }

            {
                var path = new PathData();
                path.MoveTo(1, 10);
                path.LineTo(2.1, 10);
                path.LineTo(2, 20);
                path.LineTo(1, 20);

                Assert.IsFalse(PathConverter.TryConvertToRectangle(path, out var rect));
            }
        }

    }
}
