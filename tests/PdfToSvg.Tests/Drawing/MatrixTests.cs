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
    class MatrixTests
    {
        [Test]
        public void Translate()
        {
            Assert.AreEqual(new Matrix(1, 0, 0, 1, 42, -42), Matrix.Translate(42, -42));
        }

        [Test]
        public void TranslateMatrix()
        {
            var source = new Matrix(16, 25, 34, 43, 52, 61);
            Assert.AreEqual(Matrix.Translate(-31, 47) * source, Matrix.Translate(-31, 47, source));
        }

        [Test]
        public void Scale()
        {
            Assert.AreEqual(new Matrix(2, 0, 0, -2, 0, 0), Matrix.Scale(2, -2));
        }

        [Test]
        public void ScaleMatrix()
        {
            var source = new Matrix(16, 25, 34, 43, 52, 61);
            Assert.AreEqual(Matrix.Scale(-31, 47) * source, Matrix.Scale(-31, 47, source));
        }

        [Test]
        public void Multiply()
        {
            Assert.AreEqual(new Matrix(10, 13, 22, 29, 40, 52), new Matrix(1, 2, 3, 4, 5, 6) * new Matrix(2, 3, 4, 5, 6, 7));
        }

        [Test]
        public void DecomposeTranslation()
        {
            var source = new Matrix(12, 23, 34, 45, 0, 0);
            var input = Matrix.Translate(42, 47) * source;

            input.DecomposeTranslate(out var dx, out var dy, out var remainder);

            Assert.AreEqual(42, dx);
            Assert.AreEqual(47, dy);
            Assert.AreEqual(source, remainder);
        }

        [Test]
        public void DecomposeScale()
        {
            var source = new Matrix(1, 0, 0, 1, 10, 10);
            var input = Matrix.Scale(42, 84) * source;

            input.DecomposeScale(out var scale, out var remainder);

            Assert.AreEqual(42, scale);
            Assert.AreEqual(new Matrix(1, 0, 0, 2, 10, 10), remainder);
        }

    }
}
