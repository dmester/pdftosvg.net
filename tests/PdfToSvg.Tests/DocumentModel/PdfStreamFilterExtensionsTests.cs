// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.DocumentModel;
using PdfToSvg.Imaging;
using System.IO;
using System.Linq;

namespace PdfToSvg.Tests.DocumentModel
{
    internal class PdfStreamFilterExtensionsTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void Decode_ClosesRejectedStream(bool skipLast)
        {
            using var stream = new MemoryStream();
            var filters = new PdfStreamFilter[11];

            Assert.Throws<PdfException>(() => filters.Decode(stream, skipLast));
            Assert.IsFalse(stream.CanRead);
        }

        [Test]
        public void GetContent_RejectsElevenFilters()
        {
            var owner = new PdfDictionary
            {
                { Names.Filter, Enumerable.Repeat<object>(Names.FlateDecode, 11).ToArray() },
            };

            var stream = new PdfMemoryStream(owner, new byte[0], 0);
            Assert.Throws<PdfException>(() => ImageHelper.GetContent(stream, default));
        }
    }
}
