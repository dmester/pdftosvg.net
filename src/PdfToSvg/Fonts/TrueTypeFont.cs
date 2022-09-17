// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using PdfToSvg.Fonts.OpenType;
using PdfToSvg.Fonts.OpenType.Enums;
using PdfToSvg.Fonts.OpenType.Tables;
using PdfToSvg.Fonts.WidthMaps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg.Fonts
{
    internal sealed class TrueTypeFont : BaseFont
    {
        protected override void OnInit(CancellationToken cancellationToken)
        {
            base.OnInit(cancellationToken);

            widthMap = Type1WidthMap.Parse(fontDict);
            PopulateChars(GetChars());
        }

        private string[] GetGlyphNameLookup()
        {
            var lookup = ArrayUtils.Empty<string>();

            if (openTypeFont != null)
            {
                var postTable = openTypeFont.Tables.Get<PostTable>();
                if (postTable != null)
                {
                    lookup = postTable.GlyphNames;
                }
            }

            return lookup;
        }

        private IEnumerable<CharInfo> GetChars()
        {
            // ISO 32000-2 section 9.6.5.4
            var descriptor = fontDict.GetDictionaryOrEmpty(Names.FontDescriptor);
            var fontFlags = (FontFlags)descriptor.GetValueOrDefault(Names.Flags, 0);
            var isSymbolic = fontFlags.HasFlag(FontFlags.Symbolic);
            var encodingDefinition = fontDict.GetValueOrDefault(Names.Encoding);

            var cmaps = openTypeFont?.CMaps ?? ArrayUtils.Empty<OpenTypeCMap>();
            var postGlyphNames = GetGlyphNameLookup();
            var encoding = EncodingFactory.Create(encodingDefinition);

            var chars = Enumerable.Empty<CharInfo>();

            if (openTypeFont == null)
            {
                chars = Enumerable.Range(0, 256)
                    .Select(charCode => new CharInfo
                    {
                        CharCode = (uint)charCode,
                        GlyphName = encoding.GetGlyphName((byte)charCode),
                        Unicode = encoding.GetUnicode((byte)charCode) ?? CharInfo.NotDef,
                    })
                    .Where(ch => ch.Unicode != null);
            }
            else if (encodingDefinition == null || isSymbolic)
            {
                // Symbolic font
                var cmap =
                    cmaps.FirstOrDefault(cmap => cmap.PlatformID == OpenTypePlatformID.Windows && cmap.EncodingID == 0) ??
                    cmaps.FirstOrDefault(cmap => cmap.PlatformID == OpenTypePlatformID.Macintosh && cmap.EncodingID == 0);

                if (cmap != null)
                {
                    chars = cmap.Chars
                        .Select(ch => new CharInfo
                        {
                            CharCode = ch.Unicode & 0xff,
                            GlyphIndex = ch.GlyphIndex,
                            Unicode = Utf16Encoding.EncodeCodePoint(ch.Unicode) ?? CharInfo.NotDef,
                        });
                }
            }
            else
            {
                var glyphNames = Enumerable.Empty<CharInfo?>();

                var cmap31 = cmaps.FirstOrDefault(cmap => cmap.PlatformID == OpenTypePlatformID.Windows && cmap.EncodingID == 1);
                if (cmap31 != null)
                {
                    // Glyph name -> AGL -> Unicode -> CMap -> Glyph index
                    glyphNames = cmap31.Chars
                        .Select(ch =>
                        {
                            var unicode = Utf16Encoding.EncodeCodePoint(ch.Unicode);

                            if (unicode != null)
                            {
                                if (AdobeGlyphList.TryGetGlyphName(unicode, out var glyphName))
                                {
                                    return new CharInfo
                                    {
                                        GlyphIndex = ch.GlyphIndex,
                                        Unicode = unicode,
                                        GlyphName = glyphName,
                                    };
                                }
                            }

                            return null;
                        });
                }
                else
                {
                    var cmap10 = cmaps.FirstOrDefault(cmap => cmap.PlatformID == OpenTypePlatformID.Macintosh && cmap.EncodingID == 0);
                    if (cmap10 != null)
                    {
                        // Glyph name -> Mac OS Roman -> Char code -> CMap -> Glyph index
                        var macos = new MacRomanEncoding();

                        glyphNames = cmap10.Chars
                           .Select(ch =>
                           {
                               var glyphName = macos.GetGlyphName((byte)ch.Unicode);
                               var unicode = macos.GetUnicode((byte)ch.Unicode);

                               if (glyphName != null && unicode != null)
                               {
                                   return new CharInfo
                                   {
                                       GlyphName = glyphName,
                                       GlyphIndex = ch.GlyphIndex,
                                       Unicode = unicode,
                                   };
                               }

                               return null;
                           });
                    }
                }

                // Glyph name -> Post table -> Glyph index
                glyphNames = glyphNames.Concat(postGlyphNames
                    .Select((glyphName, glyphIndex) =>
                    {
                        AdobeGlyphList.TryGetUnicode(glyphName, out var unicode);

                        return new CharInfo
                        {
                            GlyphIndex = (uint)glyphIndex,
                            Unicode = unicode ?? CharInfo.NotDef,
                            GlyphName = glyphName,
                        };
                    }));

                // Char code -> Encoding -> Glyph name
                var charCodeLookup = Enumerable.Range(0, 256)
                    .Select(charCode => new { GlyphName = encoding.GetGlyphName((byte)charCode), CharCode = charCode })
                    .Where(ch => ch.GlyphName != null)
                    .ToLookup(ch => ch.GlyphName, ch => (uint)ch.CharCode);

                chars = glyphNames
                    .WhereNotNull()
                    .Select(glyph =>
                    {
                        var charCode = charCodeLookup[glyph.GlyphName].Cast<uint?>().FirstOrDefault();

                        if (charCode.HasValue)
                        {
                            glyph.CharCode = charCode.Value;
                            return glyph;
                        }

                        return null;
                    })
                    .WhereNotNull();
            }

            return chars;
        }

    }
}
