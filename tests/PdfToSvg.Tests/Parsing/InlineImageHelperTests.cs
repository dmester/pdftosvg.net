// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing;
using PdfToSvg.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Parsing
{
    public class InlineImageHelperTests
    {
        [Test]
        [TestCase(-1, 2 * 2 * 1 * 3)]
        [TestCase(5, 5)]
        [TestCase(100, 2 * 2 * 1 * 3)]
        public void DetectLength_FixedLength(int declaredLength, int expectedResult)
        {
            var stream = new MemoryStream(
                Encoding.ASCII.GetBytes(" EI\n                           Outside image data"));

            var dict = new PdfDictionary
            {
                { Names.Width, 2 },
                { Names.Height, 2 },
                { Names.BitsPerComponent, 8 },
                { Names.ColorSpace, Names.DeviceRGB },
                { Names.Length, declaredLength },
            };

            Assert.AreEqual(expectedResult, InlineImageHelper.DetectStreamLength(stream, dict));
        }

        [Test]
        public void DetectLength_Calculated_NamedColorSpace()
        {
            var stream = new MemoryStream(
                Encoding.ASCII.GetBytes(" EI\n                           Outside image data"));

            var resources = new ResourceCache(new PdfDictionary
            {
                { Names.ColorSpace, new PdfDictionary {
                    { AbbreviatedNames.L, Names.DeviceCMYK }
                } }
            });

            var dict = new PdfDictionary
            {
                { Names.Width, 2 },
                { Names.Height, 2 },
                { Names.BitsPerComponent, 8 },
                { Names.ColorSpace, AbbreviatedNames.L },
            };

            Assert.AreEqual(2 * 2 * 1 * 4, InlineImageHelper.DetectStreamLength(stream, dict, resources));
        }

        [Test]
        public void DetectLength_Calculated_DeviceGray()
        {
            var stream = new MemoryStream(
                Encoding.ASCII.GetBytes(" EI\n                           Outside image data"));

            var dict = new PdfDictionary
            {
                { Names.Width, 2 },
                { Names.Height, 2 },
                { Names.BitsPerComponent, 8 },
                { Names.ColorSpace, Names.DeviceGray },
            };

            Assert.AreEqual(2 * 2 * 1 * 1, InlineImageHelper.DetectStreamLength(stream, dict));
        }

        [Test]
        public void DetectLength_Calculated_DeviceRgb()
        {
            var stream = new MemoryStream(
                Encoding.ASCII.GetBytes(" EI\n                           Outside image data"));

            var dict = new PdfDictionary
            {
                { Names.Width, 2 },
                { Names.Height, 2 },
                { Names.BitsPerComponent, 8 },
                { Names.ColorSpace, Names.DeviceRGB },
            };

            Assert.AreEqual(2 * 2 * 1 * 3, InlineImageHelper.DetectStreamLength(stream, dict));
        }

        [Test]
        public void DetectLength_Calculated_DeviceCmyk()
        {
            var stream = new MemoryStream(
                Encoding.ASCII.GetBytes(" EI\n                           Outside image data"));

            var dict = new PdfDictionary
            {
                { Names.Width, 2 },
                { Names.Height, 2 },
                { Names.BitsPerComponent, 8 },
                { Names.ColorSpace, Names.DeviceCMYK },
            };

            Assert.AreEqual(2 * 2 * 1 * 4, InlineImageHelper.DetectStreamLength(stream, dict));
        }

        [Test]
        public void DetectLength_Calculated_TooLargeDimension()
        {
            var stream = new MemoryStream(
                Encoding.ASCII.GetBytes("fjg\0hdk\0djgk \rEI\nOutside image data"));

            var dict = new PdfDictionary
            {
                { Names.Width, 2 },
                { Names.Height, 32769 },
                { Names.BitsPerComponent, 8 },
                { Names.ColorSpace, Names.DeviceCMYK },
            };

            Assert.AreEqual(13, InlineImageHelper.DetectStreamLength(stream, dict));
        }

        [Test]
        public void DetectLength_Calculated_MaxValues()
        {
            var stream = new MemoryStream(
                Encoding.ASCII.GetBytes("fjg\0hdk\0djgk \rEI\nOutside image data"));

            var dict = new PdfDictionary
            {
                { Names.Width, 32768 },
                { Names.Height, 32768 },
                { Names.BitsPerComponent, 16 },
                { Names.ColorSpace, Names.DeviceCMYK },
            };

            Assert.AreEqual(13, InlineImageHelper.DetectStreamLength(stream, dict));
        }

        [Test]
        public void DetectLength_Calculated_Bpc1()
        {
            var stream = new MemoryStream(
                Encoding.ASCII.GetBytes("fjg\0hdk\0djgk \rEI\nOutside image data"));

            var dict = new PdfDictionary
            {
                { Names.Width, 2 },
                { Names.Height, 2 },
                { Names.BitsPerComponent, 1 },
                { Names.ColorSpace, Names.DeviceRGB },
            };

            Assert.AreEqual(2, InlineImageHelper.DetectStreamLength(stream, dict));
        }

        [Test]
        public void DetectLengthNoFilter()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes("fjg\0hdk\0djgk \rEI\nOutside image data"));
            Assert.AreEqual(13, InlineImageHelper.DetectStreamLength(stream, new PdfDictionary()));
        }

        [Test]
        public void DetectLengthEIInsideData()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes("fjg\0hEI\0djgk \rEI\nOutside image data"));
            Assert.AreEqual(13, InlineImageHelper.DetectStreamLength(stream, new PdfDictionary()));
        }

        [Test]
        public void DetectLengthLastCommand()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes("fjg\0hdk\0djgk \rEI"));
            Assert.AreEqual(13, InlineImageHelper.DetectStreamLength(stream, new PdfDictionary()));
        }

        [Test]
        public void DetectLengthNoEI()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes("fjg\0hdk\0djgk \r"));
            Assert.AreEqual(14, InlineImageHelper.DetectStreamLength(stream, new PdfDictionary()));
        }

        [Test]
        public void DetectLengthA85()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes("fjg\0h EI EI EI dk\0djgk \r ~> end of stream"));
            var dict = new PdfDictionary { { Names.Filter, Names.ASCII85Decode } };
            Assert.AreEqual(27, InlineImageHelper.DetectStreamLength(stream, dict));
        }

        [Test]
        public void DetectLengthA85AsFirstFilter()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes("fjg\0h EI EI EI dk\0djgk \r ~> end of stream"));
            var dict = new PdfDictionary { { Names.Filter, new object[] { Names.ASCII85Decode, Names.FlateDecode } } };
            Assert.AreEqual(27, InlineImageHelper.DetectStreamLength(stream, dict));
        }

        [Test]
        public void DeabbreviateDictionary()
        {
            var dict = new PdfDictionary
            {
                { new PdfName("BPC"), 2 },
                { new PdfName("BitsPerComponent"), 4 }, // Prefer this one
                { new PdfName("I"), true },
                { new PdfName("F"), new object[] { new PdfName("AHx"), new PdfName("A85"), new PdfName("Fl") } },
                { new PdfName("CS"), new object[] { new PdfName("I") } },
            };

            InlineImageHelper.DeabbreviateInlineImageDictionary(dict);

            Assert.AreEqual(4, dict[Names.BitsPerComponent]);
            Assert.AreEqual(true, dict[Names.Interpolate]);
            Assert.AreEqual(new object[] { Names.ASCIIHexDecode, Names.ASCII85Decode, Names.FlateDecode }, dict[Names.Filter]);
            Assert.AreEqual(new object[] { Names.Indexed }, dict[Names.ColorSpace]);
        }
    }
}
