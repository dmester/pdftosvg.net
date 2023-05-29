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
    internal class JpegBitmapTests
    {
        private JpegBitmap bitmap;
        private const int Size = 80;
        private const int Components = 2;

        public JpegBitmapTests()
        {
            bitmap = new JpegBitmap(Size, Size, Components);

            for (var i = 0; i < bitmap.Data.Length; i++)
            {
                bitmap.Data[i] = (short)i;
            }
        }

        [Test]
        public void GetBlock_Full()
        {
            var block = new short[64];

            bitmap.GetBlock(block,
                x: 1, y: 1,
                componentIndex: 1,
                subSamplingX: 1,
                subSamplingY: 1);

            Assert.AreEqual((1 * Size + 1) * Components + 1, block[0]);
            Assert.AreEqual((1 * Size + 1 + 7) * Components + 1, block[7]);
            Assert.AreEqual((8 * Size + 1 + 7) * Components + 1, block[63]);
        }

        [Test]
        public void GetBlock_Subsampling()
        {
            var block = new short[64];

            bitmap.GetBlock(block,
                x: 0, y: 0,
                componentIndex: 0,
                subSamplingX: 2,
                subSamplingY: 2);

            Assert.AreEqual((0 * Size + 0) * Components, block[0]);
            Assert.AreEqual((0 * Size + 14) * Components, block[7]);
            Assert.AreEqual((2 * Size + 0) * Components, block[8]);
            Assert.AreEqual((2 * Size + 14) * Components, block[15]);
        }

        [Test]
        public void GetBlock_Partial()
        {
            var block = new short[64];

            bitmap.GetBlock(block,
                x: 75, y: 75,
                componentIndex: 0,
                subSamplingX: 1,
                subSamplingY: 1);

            Assert.AreEqual((75 * Size + 75) * Components, block[0]);

            Assert.AreEqual((75 * Size + (Size - 1)) * Components, block[6]); // Repeat last sample
            Assert.AreEqual((75 * Size + (Size - 1)) * Components, block[7]); // Repeat last sample

            // Repeat last line
            Assert.AreEqual((79 * Size + 75) * Components, block[56]);
            Assert.AreEqual((79 * Size + (Size - 1)) * Components, block[63]);
        }

        [Test]
        public void GetBlock_NoOverlap()
        {
            var block = new short[64];

            bitmap.GetBlock(block,
                x: Size, y: Size,
                componentIndex: 0,
                subSamplingX: 1,
                subSamplingY: 1);

            Assert.AreEqual(0, block[0]);
            Assert.AreEqual(0, block[7]);
            Assert.AreEqual(0, block[56]);
            Assert.AreEqual(0, block[63]);
        }
    }
}
