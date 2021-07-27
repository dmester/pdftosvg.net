// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg;
using PdfToSvg.DocumentModel;
using PdfToSvg.IO;
using PdfToSvg.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Parsing
{
    internal class DocumentParserTests
    {
        [Test]
        public void ReadCrossReferenceTable()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes(
                "xref 0 1 0000000000 65535 f 22 4 0005687164 00000 n 0005687936 00000 n 0005687978 00000 n " +
                "trailer << >>"));
            var parser = new DocumentParser(new InputFile(stream, false), stream);
            var xrefs = parser.ReadXRefTables(0, default);

            var expected = new List<XRef>
            {
                new XRef { ObjectNumber = 0, ByteOffset = 0000000000, Generation = 65535, Type = XRefEntryType.Free },
                new XRef { ObjectNumber = 22, ByteOffset = 0005687164, Generation = 0, Type = XRefEntryType.NotFree },
                new XRef { ObjectNumber = 23, ByteOffset = 0005687936, Generation = 0, Type = XRefEntryType.NotFree },
                new XRef { ObjectNumber = 24, ByteOffset = 0005687978, Generation = 0, Type = XRefEntryType.NotFree },
            };

            Assert.AreEqual(expected, xrefs);
        }

        [Test]
        public void ReadCrossReferenceStream_WithIndex()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes(@"1 0 obj <<
                /Type /XRef
                /W [2 3 4]
                /Index [1 2 10 1]
                /Size 11
                /Filter /ASCIIHexDecode
                /Length 112
            >>
            stream
                0000 000101 01010000
                0001 000201 0101020F
                0002 010203 00000005
            endstream
            endobj")); ;

            var parser = new DocumentParser(new InputFile(stream, false), stream);
            var xrefs = parser.ReadXRefTables(0, default);

            var expected = new List<XRef>
            {
                new XRef { ObjectNumber = 1, Type = XRefEntryType.Free },
                new XRef { ObjectNumber = 2, ByteOffset = 0x000201, Generation = 0x0101020F, Type = XRefEntryType.NotFree },
                new XRef { ObjectNumber = 10, CompressedObjectNumber = 0x010203, CompressedObjectElementIndex = 5, Type = XRefEntryType.Compressed },
            };

            Assert.AreEqual(expected, xrefs);
        }

        [Test]
        public void ReadCrossReferenceStream_WithoutIndex()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes(@"1 0 obj <<
                /Type /XRef
                /W [2 3 4]
                /Size 3
                /Filter /ASCIIHexDecode
                /Length 112
            >>
            stream
                0000 000101 01010000
                0001 000201 0101020F
                0002 010203 00000005
            endstream
            endobj")); ;

            var parser = new DocumentParser(new InputFile(stream, false), stream);
            var xrefs = parser.ReadXRefTables(0, default);

            var expected = new List<XRef>
            {
                new XRef { ObjectNumber = 0, Type = XRefEntryType.Free },
                new XRef { ObjectNumber = 1, ByteOffset = 0x000201, Generation = 0x0101020F, Type = XRefEntryType.NotFree },
                new XRef { ObjectNumber = 2, CompressedObjectNumber = 0x010203, CompressedObjectElementIndex = 5, Type = XRefEntryType.Compressed },
            };

            Assert.AreEqual(expected, xrefs);
        }

        [Test]
        public void ReadCrossReferenceStream_DefaultField3()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes(@"1 0 obj <<
                /Type /XRef
                /W [2 3 0]
                /Size 3
                /Filter /ASCIIHexDecode
                /Length 85
            >>
            stream
                0000 000101
                0001 000201
                0002 010203
            endstream
            endobj")); ;

            var parser = new DocumentParser(new InputFile(stream, false), stream);
            var xrefs = parser.ReadXRefTables(0, default);

            var expected = new List<XRef>
            {
                new XRef { ObjectNumber = 0, Type = XRefEntryType.Free },
                new XRef { ObjectNumber = 1, ByteOffset = 0x000201, Generation = 0, Type = XRefEntryType.NotFree },
                new XRef { ObjectNumber = 2, CompressedObjectNumber = 0x010203, CompressedObjectElementIndex = 0, Type = XRefEntryType.Compressed },
            };

            Assert.AreEqual(expected, xrefs);
        }

        [Test]
        public void ReadCrossReferenceStream_DefaultType()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes(@"1 0 obj <<
                /Type /XRef
                /W [0 3 4]
                /Size 3
                /Filter /ASCIIHexDecode
                /Length 97
            >>
            stream
                000101 01010000
                000201 0101020F
                010203 00000005
            endstream
            endobj")); ;

            var parser = new DocumentParser(new InputFile(stream, false), stream);
            var xrefs = parser.ReadXRefTables(0, default);

            var expected = new List<XRef>
            {
                new XRef { ObjectNumber = 0, ByteOffset = 0x000101, Generation = 0x01010000, Type = XRefEntryType.NotFree },
                new XRef { ObjectNumber = 1, ByteOffset = 0x000201, Generation = 0x0101020F, Type = XRefEntryType.NotFree },
                new XRef { ObjectNumber = 2, ByteOffset = 0x010203, Generation = 0x00000005, Type = XRefEntryType.NotFree },
            };

            Assert.AreEqual(expected, xrefs);
        }

    }
}
