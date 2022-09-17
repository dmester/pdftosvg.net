// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using PdfToSvg.Fonts.CompactFonts;
using PdfToSvg.Fonts.OpenType;
using PdfToSvg.Fonts.OpenType.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg.Fonts
{
    internal sealed class CidType0Font : Type0Font
    {
        protected override void OnInit(CancellationToken cancellationToken)
        {
            base.OnInit(cancellationToken);

            PopulateChars(GetChars());
        }

        private IEnumerable<CidChar> GetChars()
        {
            // ISO 32000-2 section 9.7.4.2
            if (openTypeFont != null)
            {
                var numGlyphs = openTypeFont.NumGlyphs;
                var cmap = openTypeFont.CMaps.OrderByPriority().FirstOrDefault();

                var charSet = new List<int>();
                CompactFontSet? cff = null;

                var untypedCffTable = openTypeFont.Tables.FirstOrDefault(x => x.Tag == "CFF ");
                if (untypedCffTable is CffTable cffTable)
                {
                    cff = cffTable.Content;
                }
                else if (untypedCffTable is RawTable rawTable && rawTable.Content != null)
                {
                    cff = CompactFontParser.Parse(rawTable.Content);
                }

                if (cff != null && cff.Fonts.Count > 0 && cff.Fonts[0].IsCIDFont)
                {
                    charSet = cff.Fonts[0].CharSet;
                }

                foreach (var ch in cmap.Chars)
                {
                    var cid = ch.GlyphIndex < charSet.Count ? (uint)charSet[(int)ch.GlyphIndex] : ch.GlyphIndex;

                    yield return new CidChar
                    {
                        Cid = cid,
                        GlyphIndex = ch.GlyphIndex,
                        Unicode = Utf16Encoding.EncodeCodePoint(ch.Unicode),
                    };
                }

                for (var glyphIndex = 0u; glyphIndex < numGlyphs; glyphIndex++)
                {
                    var cid = glyphIndex < charSet.Count ? (uint)charSet[(int)glyphIndex] : glyphIndex;

                    yield return new CidChar
                    {
                        Cid = cid,
                        GlyphIndex = glyphIndex,
                    };
                }
            }
        }
    }
}
