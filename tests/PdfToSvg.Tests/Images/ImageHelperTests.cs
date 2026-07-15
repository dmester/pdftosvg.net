// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.ColorSpaces;
using PdfToSvg.DocumentModel;
using PdfToSvg.Imaging;

namespace PdfToSvg.Tests.Images
{
    internal class ImageHelperTests
    {
        private static readonly DeviceRgbColorSpace RgbColorSpace = new DeviceRgbColorSpace();

        private static PdfDictionary CreateImageDictionary(params object[] decodeValues)
        {
            return new PdfDictionary
            {
                { Names.BitsPerComponent, 8 },
                { Names.Decode, decodeValues },
            };
        }

        private static void AssertRange(DecodeArray decodeArray, int componentIndex, float dmin, float dmax)
        {
            Assert.AreEqual(dmin, decodeArray[componentIndex].Dmin, "Dmin for component " + componentIndex);
            Assert.AreEqual(dmax, decodeArray[componentIndex].Dmax, "Dmax for component " + componentIndex);
        }

        [Test]
        public void GetDecodeArray_MissingUsesDefault()
        {
            var imageDictionary = new PdfDictionary { { Names.BitsPerComponent, 8 } };

            var decodeArray = ImageHelper.GetDecodeArray(imageDictionary, RgbColorSpace);

            Assert.AreEqual(3, decodeArray.Count);
            AssertRange(decodeArray, 0, 0f, 1f);
            AssertRange(decodeArray, 1, 0f, 1f);
            AssertRange(decodeArray, 2, 0f, 1f);
        }

        [Test]
        public void GetDecodeArray_EmptyUsesDefault()
        {
            var imageDictionary = CreateImageDictionary();

            var decodeArray = ImageHelper.GetDecodeArray(imageDictionary, RgbColorSpace);

            Assert.AreEqual(3, decodeArray.Count);
            AssertRange(decodeArray, 0, 0f, 1f);
            AssertRange(decodeArray, 1, 0f, 1f);
            AssertRange(decodeArray, 2, 0f, 1f);
        }

        [Test]
        public void GetDecodeArray_ExactLengthUsesProvidedRanges()
        {
            var imageDictionary = CreateImageDictionary(.2, .8, .1, .9, 1, 0);

            var decodeArray = ImageHelper.GetDecodeArray(imageDictionary, RgbColorSpace);

            Assert.AreEqual(3, decodeArray.Count);
            AssertRange(decodeArray, 0, .2f, .8f);
            AssertRange(decodeArray, 1, .1f, .9f);
            AssertRange(decodeArray, 2, 1f, 0f);
        }

        [Test]
        public void GetDecodeArray_ShortUsesDefaultsForMissingRanges()
        {
            var imageDictionary = CreateImageDictionary(1, 0);

            var decodeArray = ImageHelper.GetDecodeArray(imageDictionary, RgbColorSpace);

            Assert.AreEqual(3, decodeArray.Count);
            AssertRange(decodeArray, 0, 1f, 0f);
            AssertRange(decodeArray, 1, 0f, 1f);
            AssertRange(decodeArray, 2, 0f, 1f);
        }

        [Test]
        public void GetDecodeArray_PartialRangeUsesDefault()
        {
            var imageDictionary = CreateImageDictionary(1, 0, .5);

            var decodeArray = ImageHelper.GetDecodeArray(imageDictionary, RgbColorSpace);

            Assert.AreEqual(3, decodeArray.Count);
            AssertRange(decodeArray, 0, 1f, 0f);
            AssertRange(decodeArray, 1, 0f, 1f);
            AssertRange(decodeArray, 2, 0f, 1f);
        }

        [Test]
        public void GetDecodeArray_LongTruncatesExcessRanges()
        {
            var imageDictionary = CreateImageDictionary(.2, .8, .1, .9, 1, 0, 10, 20);

            var decodeArray = ImageHelper.GetDecodeArray(imageDictionary, RgbColorSpace);

            Assert.AreEqual(3, decodeArray.Count);
            AssertRange(decodeArray, 0, .2f, .8f);
            AssertRange(decodeArray, 1, .1f, .9f);
            AssertRange(decodeArray, 2, 1f, 0f);
        }

        [Test]
        public void GetDecodeArray_InvalidArrayUsesDefault()
        {
            var imageDictionary = new PdfDictionary
            {
                { Names.BitsPerComponent, 8 },
                { Names.Decode, new object[] { 0, "invalid" } },
            };

            var decodeArray = ImageHelper.GetDecodeArray(imageDictionary, RgbColorSpace);

            Assert.AreEqual(3, decodeArray.Count);
            AssertRange(decodeArray, 0, 0f, 1f);
            AssertRange(decodeArray, 1, 0f, 1f);
            AssertRange(decodeArray, 2, 0f, 1f);
        }

        [Test]
        public void GetDecodeArray_ShouldBeIgnoredForJpxIfColorSpaceIsMissing()
        {
            var imageDictionary = new PdfDictionary
            {
                { Names.BitsPerComponent, 8 },
                { Names.Filter, Names.JPXDecode },
                { Names.Decode, new object[] { 0, 1, 1, 0, 0, 1 } },
            };
            imageDictionary.MakeIndirectObject(default, new PdfMemoryStream(imageDictionary, [], 0));

            var decodeArray = ImageHelper.GetDecodeArray(imageDictionary, RgbColorSpace);

            Assert.AreEqual(3, decodeArray.Count);
            AssertRange(decodeArray, 0, 0f, 1f);
            AssertRange(decodeArray, 1, 0f, 1f);
            AssertRange(decodeArray, 2, 0f, 1f);
        }

        [Test]
        public void HasCustomDecodeArray_MissingReturnsFalse()
        {
            var imageDictionary = new PdfDictionary { { Names.BitsPerComponent, 8 } };

            Assert.IsFalse(ImageHelper.HasCustomDecodeArray(imageDictionary, RgbColorSpace));
        }

        [Test]
        public void HasCustomDecodeArray_DefaultReturnsFalse()
        {
            var imageDictionary = CreateImageDictionary(0, 1, 0, 1, 0, 1);

            Assert.IsFalse(ImageHelper.HasCustomDecodeArray(imageDictionary, RgbColorSpace));
        }

        [Test]
        public void HasCustomDecodeArray_CustomReturnsTrue()
        {
            var imageDictionary = CreateImageDictionary(1, 0, 0, 1, 0, 1);

            Assert.IsTrue(ImageHelper.HasCustomDecodeArray(imageDictionary, RgbColorSpace));
        }

        [Test]
        public void HasCustomDecodeArray_NormalizedDefaultReturnsFalse()
        {
            var imageDictionary = CreateImageDictionary(0, 1, 0, 1, 0, 1, 10, 20);

            Assert.IsFalse(ImageHelper.HasCustomDecodeArray(imageDictionary, RgbColorSpace));
        }

        [Test]
        public void HasCustomDecodeArray_ShortDefaultReturnsFalse()
        {
            var imageDictionary = CreateImageDictionary(0, 1);

            Assert.IsFalse(ImageHelper.HasCustomDecodeArray(imageDictionary, RgbColorSpace));
        }

        [Test]
        public void HasCustomDecodeArray_InvalidArrayReturnsFalse()
        {
            var imageDictionary = new PdfDictionary
            {
                { Names.BitsPerComponent, 8 },
                { Names.Decode, new object[] { 0, "invalid" } },
            };

            Assert.IsFalse(ImageHelper.HasCustomDecodeArray(imageDictionary, RgbColorSpace));
        }
    }
}
