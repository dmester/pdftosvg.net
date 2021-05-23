using NUnit.Framework;
using PdfToSvg.DocumentModel;
using PdfToSvg.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Parsing
{
    class InlineImageHelperTests
    {
        [Test]
        public void DetectLengthNoFilter()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes("fjg\0hdk\0djgk \rEI\nOutside image data"));
            Assert.AreEqual(13, InlineImageHelper.DetectStreamLength(stream, null));
        }

        [Test]
        public void DetectLengthEIInsideData()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes("fjg\0hEI\0djgk \rEI\nOutside image data"));
            Assert.AreEqual(13, InlineImageHelper.DetectStreamLength(stream, null));
        }

        [Test]
        public void DetectLengthLastCommand()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes("fjg\0hdk\0djgk \rEI"));
            Assert.AreEqual(13, InlineImageHelper.DetectStreamLength(stream, null));
        }

        [Test]
        public void DetectLengthNoEI()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes("fjg\0hdk\0djgk \r"));
            Assert.AreEqual(14, InlineImageHelper.DetectStreamLength(stream, null));
        }

        [Test]
        public void DetectLengthA85()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes("fjg\0h EI EI EI dk\0djgk \r ~> end of stream"));
            Assert.AreEqual(27, InlineImageHelper.DetectStreamLength(stream, Names.ASCII85Decode));
        }

        [Test]
        public void DetectLengthA85AsFirstFilter()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes("fjg\0h EI EI EI dk\0djgk \r ~> end of stream"));
            Assert.AreEqual(27, InlineImageHelper.DetectStreamLength(stream, new object[] { Names.ASCII85Decode, Names.FlateDecode }));
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
