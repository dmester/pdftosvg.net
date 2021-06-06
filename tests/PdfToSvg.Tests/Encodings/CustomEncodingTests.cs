// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Encodings
{
    class CustomEncodingTests
    {
        [Test]
        public void DecodeText()
        {
            var decoder = CustomEncoding.Create(new PdfDictionary
            {
                { Names.BaseEncoding, Names.WinAnsiEncoding },
                { Names.Differences, new object[] 
                {
                    39, new PdfName("quotesingle"),
                    128, new PdfName("Aring"), new PdfName("Ntilde"),
                } },
            });

            var str = new PdfString(new byte[] { 39, 128, 129, 198 });
            Assert.AreEqual(new CharacterCode(39, 1, "'"), decoder.GetCharacter(str, 0));
            Assert.AreEqual(new CharacterCode(128, 1, "Å"), decoder.GetCharacter(str, 1));
            Assert.AreEqual(new CharacterCode(129, 1, "Ñ"), decoder.GetCharacter(str, 2));
            Assert.AreEqual(new CharacterCode(198, 1, "Æ"), decoder.GetCharacter(str, 3));
        }
    }
}
