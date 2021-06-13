// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.DocumentModel;
using PdfToSvg.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.DocumentModel
{
    class PdfStreamTests
    {
        [Test]
        public void NoFilter()
        {
            var streamDictionary = new PdfDictionary();

            var stream = new PdfMemoryStream(streamDictionary, new byte[0], 0);

            Assert.AreEqual(0, stream.Filters.Count);
        }

        [Test]
        public void UnsupportedFilters()
        {
            var streamDictionary = new PdfDictionary
            {
                { Names.Filter, new object[] { Names.ASCII85Decode, null, 42, new PdfName("NotARealFilter"), Names.FlateDecode } },
            };

            var stream = new PdfMemoryStream(streamDictionary, new byte[0], 0);

            Assert.AreEqual(2, stream.Filters.Count);
            Assert.AreEqual(Filter.Ascii85Decode, stream.Filters[0].Filter);
            Assert.AreEqual(Filter.FlateDecode, stream.Filters[1].Filter);
        }


        [Test]
        public void MultiFilter_Array_NoDecodeParms()
        {
            var streamDictionary = new PdfDictionary
            {
                { Names.Filter, new object[] { Names.ASCII85Decode, Names.ASCIIHexDecode, Names.FlateDecode } },
            };

            var stream = new PdfMemoryStream(streamDictionary, new byte[0], 0);

            Assert.AreEqual(3, stream.Filters.Count);
            Assert.AreEqual(Filter.Ascii85Decode, stream.Filters[0].Filter);
            Assert.AreEqual(Filter.AsciiHexDecode, stream.Filters[1].Filter);
            Assert.AreEqual(Filter.FlateDecode, stream.Filters[2].Filter);
            Assert.AreEqual(null, stream.Filters[0].DecodeParms);
            Assert.AreEqual(null, stream.Filters[1].DecodeParms);
            Assert.AreEqual(null, stream.Filters[2].DecodeParms);
        }

        [Test]
        public void MultiFilter_Array_WithDecodeParms()
        {
            var streamDictionary = new PdfDictionary
            {
                { Names.Filter, new object[] { Names.ASCII85Decode, Names.ASCIIHexDecode, Names.FlateDecode } },
                { Names.DecodeParms, new object[]
                {
                    null,
                    new PdfDictionary { { Names.BitsPerComponent, 42 } },
                } },
            };

            var stream = new PdfMemoryStream(streamDictionary, new byte[0], 0);

            Assert.AreEqual(3, stream.Filters.Count);
            Assert.AreEqual(Filter.Ascii85Decode, stream.Filters[0].Filter);
            Assert.AreEqual(Filter.AsciiHexDecode, stream.Filters[1].Filter);
            Assert.AreEqual(Filter.FlateDecode, stream.Filters[2].Filter);
            Assert.AreEqual(null, stream.Filters[0].DecodeParms);
            Assert.AreEqual(42, stream.Filters[1].DecodeParms[Names.BitsPerComponent]);
            Assert.AreEqual(null, stream.Filters[2].DecodeParms);
        }

        [Test]
        public void SingleFilter_Array_NoDecodeParms()
        {
            var streamDictionary = new PdfDictionary
            {
                { Names.Filter, new object[] { Names.FlateDecode } },
            };

            var stream = new PdfMemoryStream(streamDictionary, new byte[0], 0);

            Assert.AreEqual(Filter.FlateDecode, stream.Filters[0].Filter);
            Assert.AreEqual(null, stream.Filters[0].DecodeParms);
        }

        [Test]
        public void SingleFilter_Array_WithDecodeParms()
        {
            var streamDictionary = new PdfDictionary
            {
                { Names.Filter, new object[] { Names.FlateDecode } },
                { Names.DecodeParms, new PdfDictionary { { Names.BitsPerComponent, 8 } } },
            };

            var stream = new PdfMemoryStream(streamDictionary, new byte[0], 0);

            Assert.AreEqual(Filter.FlateDecode, stream.Filters[0].Filter);
            Assert.AreEqual(8, stream.Filters[0].DecodeParms[Names.BitsPerComponent]);
        }

        [Test]
        public void SingleFilter_Array_WithDecodeParmsArray()
        {
            var streamDictionary = new PdfDictionary
            {
                { Names.Filter, new object[] { Names.FlateDecode } },
                { Names.DecodeParms, new object[] { new PdfDictionary { { Names.BitsPerComponent, 8 } } } },
            };

            var stream = new PdfMemoryStream(streamDictionary, new byte[0], 0);

            Assert.AreEqual(Filter.FlateDecode, stream.Filters[0].Filter);
            Assert.AreEqual(8, stream.Filters[0].DecodeParms[Names.BitsPerComponent]);
        }

        [Test]
        public void SingleFilter_NoDecodeParms()
        {
            var streamDictionary = new PdfDictionary
            {
                { Names.Filter, Names.FlateDecode },
            };

            var stream = new PdfMemoryStream(streamDictionary, new byte[0], 0);

            Assert.AreEqual(Filter.FlateDecode, stream.Filters[0].Filter);
            Assert.AreEqual(null, stream.Filters[0].DecodeParms);
        }

        [Test]
        public void SingleFilter_WithDecodeParms()
        {
            var streamDictionary = new PdfDictionary
            {
                { Names.Filter, Names.FlateDecode },
                { Names.DecodeParms, new PdfDictionary { { Names.BitsPerComponent, 8 } } },
            };

            var stream = new PdfMemoryStream(streamDictionary, new byte[0], 0);

            Assert.AreEqual(Filter.FlateDecode, stream.Filters[0].Filter);
            Assert.AreEqual(8, stream.Filters[0].DecodeParms[Names.BitsPerComponent]);
        }

        [Test]
        public void SingleFilter_WithDecodeParmsArray()
        {
            var streamDictionary = new PdfDictionary
            {
                { Names.Filter, Names.FlateDecode },
                { Names.DecodeParms, new object[] { new PdfDictionary { { Names.BitsPerComponent, 8 } } } },
            };

            var stream = new PdfMemoryStream(streamDictionary, new byte[0], 0);

            Assert.AreEqual(Filter.FlateDecode, stream.Filters[0].Filter);
            Assert.AreEqual(8, stream.Filters[0].DecodeParms[Names.BitsPerComponent]);
        }

    }
}
