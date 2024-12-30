// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jbig2;
using PdfToSvg.Imaging.Jbig2.DecodingProcedures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Images.Jbig2
{
    public class JbigGenericRefinementRegionDecoderTests
    {
        private static JbigBitmap TestBitmap = JbigBitmapStringHelpers.ParseBitmapString(@"
            ◻◻◻◻◻◻◻◻
            ◻◻◻◻◻◻◻◻
            ◻◻◻◻◻◻◻◻
            ◻◻◻◻◻◻◻◻
            ◻◻◻◻◼◼◼◼
            ◻◻◻◻◼◼◼◼
            ◻◻◻◻◼◼◼◼
            ◻◻◻◻◼◼◼◼
        ");

        [TestCase("Inside-1", 5, 5, true)]
        [TestCase("Inside-0", 2, 2, false)]
        [TestCase("Inside-non-same", 3, 3, null)]
        [TestCase("Inside-Corner-1", 6, 6, true)]
        [TestCase("Inside-Corner-0", 1, 1, false)]
        [TestCase("PartiallyOutside-0", 1, 0, false)]
        [TestCase("PartiallyOutside-1", 7, 6, null)]
        [TestCase("PixelOutside-0", -1, -1, false)]
        [TestCase("PixelOutside-1", 8, 8, null)]
        [TestCase("PixelOutside-far", 88, 88, false)]
        public void GetPredictedValue(string name, int x, int y, bool? expectedResult)
        {
            var predictedValue = JbigGenericRefinementRegionDecoder.GetPredictedValue(TestBitmap, x, y);
            Assert.AreEqual(expectedResult, predictedValue);
        }
    }
}
