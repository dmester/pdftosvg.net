// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using PdfToSvg.Fonts.OpenType;
using PdfToSvg.Fonts.OpenType.Tables;
using PdfToSvg.Fonts.Type1;
using PdfToSvg.Fonts.WidthMaps;
using PdfToSvg.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg.Fonts
{
    internal sealed class Type1Font : BaseFont
    {
        protected override void OnInit(CancellationToken cancellationToken)
        {
            base.OnInit(cancellationToken);
            widthMap = Type1WidthMap.Parse(fontDict);
        }

        private Dictionary<string, uint> GetPostGlyphIndexLookup()
        {
            var lookup = new Dictionary<string, uint>();

            if (openTypeFont != null)
            {
                var postTable = openTypeFont.Tables.Get<PostTable>();
                if (postTable != null)
                {
                    for (var glyphIndex = 0u; glyphIndex < postTable.GlyphNames.Length; glyphIndex++)
                    {
                        lookup.TryAdd(postTable.GlyphNames[glyphIndex], glyphIndex);
                    }
                }
            }

            return lookup;
        }

        protected override IEnumerable<CharInfo> GetChars()
        {
            // ISO 32000-2 section 9.6.5.2
            var encoding = pdfFontEncoding ?? openTypeFontEncoding ?? SingleByteEncoding.Standard;

            var postGlyphIndexes = GetPostGlyphIndexLookup();
            var cmap = openTypeFont?.CMaps.OrderByPriority().FirstOrDefault();

            for (var charCode = 0u; charCode <= byte.MaxValue; charCode++)
            {
                var glyphName = encoding.GetGlyphName((byte)charCode);
                var encodingUnicode = encoding.GetUnicode((byte)charCode);

                uint? glyphIndex;

                if (glyphName != null && postGlyphIndexes.TryGetValue(glyphName, out var postGlyphIndex))
                {
                    glyphIndex = postGlyphIndex;
                }
                else if (cmap != null && encodingUnicode != null)
                {
                    glyphIndex = cmap.ToGlyphIndex(encodingUnicode);
                }
                else
                {
                    glyphIndex = null;
                }

                if (glyphName != null || glyphIndex != null)
                {
                    yield return new CharInfo
                    {
                        CharCode = charCode,
                        GlyphIndex = glyphIndex,
                        GlyphName = glyphName,
                        Unicode = encodingUnicode ?? CharInfo.NotDef,
                    };
                }
            }
        }

        public override string ToString() => base.ToString() + "; Type 1";
    }
}
