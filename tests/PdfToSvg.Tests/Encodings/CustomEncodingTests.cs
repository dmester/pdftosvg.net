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
    public class CustomEncodingTests
    {
        [Test]
        public void DecodeText()
        {
            var decoder = CustomEncoding.Create(new PdfDictionary
            {
                { Names.BaseEncoding, Names.WinAnsiEncoding },
                { Names.Differences, new object[]
                {
                    39, new PdfName("Ebreve"),
                    128, new PdfName("Ncaron"), new PdfName("Wcircumflex"),
                } },
            });

            var str = decoder.GetString(new byte[] { 39, 128, 129, 198 });
            var bytes = decoder.GetBytes("ĔŇŴÆ");

            Assert.AreEqual("ĔŇŴÆ", str);
            Assert.AreEqual(new byte[] { 39, 128, 129, 198 }, bytes);
        }
    }
}
