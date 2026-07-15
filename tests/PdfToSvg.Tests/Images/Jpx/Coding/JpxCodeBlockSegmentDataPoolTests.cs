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
    public class JpxCodeBlockSegmentDataPoolTests
    {
        [Test]
        public void ToArraySegment_0()
        {
            var pool = new JpxCodeBlockSegmentDataPool();
            var data = new JpxCodeBlockSegmentData();

            var actual = pool.ToArraySegment(ref data);

            Assert.AreSame(ArrayUtils.EmptySegment<byte>().Array, actual.Array);
        }

        [Test]
        public void ToArraySegment_1()
        {
            var pool = new JpxCodeBlockSegmentDataPool();

            var data = new JpxCodeBlockSegmentData();
            var segment1 = new ArraySegment<byte>([255, 1, 2, 3, 255], offset: 1, count: 3);
            data.Append(segment1);

            var actual = pool.ToArraySegment(ref data);

            Assert.AreSame(segment1.Array, actual.Array);
            Assert.AreEqual(3, actual.Count);
            Assert.AreEqual(1, actual.Offset);
        }

        [Test]
        public void ToArraySegment_2_EmptyPool()
        {
            var pool = new JpxCodeBlockSegmentDataPool();

            var data = new JpxCodeBlockSegmentData();
            var segment1 = new ArraySegment<byte>([255, 1, 2, 3, 255], offset: 1, count: 3);
            var segment2 = new ArraySegment<byte>([255, 4, 5, 6, 255], offset: 1, count: 3);

            data.Append(segment1);
            data.Append(segment2);

            var actual = pool.ToArraySegment(ref data);

            Assert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6 }, actual.ToArray());
        }

        [Test]
        public void ToArraySegment_2_TooSmallPool()
        {
            var pool = new JpxCodeBlockSegmentDataPool();

            var segment1 = new ArraySegment<byte>([255, 1, 2, 3, 255], offset: 1, count: 3);
            var segment2 = new ArraySegment<byte>([255, 4, 5, 6, 255], offset: 1, count: 3);

            var data = new JpxCodeBlockSegmentData();

            data.Append(segment1);
            var actual1 = pool.ToArraySegment(ref data);

            // Will need to grow the pooled array
            data.Append(segment2);
            var actual2 = pool.ToArraySegment(ref data);

            Assert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6 }, actual2.ToArray());
        }

        [Test]
        public void ToArraySegment_2_ReusablePool()
        {
            var pool = new JpxCodeBlockSegmentDataPool();

            var data1 = new JpxCodeBlockSegmentData();
            var segment1a = new ArraySegment<byte>([255, 1, 2, 3, 255], offset: 1, count: 3);
            var segment1b = new ArraySegment<byte>([255, 4, 5, 6, 255], offset: 1, count: 3);
            var segment2 = new ArraySegment<byte>([255, 7, 8, 9, 255], offset: 1, count: 3);

            data1.Append(segment1a);
            data1.Append(segment1b);
            var data2 = data1;
            data2.Append(segment2);

            var actual2 = pool.ToArraySegment(ref data2);
            var actual1 = pool.ToArraySegment(ref data1);

            Assert.AreSame(actual1.Array, actual2.Array);
            Assert.AreEqual(6, actual1.Count);
            Assert.AreEqual(9, actual2.Count);
        }
    }
}
