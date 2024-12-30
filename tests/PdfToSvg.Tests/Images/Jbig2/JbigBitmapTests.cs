// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jbig2;
using PdfToSvg.Imaging.Jbig2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Images.Jbig2
{
    public class JbigBitmapTests
    {
        private static JbigBitmap TestBitmap = JbigBitmapStringHelpers.ParseBitmapString(@"
            ◻◼◼◼◼◻
            ◻◻◻◻◻◼
            ◻◼◼◼◼◼
            ◼◻◻◻◻◼
            ◼◻◻◻◻◼
            ◻◼◼◼◼◼
        ");

        private static JbigBitmap TestBitmap2 = JbigBitmapStringHelpers.ParseBitmapString(@"
            ◻◻◼◼◻
            ◻◼◼◻◻
            ◻◼◼◻◻
            ◻◻◼◼◻
        ");

        [Test]
        public void Draw_Xor()
        {
            var result = TestBitmap2.Crop(0, 0, 5, 4);

            result.Draw(TestBitmap2, 1, 1, JbigCombinationOperator.Xor);

            var expected = JbigBitmapStringHelpers.NormalizeBitmapString(@"
                ◻◻◼◼◻
                ◻◼◼◼◼
                ◻◼◻◼◻
                ◻◻◻◻◻
            ");

            Assert.AreEqual(expected, result.DebugView);
        }

        [Test]
        public void Draw_Or_BottomRight()
        {
            var result = TestBitmap2.Crop(0, 0, 5, 4);

            result.Draw(TestBitmap2, 1, 1, JbigCombinationOperator.Or);

            var expected = JbigBitmapStringHelpers.NormalizeBitmapString(@"
                ◻◻◼◼◻
                ◻◼◼◼◼
                ◻◼◼◼◻
                ◻◻◼◼◻
            ");

            Assert.AreEqual(expected, result.DebugView);
        }

        [Test]
        public void Draw_Or_TopLeft()
        {
            var result = TestBitmap2.Crop(0, 0, 5, 4);

            result.Draw(TestBitmap2, -1, -1, JbigCombinationOperator.Or);

            var expected = JbigBitmapStringHelpers.NormalizeBitmapString(@"
                ◼◼◼◼◻
                ◼◼◼◻◻
                ◻◼◼◻◻
                ◻◻◼◼◻
            ");

            Assert.AreEqual(expected, result.DebugView);
        }

        [Test]
        public void Draw_Or_Outside()
        {
            var result = TestBitmap2.Crop(0, 0, 5, 4);

            result.Draw(TestBitmap2, -10, -1, JbigCombinationOperator.Or);

            var expected = JbigBitmapStringHelpers.NormalizeBitmapString(@"
                ◻◻◼◼◻
                ◻◼◼◻◻
                ◻◼◼◻◻
                ◻◻◼◼◻
            ");

            Assert.AreEqual(expected, result.DebugView);
        }

        [Test]
        public void Draw_And()
        {
            var result = TestBitmap2.Crop(0, 0, 5, 4);

            result.Draw(TestBitmap2, 1, 1, JbigCombinationOperator.And);

            var expected = JbigBitmapStringHelpers.NormalizeBitmapString(@"
                ◻◻◼◼◻
                ◻◻◻◻◻
                ◻◻◼◻◻
                ◻◻◼◼◻
            ");

            Assert.AreEqual(expected, result.DebugView);
        }

        [Test]
        public void Draw_Xnor()
        {
            var result = TestBitmap2.Crop(0, 0, 5, 4);

            result.Draw(TestBitmap2, 1, 1, JbigCombinationOperator.Xnor);

            var expected = JbigBitmapStringHelpers.NormalizeBitmapString(@"
                ◻◻◼◼◻
                ◻◻◻◻◻
                ◻◻◼◻◼
                ◻◼◼◼◼
            ");

            Assert.AreEqual(expected, result.DebugView);
        }

        [Test]
        public void Draw_Replace_BottomRight()
        {
            var result = TestBitmap2.Crop(0, 0, 5, 4);

            result.Draw(TestBitmap2, 1, 1, JbigCombinationOperator.Replace);

            var expected = JbigBitmapStringHelpers.NormalizeBitmapString(@"
                ◻◻◼◼◻
                ◻◻◻◼◼
                ◻◻◼◼◻
                ◻◻◼◼◻
            ");

            Assert.AreEqual(expected, result.DebugView);
        }

        [Test]
        public void Draw_Replace_TopLeft()
        {
            var result = TestBitmap2.Crop(0, 0, 5, 4);

            result.Draw(TestBitmap2, -1, -1, JbigCombinationOperator.Replace);

            var expected = JbigBitmapStringHelpers.NormalizeBitmapString(@"
                ◼◼◻◻◻
                ◼◼◻◻◻
                ◻◼◼◻◻
                ◻◻◼◼◻
            ");

            Assert.AreEqual(expected, result.DebugView);
        }

        [Test]
        public void Draw_Replace_Outside()
        {
            var result = TestBitmap2.Crop(0, 0, 5, 4);

            result.Draw(TestBitmap2, -10, -10, JbigCombinationOperator.Replace);

            var expected = JbigBitmapStringHelpers.NormalizeBitmapString(@"
                ◻◻◼◼◻
                ◻◼◼◻◻
                ◻◼◼◻◻
                ◻◻◼◼◻
            ");

            Assert.AreEqual(expected, result.DebugView);
        }

        [Test]
        public void Crop_SameDimensions()
        {
            var result = TestBitmap.Crop(0, 0, 6, 6);

            Assert.AreEqual(TestBitmap.DebugView, result.DebugView);
        }

        [Test]
        public void Crop_Containing()
        {
            var result = TestBitmap.Crop(1, 1, 4, 3);

            var expected = JbigBitmapStringHelpers.NormalizeBitmapString(@"
                ◻◻◻◻
                ◼◼◼◼
                ◻◻◻◻
            ");

            Assert.AreEqual(expected, result.DebugView);
        }

        [Test]
        public void Crop_Overlapping()
        {
            var result = TestBitmap.Crop(-1, -1, 9, 8);

            var expected = JbigBitmapStringHelpers.NormalizeBitmapString(@"
                ◻◻◻◻◻◻◻◻◻
                ◻◻◼◼◼◼◻◻◻
                ◻◻◻◻◻◻◼◻◻
                ◻◻◼◼◼◼◼◻◻
                ◻◼◻◻◻◻◼◻◻
                ◻◼◻◻◻◻◼◻◻
                ◻◻◼◼◼◼◼◻◻
                ◻◻◻◻◻◻◻◻◻
            ");

            Assert.AreEqual(expected, result.DebugView);
        }

        [Test]
        public void Crop_TopLeft()
        {
            var result = TestBitmap.Crop(-1, -1, 3, 4);

            var expected = JbigBitmapStringHelpers.NormalizeBitmapString(@"
                ◻◻◻
                ◻◻◼
                ◻◻◻
                ◻◻◼
            ");

            Assert.AreEqual(expected, result.DebugView);
        }

        [Test]
        public void Crop_BottomRight()
        {
            var result = TestBitmap.Crop(3, 3, 4, 3);

            var expected = JbigBitmapStringHelpers.NormalizeBitmapString(@"
                ◻◻◼◻
                ◻◻◼◻
                ◼◼◼◻
            ");

            Assert.AreEqual(expected, result.DebugView);
        }

        [Test]
        public void Crop_Outside_TopLeft()
        {
            var result = TestBitmap.Crop(-5, -5, 4, 4);

            var expected = JbigBitmapStringHelpers.NormalizeBitmapString(@"
                ◻◻◻◻
                ◻◻◻◻
                ◻◻◻◻
                ◻◻◻◻
            ");

            Assert.AreEqual(expected, result.DebugView);
        }

        [Test]
        public void Crop_Outside_BottomRight()
        {
            var result = TestBitmap.Crop(6, 6, 4, 4);

            var expected = JbigBitmapStringHelpers.NormalizeBitmapString(@"
                ◻◻◻◻
                ◻◻◻◻
                ◻◻◻◻
                ◻◻◻◻
            ");

            Assert.AreEqual(expected, result.DebugView);
        }

        [Test]
        public void Crop_Corner()
        {
            var result = TestBitmap.Crop(0, 0, 0, 0);
            Assert.AreEqual("<empty>", result.DebugView);
        }

        [Test]
        public void Crop_EmptySource_ZeroSize()
        {
            var result = JbigBitmap.Empty.Crop(0, 0, 0, 0);
            Assert.AreEqual("<empty>", result.DebugView);
        }

        [Test]
        public void Crop_EmptySource_NonZeroSize()
        {
            var result = JbigBitmap.Empty.Crop(0, 0, 4, 4);

            var expected = JbigBitmapStringHelpers.NormalizeBitmapString(@"
                ◻◻◻◻
                ◻◻◻◻
                ◻◻◻◻
                ◻◻◻◻
            ");

            Assert.AreEqual(expected, result.DebugView);
        }
    }
}
