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

        private IEnumerable<CharInfo> GetEncodingChars(SingleByteEncoding encoding)
        {
            return Enumerable.Range(0, 256)
                .Select(charCode => new CharInfo
                {
                    CharCode = (uint)charCode,
                    GlyphName = encoding.GetGlyphName((byte)charCode),
                    Unicode = encoding.GetUnicode((byte)charCode) ?? CharInfo.NotDef,
                })
                .Where(ch => ch.Unicode != null);
        }

        private IEnumerable<CharInfo> GetSymbolicChars(OpenTypeCMap cmap)
        {
            var postGlyphNames = GetGlyphNameLookup();
            var cmapEncoding = cmap.PlatformID == OpenTypePlatformID.Macintosh ? SingleByteEncoding.MacRoman : null;

            return cmap.Chars
               .Where(ch =>
               {
                   // Allowed ranges according to section 9.6.6.4:
                   // 0x0000-0x00FF, 0xF000-0xF0FF, 0xF100-0xF1FF, 0xF200-0xF2FF

                   var hibyte = ch.Unicode >> 8;

                   return
                       hibyte == 0x00 ||
                       hibyte == 0xF0 ||
                       hibyte == 0xF1 ||
                       hibyte == 0xF2;
               })
               .Select(ch => {

                   string? unicode = null;
                   string? glyphName = null;

                   // First hand: glyph names from Post table
                   if (ch.GlyphIndex < postGlyphNames.Length)
                   {
                       glyphName = postGlyphNames[ch.GlyphIndex];
                       AdobeGlyphList.TryGetUnicode(glyphName, out unicode);
                   }

                   // Second hand: Mac OS Roman encoding
                   if (unicode == null && cmapEncoding != null)
                   {
                       unicode = cmapEncoding.GetUnicode((byte)ch.Unicode);
                   }

                   // Third hand:
                   // Since this is a symbolic encoding, there is no point in trying to give the character codes any meaning.
                   // There is a risk that the glyphs are then mapped to characters with special meaning, like soft hyphen, line break etc.
                   // Because of this, ensure all characters are mapped to the private use area.
                   if (unicode == null)
                   {
                       if (ch.Unicode >= 0xF000)
                       {
                           unicode = Utf16Encoding.EncodeCodePoint(ch.Unicode);
                       }
                       else
                       {
                           unicode = Utf16Encoding.EncodeCodePoint(0xF000 | (ch.Unicode & 0xff));
                       }
                   }

                   return new CharInfo
                   {
                       CharCode = ch.Unicode & 0xff,
                       GlyphIndex = ch.GlyphIndex,
                       GlyphName = glyphName,
                       Unicode = unicode ?? CharInfo.NotDef,
                   };
               });
        }

        private IEnumerable<CharInfo> GetNonSymbolicChars(OpenTypeCMap cmap, SingleByteEncoding encoding)
        {
            var postGlyphNames = GetGlyphNameLookup();

            IEnumerable<CharInfo?> glyphNames;

            if (cmap.PlatformID == OpenTypePlatformID.Windows)
            {
                // Glyph name -> AGL -> Unicode -> CMap -> Glyph index
                glyphNames = cmap.Chars
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
                // Glyph name -> Mac OS Roman -> Char code -> CMap -> Glyph index
                var macos = SingleByteEncoding.MacRoman;

                glyphNames = cmap.Chars
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

            return glyphNames
                .WhereNotNull()
                .SelectMany(glyph => charCodeLookup[glyph.GlyphName]
                    .Select(charCode =>
                    {
                        var clone = glyph.Clone();
                        clone.CharCode = charCode;
                        return clone;
                    }));
        }

        protected override IEnumerable<CharInfo> GetChars()
        {
            // ISO 32000-2 section 9.6.5.4
            var cmaps = openTypeFont?.CMaps ?? ArrayUtils.Empty<OpenTypeCMap>();
            var encoding = pdfFontEncoding ?? SingleByteEncoding.Standard;

            if (openTypeFont == null)
            {
                return GetEncodingChars(encoding);
            }

            if (isSymbolic)
            {
                // Symbolic font
                var cmap =
                    cmaps.FirstOrDefault(cmap => cmap.PlatformID == OpenTypePlatformID.Windows && cmap.EncodingID == 0) ??
                    cmaps.FirstOrDefault(cmap => cmap.PlatformID == OpenTypePlatformID.Macintosh && cmap.EncodingID == 0);

                if (cmap != null)
                {
                    return GetSymbolicChars(cmap);
                }
            }
            else
            {
                var cmap =
                    cmaps.FirstOrDefault(cmap => cmap.PlatformID == OpenTypePlatformID.Windows && cmap.EncodingID == 1) ??
                    cmaps.FirstOrDefault(cmap => cmap.PlatformID == OpenTypePlatformID.Macintosh && cmap.EncodingID == 0);

                if (cmap != null)
                {
                    return GetNonSymbolicChars(cmap, encoding);
                }
            }

            return Enumerable.Empty<CharInfo>();
        }

        public override string ToString() => base.ToString() + "; TrueType";
    }
}
