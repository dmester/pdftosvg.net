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
    class DocumentParserTests
    {
        [Test]
        public void ReadCrossReferenceTable()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes(
                "xref 0 1 0000000000 65535 f 22 4 0005687164 00000 n 0005687936 00000 n 0005687978 00000 n"));
            var parser = new DocumentParser(new InputFile(stream), stream);
            var xrefs = new XRefTable();
            parser.ReadXRefTable(xrefs);

            var expected = new List<XRef>
            {
                new XRef { ObjectNumber = 0, ByteOffset = 0000000000, Generation = 65535, Type = XRefEntryType.Free },
                new XRef { ObjectNumber = 22, ByteOffset = 0005687164, Generation = 0, Type = XRefEntryType.NotFree },
                new XRef { ObjectNumber = 23, ByteOffset = 0005687936, Generation = 0, Type = XRefEntryType.NotFree },
                new XRef { ObjectNumber = 24, ByteOffset = 0005687978, Generation = 0, Type = XRefEntryType.NotFree },
            };

            Assert.AreEqual(expected, xrefs);
        }
    }
}
