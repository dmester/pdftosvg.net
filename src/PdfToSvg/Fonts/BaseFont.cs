// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.CMaps;
using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using PdfToSvg.Fonts.CompactFonts;
using PdfToSvg.Fonts.OpenType;
using PdfToSvg.Fonts.OpenType.Enums;
using PdfToSvg.Fonts.OpenType.Tables;
using PdfToSvg.Fonts.WidthMaps;
using PdfToSvg.Fonts.Woff;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Fonts
{
    internal abstract class BaseFont : SourceFont
    {
        private static readonly Font fallbackSubstituteFont = new LocalFont("'Times New Roman',serif");
        private static readonly PdfDictionary emptyDict = new();

        private string? name;

        protected OpenTypeFont? openTypeFont;
        protected Exception? openTypeFontException;

        protected PdfDictionary fontDict = emptyDict;

        private readonly CharMap chars = new();
        protected UnicodeMap toUnicode = UnicodeMap.Empty;
        protected CMap cmap = CMap.OneByteIdentity;
        protected WidthMap widthMap = WidthMap.Empty;

        public static BaseFont Fallback { get; } = Create(
            new PdfDictionary {
                { Names.Subtype, Names.Type1 },
                { Names.BaseFont, StandardFonts.TimesRoman },
            },
            FontResolver.LocalFonts,
            CancellationToken.None);

        public override string? Name => name;

        public bool HasGlyphSubstitutions { get; private set; }

        public Font SubstituteFont { get; private set; } = fallbackSubstituteFont;

        protected BaseFont() { }

        protected virtual void OnInit(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Read font
            try
            {
                openTypeFont = GetOpenTypeFont(cancellationToken);
            }
            catch (Exception ex)
            {
                openTypeFontException = ex;
            }

            if (openTypeFont != null)
            {
                HasGlyphSubstitutions = openTypeFont.Tables.Any(t => t.Tag == "GSUB");
            }

            // ToUnicode
            if (fontDict.TryGetDictionary(Names.ToUnicode, out var toUnicode) && toUnicode.Stream != null)
            {
                this.toUnicode = UnicodeMap.Create(toUnicode.Stream, cancellationToken);
            }
            else
            {
                this.toUnicode = UnicodeMap.Empty;
            }

            // Name
            if (fontDict.TryGetName(Names.BaseFont, out var name))
            {
                if ((string.IsNullOrEmpty(name.Value) || name.Value.StartsWith("CIDFont+")) && openTypeFont != null)
                {
                    this.name = openTypeFont.Names.FontFamily + "-" + openTypeFont.Names.FontSubfamily;
                }
                else
                {
                    this.name = name.Value;
                }
            }
        }

        protected virtual void OnPostInit(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            chars.TryPopulate(GetChars, toUnicode, optimizeForEmbeddedFont: false);
        }

        private OpenTypeFont? GetOpenTypeFont(CancellationToken cancellationToken)
        {
            if (fontDict.TryGetDictionary(Names.FontDescriptor, out var fontDescriptor) ||
                fontDict.TryGetDictionary(Names.DescendantFonts / Indexes.First / Names.FontDescriptor, out fontDescriptor))
            {
                // FontFile2 (TrueType)
                if (fontDescriptor.TryGetStream(Names.FontFile2, out var fontFile2))
                {
                    try
                    {
                        using var fontFileStream = fontFile2.OpenDecoded(cancellationToken);
                        var fontFileData = fontFileStream.ToArray();
                        var openTypeFont = OpenTypeFont.Parse(fontFileData);
                        OpenTypeSanitizer.Sanitize(openTypeFont);
                        return openTypeFont;
                    }
                    catch (Exception ex)
                    {
                        throw new FontException("Failed to parse TrueType font.", ex);
                    }
                }

                // FontFile3 (CFF or OpenType)
                if (fontDescriptor.TryGetDictionary(Names.FontFile3, out var fontFile3) && fontFile3.Stream != null)
                {
                    if (fontFile3.GetNameOrNull(Names.Subtype) == Names.OpenType)
                    {
                        try
                        {
                            using var fontFileStream = fontFile3.Stream.OpenDecoded(cancellationToken);
                            var fontFileData = fontFileStream.ToArray();
                            var openTypeFont = OpenTypeFont.Parse(fontFileData);
                            OpenTypeSanitizer.Sanitize(openTypeFont);
                            return openTypeFont;
                        }
                        catch (Exception ex)
                        {
                            throw new FontException("Failed to parse OpenType font.", ex);
                        }
                    }
                    else
                    {
                        try
                        {
                            using var fontFileStream = fontFile3.Stream.OpenDecoded(cancellationToken);
                            var fontFileData = fontFileStream.ToArray();

                            var compactFontSet = CompactFontParser.Parse(fontFileData, maxFontCount: 1);

                            var openTypeFont = new OpenTypeFont();
                            var cffTable = new CffTable { Content = compactFontSet };
                            openTypeFont.Tables.Add(cffTable);

                            OpenTypeSanitizer.Sanitize(openTypeFont);
                            return openTypeFont;
                        }
                        catch (Exception ex)
                        {
                            throw new FontException("Failed to parse CFF font.", ex);
                        }
                    }
                }
            }

            return null;
        }

        protected virtual IEnumerable<CharInfo> GetChars()
        {
            yield break;
        }

        private void OverwriteOpenTypeGlyphWidths(OpenTypeFont inputFont)
        {
            var head = inputFont.Tables.Get<HeadTable>();
            var hhea = inputFont.Tables.Get<HheaTable>();
            var maxp = inputFont.Tables.Get<MaxpTable>();

            if (head == null || hhea == null || maxp == null)
            {
                return;
            }

            var nonZeroWidthGlyphs = chars
                .Where(ch => ch.GlyphIndex != null)
                .Select(ch => new
                {
                    Index = (int)ch.GlyphIndex!,
                    Char = ch,
                    Width = widthMap.GetWidth(ch) * head.UnitsPerEm,
                })
                .Where(ch => ch.Width != 0)
                .ToList();

            if (nonZeroWidthGlyphs.Count == 0)
            {
                return;
            }

            // Update hmtx table (it is used by Firefox)
            var hmtx = inputFont.Tables.Get<HmtxTable>();
            if (hmtx != null)
            {
                var originalMetrics = hmtx.HorMetrics;
                var originalLsb = hmtx.LeftSideBearings;

                hmtx.HorMetrics = new LongHorMetricRecord[maxp.NumGlyphs];
                hmtx.LeftSideBearings = new short[0];
                hhea.NumberOfHMetrics = maxp.NumGlyphs;

                for (var i = 0; i < hmtx.HorMetrics.Length; i++)
                {
                    var metric = hmtx.HorMetrics[i] = new LongHorMetricRecord();

                    if (i < originalMetrics.Length)
                    {
                        metric.AdvanceWidth = originalMetrics[i].AdvanceWidth;
                        metric.LeftSideBearing = originalMetrics[i].LeftSideBearing;
                    }
                    else
                    {
                        if (originalMetrics.Length > 0)
                        {
                            metric.AdvanceWidth = originalMetrics[originalMetrics.Length - 1].AdvanceWidth;
                        }

                        var lsbIndex = i - originalMetrics.Length;
                        if (lsbIndex < originalLsb.Length)
                        {
                            metric.LeftSideBearing = originalLsb[lsbIndex];
                        }
                    }
                }

                foreach (var glyph in nonZeroWidthGlyphs)
                {
                    if (glyph.Index < hmtx.HorMetrics.Length)
                    {
                        hmtx.HorMetrics[glyph.Index].AdvanceWidth =
                            glyph.Width <= ushort.MinValue ? ushort.MinValue :
                            glyph.Width >= ushort.MaxValue ? ushort.MaxValue :
                            (ushort)glyph.Width;
                    }
                }
            }

            // Update CFF widths (they are used by Chrome)
            var cff = inputFont.Tables.Get<CffTable>()?.Content?.Fonts[0];
            if (cff != null)
            {
                var medianWidth = nonZeroWidthGlyphs
                    .Select(glyph => glyph.Width)
                    .OrderBy(width => width)
                    .ElementAt(nonZeroWidthGlyphs.Count / 2);

                cff.PrivateDict.DefaultWidthX = medianWidth;
                cff.PrivateDict.NominalWidthX = medianWidth;

                foreach (var fd in cff.FDArray)
                {
                    fd.PrivateDict.DefaultWidthX = medianWidth;
                    fd.PrivateDict.NominalWidthX = medianWidth;
                }

                foreach (var glyph in nonZeroWidthGlyphs)
                {
                    if (glyph.Index < cff.Glyphs.Count)
                    {
                        var cffGlyph = cff.Glyphs[glyph.Index];

                        cffGlyph.Width = glyph.Width;
                        cffGlyph.CharString.Width = glyph.Width == medianWidth ? null : glyph.Width - medianWidth;
                    }
                }
            }
        }

        private void RecreateOpenTypeCMap(OpenTypeFont font)
        {
            var maxpTable = font.Tables.Get<MaxpTable>();
            var numGlyphs = maxpTable?.NumGlyphs ?? ushort.MaxValue;

            var cmapTable = new CMapTable();
            var nameTable = new NameTable();

            var allChars = chars
                .Where(ch => ch.GlyphIndex != null)
                .Select(ch =>
                {
                    var unicode = Utf16Encoding.DecodeCodePoint(ch.Unicode, 0, out var _);
                    return new OpenTypeCMapRange(unicode, unicode, ch.GlyphIndex!.Value);
                })
                .DistinctBy(ch => ch.StartUnicode);

            cmapTable.EncodingRecords = new[]
            {
                new CMapEncodingRecord
                {
                    PlatformID = OpenTypePlatformID.Windows,
                    EncodingID = 1,
                    Content = OpenTypeCMapEncoder.EncodeFormat4(allChars),
                }
            };

            nameTable.Version = 0;
            nameTable.NameRecords = font
                .Names
                .Select(name => new NameRecord
                {
                    NameID = name.Key,
                    PlatformID = OpenTypePlatformID.Windows,
                    EncodingID = 1,
                    LanguageID = 0x0409,
                    Content = Encoding.BigEndianUnicode.GetBytes(name.Value),
                })

                // Order stipulated by spec
                .OrderBy(x => x.NameID)

                .ToArray();

            font.Tables.Remove<NameTable>();
            font.Tables.Remove<CMapTable>();
            font.Tables.Add(cmapTable);
            font.Tables.Add(nameTable);
        }

        private static BaseFont Create(PdfDictionary fontDict, CancellationToken cancellationToken)
        {
            if (fontDict == null) throw new ArgumentNullException(nameof(fontDict));
            cancellationToken.ThrowIfCancellationRequested();

            BaseFont? font = null;

            var type = fontDict.GetNameOrNull(Names.Subtype);

            if (type == Names.Type0)
            {
                var cidFontType = fontDict.GetNameOrNull(Names.DescendantFonts / Indexes.First / Names.Subtype);

                if (cidFontType == Names.CIDFontType0)
                {
                    font = new CidType0Font();
                }
                else if (cidFontType == Names.CIDFontType2)
                {
                    font = new CidType2Font();
                }
            }
            else if (type == Names.Type1 || type == Names.MMType1)
            {
                font = new Type1Font();
            }
            else if (type == Names.Type3)
            {
                font = new Type3Font();
            }

            if (font == null)
            {
                font = new TrueTypeFont();
            }

            font.fontDict = fontDict;

            return font;
        }

        public static BaseFont Create(PdfDictionary fontDict, FontResolver fontResolver, CancellationToken cancellationToken)
        {
            var font = Create(fontDict, cancellationToken);
            font.OnInit(cancellationToken);
            font.SubstituteFont = fontResolver.ResolveFont(font, cancellationToken);
            font.OnPostInit(cancellationToken);
            return font;
        }

        public static Task<BaseFont> CreateAsync(PdfDictionary fontDict, FontResolver fontResolver, CancellationToken cancellationToken)
        {
            var font = Create(fontDict, cancellationToken);
            font.OnInit(cancellationToken);

            return fontResolver
                .ResolveFontAsync(font, cancellationToken)
                .ContinueWith(t =>
                {
                    font.SubstituteFont = t.Result;
                    font.OnPostInit(cancellationToken);
                    return font;
                }, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
        }

        public override byte[] ToOpenType()
        {
            if (openTypeFont == null)
            {
                throw openTypeFontException ?? new NotSupportedException("This font cannot be converted to OpenType format.");
            }

            chars.TryPopulate(GetChars, toUnicode, optimizeForEmbeddedFont: true);

            var preparedFont = new OpenTypeFont();

            foreach (var table in openTypeFont.Tables)
            {
                preparedFont.Tables.Add(table);
            }

            RecreateOpenTypeCMap(preparedFont);
            OverwriteOpenTypeGlyphWidths(preparedFont);

            OpenTypeSanitizer.Sanitize(preparedFont);

            var binaryOtf = preparedFont.ToByteArray();
            return binaryOtf;
        }

        public override byte[] ToWoff()
        {
            if (openTypeFont == null)
            {
                throw openTypeFontException ?? new NotSupportedException("This font cannot be converted to WOFF format.");
            }

            var binaryOtf = ToOpenType();
            return WoffBuilder.FromOpenType(binaryOtf);
        }

        public string Decode(PdfString value, out double width)
        {
            var sb = new StringBuilder(value.Length);
            width = 0;

            for (var i = 0; i < value.Length;)
            {
                var handled = false;
                var character = cmap.GetCharCode(value, i);

                if (!character.IsEmpty)
                {
                    if (!chars.TryGetChar(character.CharCode, out var charInfo))
                    {
                        charInfo = new CharInfo
                        {
                            CharCode = character.CharCode,
                            Unicode = toUnicode.GetUnicode(character.CharCode) ?? CharInfo.NotDef,
                        };
                    }

                    if (charInfo.Unicode != null)
                    {
                        sb.Append(charInfo.Unicode);
                        i += character.CharCodeLength;
                        width += widthMap.GetWidth(charInfo);
                        handled = true;
                    }
                }

                if (!handled)
                {
                    // TODO width
                    sb.Append('\ufffd');
                    i++;
                }
            }

            return sb.ToString();
        }
    }
}
