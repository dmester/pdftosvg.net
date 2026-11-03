// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Fonts.OpenType;
using PdfToSvg.Fonts.OpenType.Tables;
using System.Linq;

namespace PdfToSvg.Tests.Fonts.OpenType.Tables
{
    public class CMapTableTests
    {
        [Test]
        public void Read_InvalidFormat12Ranges()
        {
            var font = new OpenTypeFont();
            font.Tables.Add(new MaxpTableV10 { NumGlyphs = 3 });
            font.Tables.Add(new CMapTable
            {
                EncodingRecords = new[]
                {
                    new CMapEncodingRecord
                    {
                        Content = new CMapFormat12
                        {
                            Groups = new[]
                            {
                                new CMapFormat12Group { StartCharCode = 0, EndCharCode = uint.MaxValue },
                                new CMapFormat12Group { StartCharCode = 1, EndCharCode = 2, StartGlyphID = 1 },
                                new CMapFormat12Group { StartCharCode = 65, EndCharCode = 66, StartGlyphID = 1 },
                                new CMapFormat12Group { StartCharCode = 100, EndCharCode = 99 },
                                new CMapFormat12Group { StartCharCode = 200, EndCharCode = 201, StartGlyphID = 3 },
                            },
                        },
                    },
                },
            });

            var parsed = OpenTypeFont.Parse(font.ToByteArray());
            var format = (CMapFormat12)parsed.Tables.Get<CMapTable>().EncodingRecords.Single().Content;

            Assert.AreEqual(2, format.Groups.Length);
            Assert.AreEqual(0, format.Groups[0].StartCharCode);
            Assert.AreEqual(2, format.Groups[0].EndCharCode);
            Assert.AreEqual(65, format.Groups[1].StartCharCode);
            Assert.AreEqual(66, format.Groups[1].EndCharCode);
            Assert.AreEqual(1, format.Groups[1].StartGlyphID);

            var roundtrip = OpenTypeFont.Parse(parsed.ToByteArray());
            Assert.AreEqual(5, roundtrip.CMaps.Single().Chars.Count());
        }
    }
}
