// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx.ImageModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Images.Jpx.ImageModel
{
    public class JpxInlineListTests
    {
        [Test]
        public void Add()
        {
            var list = new JpxInlineList<int>();
            var expected = Enumerable.Range(0, 25).ToArray();

            for (var i = 0; i < expected.Length; i++)
            {
                list.Add(expected[i]);
            }

            Assert.AreEqual(25, list.Count);
            Assert.AreEqual(expected, list.ToArray());
        }

        [Test]
        public void IndexAccessor_HeadTail()
        {
            var list = new JpxInlineList<int> { 1, 2, 3 };

            Assert.AreEqual(1, list[0]);
            Assert.AreEqual(2, list[1]);
            Assert.AreEqual(3, list[2]);
        }

        [Test]
        public void IndexAccessor_ArgumentOutOfRangeException()
        {
            var list = new JpxInlineList<int> { 1, 2 };

            int result;
            Assert.Throws<ArgumentOutOfRangeException>(() => result = list[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => result = list[2]);
        }
    }
}
