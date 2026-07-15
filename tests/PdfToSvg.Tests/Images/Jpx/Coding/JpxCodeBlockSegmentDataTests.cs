// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.Coding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Images.Jpx.Coding
{
    public class JpxCodeBlockSegmentDataTests
    {
        [Test]
        public void Append()
        {
            var data = new JpxCodeBlockSegmentData();

            data.Append(new ArraySegment<byte>([255, 1, 2, 3, 255], offset: 1, count: 3));
            data.Append(new ArraySegment<byte>([255, 4, 5, 6, 255], offset: 1, count: 3));
            data.Append(new ArraySegment<byte>([255, 7, 8, 9, 255], offset: 1, count: 3));

            Assert.AreEqual(9, data.ByteCount);
            Assert.AreEqual(3, data.SegmentCount);
        }

        [Test]
        public void Indexer_ReturnsOriginalSegment()
        {
            var data = new JpxCodeBlockSegmentData();
            var segment1 = new ArraySegment<byte>([255, 1, 2, 3, 255], offset: 1, count: 3);
            var segment2 = new ArraySegment<byte>([255, 4, 5, 6, 255], offset: 1, count: 3);
            data.Append(segment1);
            data.Append(segment2);

            Assert.AreSame(segment1.Array, data[0].Array);
            Assert.AreSame(segment2.Array, data[1].Array);
        }

        [Test]
        public void Indexer_ArgumentOutOfRange()
        {
            var data = new JpxCodeBlockSegmentData();

            data.Append(new ArraySegment<byte>([255, 1, 2, 3, 255], offset: 1, count: 3));

            ArraySegment<byte> result;
            Assert.Throws<ArgumentOutOfRangeException>(() => result = data[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => result = data[1]);
        }

        [Test]
        public void ToArray()
        {
            var data = new JpxCodeBlockSegmentData();
            data.Append(new ArraySegment<byte>([255, 1, 2, 3, 255], offset: 1, count: 3));
            data.Append(new ArraySegment<byte>([255, 4, 5, 6, 255], offset: 1, count: 3));

            var actual = data.ToArray();

            Assert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6 }, actual);
        }

        [Test]
        public void CopyTo()
        {
            var data = new JpxCodeBlockSegmentData();
            data.Append(new ArraySegment<byte>([255, 1, 2, 3, 255], offset: 1, count: 3));
            data.Append(new ArraySegment<byte>([255, 4, 5, 6, 255], offset: 1, count: 3));
            var actual = new byte[8];
            ArrayUtils.Fill(actual, (byte)255);

            data.CopyTo(actual, offset: 1);

            Assert.AreEqual(new byte[] { 255, 1, 2, 3, 4, 5, 6, 255 }, actual);
        }

        [Test]
        public void ToArraySegment()
        {
            var data = new JpxCodeBlockSegmentData();
            data.Append(new ArraySegment<byte>([255, 1, 2, 3, 255], offset: 1, count: 3));
            data.Append(new ArraySegment<byte>([255, 4, 5, 6, 255], offset: 1, count: 3));
            var actual = new byte[8];
            ArrayUtils.Fill(actual, (byte)255);

            data.CopyTo(actual, offset: 1);

            Assert.AreEqual(new byte[] { 255, 1, 2, 3, 4, 5, 6, 255 }, actual);
        }
    }
}
