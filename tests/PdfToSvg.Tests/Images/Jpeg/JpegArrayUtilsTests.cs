// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Images.Jpeg
{
    public class JpegArrayUtilsTests
    {
        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(7)]
        [TestCase(8)] // Vector128 implementation can convert 8 elements at a time
        [TestCase(9)]
        [TestCase(15)]
        [TestCase(16)] // Vector256 implementation can convert 16 elements at a time
        [TestCase(17)]
        [TestCase(31)]
        [TestCase(32)] // Two batches for Vector256
        [TestCase(33)]
        public void Cast_ConvertsShortToFloat(int length)
        {
            var sourceArray = new short[length];
            var destinationArray = new float[length];
            var expectedDestinationArray = new float[length];

            for (var i = 0; i < length; i++)
            {
                sourceArray[i] = (short)i;
                expectedDestinationArray[i] = i;
            }

            JpegArrayUtils.Cast(destinationArray, sourceArray, length);

            Assert.AreEqual(expectedDestinationArray, destinationArray);
        }

        [Test]
        public void Cast_WithPartialLength()
        {
            var sourceArray = new short[] { 10, 20, 30, 40, 50 };
            var destinationArray = new float[5];

            JpegArrayUtils.Cast(destinationArray, sourceArray, 3);

            Assert.AreEqual(destinationArray[0], 10f);
            Assert.AreEqual(destinationArray[1], 20f);
            Assert.AreEqual(destinationArray[2], 30f);
            Assert.AreEqual(destinationArray[3], 0f); // Not written
            Assert.AreEqual(destinationArray[4], 0f); // Not written
        }

        [Test]
        public void Cast_ThrowsWhenDestinationArrayIsNull()
        {
            var sourceArray = new short[] { 1, 2, 3 };

            Assert.Throws<ArgumentNullException>(() => JpegArrayUtils.Cast(null, sourceArray, 3));
        }

        [Test]
        public void Cast_ThrowsWhenSourceArrayIsNull()
        {
            var destinationArray = new float[] { 0f, 0f, 0f };

            Assert.Throws<ArgumentNullException>(() => JpegArrayUtils.Cast(destinationArray, null, 3));
        }

        [Test]
        public void Cast_ThrowsWhenLengthIsNegative()
        {
            var sourceArray = new short[] { 1, 2, 3 };
            var destinationArray = new float[] { 0f, 0f, 0f };

            Assert.Throws<ArgumentOutOfRangeException>(() => JpegArrayUtils.Cast(destinationArray, sourceArray, -1));
        }

        [Test]
        public void Cast_ThrowsWhenLengthExceedsSourceArray()
        {
            var sourceArray = new short[] { 1, 2, 3 };
            var destinationArray = new float[10];

            Assert.Throws<ArgumentOutOfRangeException>(() => JpegArrayUtils.Cast(destinationArray, sourceArray, 4));
        }

        [Test]
        public void Cast_ThrowsWhenLengthExceedsDestinationArray()
        {
            var sourceArray = new short[10];
            var destinationArray = new float[] { 0f, 0f, 0f };

            Assert.Throws<ArgumentOutOfRangeException>(() => JpegArrayUtils.Cast(destinationArray, sourceArray, 4));
        }
    }
}
