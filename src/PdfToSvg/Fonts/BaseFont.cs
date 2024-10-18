// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.CMaps;
using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using PdfToSvg.Fonts.CharStrings;
using PdfToSvg.Fonts.CompactFonts;
using PdfToSvg.Fonts.OpenType;
using PdfToSvg.Fonts.OpenType.Enums;
using PdfToSvg.Fonts.OpenType.Tables;
using PdfToSvg.Fonts.Type1;
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
        private static readonly Font fallbackSubstituteFont = new LocalFont("Arial,sans-serif");
        private static readonly PdfDictionary emptyDict = new();

        private string? name;
        private Font? substituteFontDuplicatedGlyph0;

        protected SingleByteEncoding? pdfFontEncoding;

        protected OpenTypeFont? openTypeFont;
        protected SingleByteEncoding? openTypeFontEncoding;
        protected Exception? openTypeFontException;

        protected PdfDictionary fontDict = emptyDict;
        protected Glyph0Options glyph0Options;

        private readonly CharMap chars = new();
        protected UnicodeMap toUnicode = UnicodeMap.Empty;
        protected CMap cmap = CMap.OneByteIdentity;
        protected WidthMap widthMap = WidthMap.Empty;
        protected bool isSymbolic;
        protected bool isStandardFont;

        public static BaseFont Fallback { get; } = Create(
            new PdfDictionary {
                { Names.Subtype, Names.Type1 },
                { Names.BaseFont, StandardFonts.Helvetica },
                { InternalNames.FallbackFont, true },
            },
            FontResolver.LocalFonts,
            CancellationToken.None);

        public override string? Name => name;

        public bool HasGlyphSubstitutions { get; private set; }

        public Font SubstituteFont { get; private set; } = fallbackSubstituteFont;

        public override bool CanBeExtracted => openTypeFont != null;

        public override bool IsStandardFont => isStandardFont;

        protected BaseFont() { }

        protected virtual void OnInit(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Symbolic
            var descriptor = fontDict.GetDictionaryOrEmpty(Names.FontDescriptor);
            var fontFlags = (FontFlags)descriptor.GetValueOrDefault(Names.Flags, 0);
            isSymbolic |= fontFlags.HasFlag(FontFlags.Symbolic);

            // Read font
            try
            {
                PopulateOpenTypeFont(cancellationToken);

                if (openTypeFont != null)
                {
                    OpenTypeSanitizer.Sanitize(openTypeFont);
                    HasGlyphSubstitutions = openTypeFont.Tables.Any(t => t.Tag == "GSUB");
                }
            }
            catch (Exception ex)
            {
                openTypeFont = null;
                openTypeFontException = ex;
                isStandardFont = false;
            }

            // Encoding
            var baseEncoding = openTypeFontEncoding ?? SingleByteEncoding.Standard;
            var encodingDefinition = fontDict.GetValueOrDefault(Names.Encoding);
            pdfFontEncoding = EncodingFactory.Create(encodingDefinition, baseEncoding);

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

            chars.TryPopulate(GetChars, toUnicode, pdfFontEncoding ?? openTypeFontEncoding, widthMap, optimizeForEmbeddedFont: false);
        }

        private void PopulateOpenTypeFont(CancellationToken cancellationToken)
        {
            if (fontDict.TryGetDictionary(Names.FontDescriptor, out var fontDescriptor) ||
                fontDict.TryGetDictionary(Names.DescendantFonts / Indexes.First / Names.FontDescriptor, out fontDescriptor))
            {
                // FontFile (Type 1)
                if (fontDescriptor.TryGetDictionary(Names.FontFile, out var fontFile) &&
                    fontFile.Stream != null)
                {
                    if (!fontFile.TryGetInteger(Names.Length1, out var length1))
                    {
                        throw new FontException("Failed to parse Type 1 font. Missing Length1.");
                    }

                    if (!fontFile.TryGetInteger(Names.Length2, out var length2))
                    {
                        throw new FontException("Failed to parse Type 1 font. Missing Length2.");
                    }

                    try
                    {
                        using var fontFileStream = fontFile.Stream.OpenDecoded(cancellationToken);
                        var fontFileData = fontFileStream.ToArray();
                        var info = Type1Parser.Parse(fontFileData, length1, length2);

                        openTypeFont = Type1Converter.ConvertToOpenType(info);
                        openTypeFontEncoding = info.Encoding;
                    }
                    catch (Exception ex)
                    {
                        throw new FontException("Failed to parse Type 1 font.", ex);
                    }

                    return;
                }

                // FontFile2 (TrueType)
                if (fontDescriptor.TryGetStream(Names.FontFile2, out var fontFile2))
                {
                    try
                    {
                        using var fontFileStream = fontFile2.OpenDecoded(cancellationToken);
                        var fontFileData = fontFileStream.ToArray();
                        openTypeFont = OpenTypeFont.Parse(fontFileData);
                    }
                    catch (Exception ex)
                    {
                        throw new FontException("Failed to parse TrueType font.", ex);
                    }

                    return;
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
                            openTypeFont = OpenTypeFont.Parse(fontFileData);
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

                            openTypeFont = new OpenTypeFont();
                            var cffTable = new CffTable { Content = compactFontSet };
                            openTypeFont.Tables.Add(cffTable);

                            openTypeFontEncoding = compactFontSet.Fonts.FirstOrDefault()?.Encoding;
                        }
                        catch (Exception ex)
                        {
                            throw new FontException("Failed to parse CFF font.", ex);
                        }
                    }

                    return;
                }
            }

            // Standard font
            if (fontDict.TryGetName(Names.BaseFont, out var name) && !fontDict.ContainsKey(InternalNames.FallbackFont))
            {
                var standardFont = StandardFonts.GetFont(name);
                if (standardFont != null)
                {
                    try
                    {
                        var compactFontSet = CompactFontParser.Parse(standardFont.Data, maxFontCount: 1);

                        openTypeFont = new OpenTypeFont();
                        var cffTable = new CffTable { Content = compactFontSet };
                        openTypeFont.Tables.Add(cffTable);

                        openTypeFontEncoding = standardFont.Encoding;

                        if (standardFont.License != null)
                        {
                            openTypeFont.Names.License = standardFont.License;
                        }

                        isSymbolic |= standardFont.IsSymbolic;
                        isStandardFont = true;
                    }
                    catch (Exception ex)
                    {
                        throw new FontException("Failed to parse standard font " + name + ".", ex);
                    }

                    return;
                }
            }
        }

        protected virtual IEnumerable<CharInfo> GetChars()
        {
            yield break;
        }

        private void OverwriteOpenTypeGlyphWidths(OpenTypeFont inputFont, int glyph0DuplicateIndex)
        {
            var head = inputFont.Tables.Get<HeadTable>();
            var hhea = inputFont.Tables.Get<HheaTable>();
            var maxp = inputFont.Tables.Get<MaxpTable>();
            var hmtx = inputFont.Tables.Get<HmtxTable>();

            if (head == null || hhea == null || maxp == null || hmtx == null)
            {
                return;
            }

            // Expand hmtx table with one entry per glyph
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

            // Update hmtx metrics (it is used by Firefox)
            foreach (var ch in chars)
            {
                if (ch.GlyphIndex == null)
                {
                    continue;
                }

                var width = ch.Width * head.UnitsPerEm;
                if (width != 0)
                {
                    hmtx.HorMetrics[(int)ch.GlyphIndex].AdvanceWidth =
                        width <= ushort.MinValue ? ushort.MinValue :
                        width >= ushort.MaxValue ? ushort.MaxValue :
                        (ushort)width;
                }
            }

            // Width of duplicated glyph 0
            if (glyph0DuplicateIndex > 0)
            {
                hmtx.HorMetrics[glyph0DuplicateIndex] = hmtx.HorMetrics[0];
            }

            // Update CFF widths (they are used by Chrome)
            var cff = inputFont.Tables.Get<CffTable>()?.Content?.Fonts[0];
            if (cff != null)
            {
                var medianWidth = hmtx.HorMetrics
                    .Select(glyph => glyph.AdvanceWidth)
                    .OrderBy(width => width)
                    .ElementAt(hmtx.HorMetrics.Length / 2);

                cff.PrivateDict.DefaultWidthX = medianWidth;
                cff.PrivateDict.NominalWidthX = medianWidth;

                foreach (var fd in cff.FDArray)
                {
                    fd.PrivateDict.DefaultWidthX = medianWidth;
                    fd.PrivateDict.NominalWidthX = medianWidth;
                }

                var count = Math.Min(cff.Glyphs.Count, hmtx.HorMetrics.Length);

                for (var i = 0; i < count; i++)
                {
                    var cffGlyph = cff.Glyphs[i];
                    var horMetric = hmtx.HorMetrics[i];

                    cffGlyph.Width = horMetric.AdvanceWidth;
                    cffGlyph.CharString.Width = horMetric.AdvanceWidth == medianWidth
                        ? null // Use NominalWidthX
                        : horMetric.AdvanceWidth - medianWidth;
                }
            }
        }

        public Font GetSubstituteFontWithDuplicatedGlyph0(FontResolver fontResolver, CancellationToken cancellationToken)
        {
            if (substituteFontDuplicatedGlyph0 == null)
            {
                var withDuplicatedGlyph0 = Create(fontDict, fontResolver, Glyph0Options.Duplicate, cancellationToken);
                var substituteFont = withDuplicatedGlyph0.SubstituteFont;
                Interlocked.CompareExchange(ref substituteFontDuplicatedGlyph0, substituteFont, null);
            }

            return substituteFontDuplicatedGlyph0;
        }

        private int DuplicateGlyph0(OpenTypeFont font)
        {
            const int MaxGlyphZeroReferences = 100;

            var glyph0Ref = chars
                .FirstOrDefault(ch => ch.GlyphIndex == 0);
            if (glyph0Ref == null)
            {
                return -1;
            }

            var duplicateGlyphZero = !chars
                .Where(ch => ch.GlyphIndex == 0)
                .Skip(MaxGlyphZeroReferences)
                .Any();

            if (!duplicateGlyphZero)
            {
                return -1;
            }

            var maxpTable = font.Tables.Get<MaxpTable>();

            var cffTable = font.Tables.Get<CffTable>();
            if (cffTable == null)
            {
                var rawCffTable = font.Tables.OfType<RawTable>().FirstOrDefault(x => x.Tag == "CFF ");
                if (rawCffTable != null && rawCffTable.Content != null)
                {
                    cffTable = new CffTable
                    {
                        Content = CompactFontParser.Parse(rawCffTable.Content)
                    };
                    font.Tables.Remove(rawCffTable);
                    font.Tables.Add(cffTable);
                }
            }

            if (cffTable != null && cffTable.Content != null)
            {
                var glyphs = cffTable.Content.Fonts[0].Glyphs;
                if (glyphs.Count > 0)
                {
                    glyphs.Add(glyphs[0]);

                    if (maxpTable != null)
                    {
                        maxpTable.NumGlyphs++;
                    }

                    // CFF fonts must use post table V3, not including any glyph names

                    return glyphs.Count - 1;
                }
            }

            var locaTable = font.Tables.Get<LocaTable>();
            var glyfTable = font.Tables.Get<GlyfTable>();
            if (locaTable != null && glyfTable != null)
            {
                if (locaTable.Offsets.Length > 1)
                {
                    var startOffset = (int)locaTable.Offsets[0];
                    var endOffset = (int)locaTable.Offsets[1];
                    var lastOffset = (int)locaTable.Offsets.Last();
                    var glyph0Length = endOffset - startOffset;

                    if (glyph0Length > 0)
                    {
                        // Duplicate content
                        var originalContent = glyfTable.Content;
                        var newContent = new byte[lastOffset + glyph0Length];

                        Buffer.BlockCopy(originalContent, 0, newContent, 0, lastOffset);
                        Buffer.BlockCopy(originalContent, 0, newContent, lastOffset, glyph0Length);

                        glyfTable.Content = newContent;
                    }

                    locaTable.Offsets = ArrayUtils.Add(locaTable.Offsets, (uint)(lastOffset + glyph0Length));

                    if (maxpTable != null)
                    {
                        maxpTable.NumGlyphs++;
                    }

                    var postTable = font.Tables.Get<PostTable>();
                    if (postTable is PostTableV2 ||
                        postTable is PostTableV25)
                    {
                        postTable.GlyphNames = ArrayUtils.Add(
                            postTable.GlyphNames,
                            glyph0Ref.GlyphName ?? postTable.GlyphNames[0]);
                    }

                    return locaTable.Offsets.Length - 2;
                }
            }

            return -1;
        }

        private void RecreateOpenTypeCMap(OpenTypeFont font, int glyph0DuplicateIndex)
        {
            var maxpTable = font.Tables.Get<MaxpTable>();
            var numGlyphs = maxpTable?.NumGlyphs ?? ushort.MaxValue;

            var cmapTable = new CMapTable();

            var sourceChars = chars.Where(ch => ch.GlyphIndex != null && ch.GlyphIndex < numGlyphs);

            if (glyph0DuplicateIndex < 0)
            {
                sourceChars = sourceChars.Where(ch => ch.GlyphIndex > 0);
            }

            var allChars = sourceChars
                .Select(ch =>
                {
                    var unicode = Utf16Encoding.DecodeCodePoint(ch.Unicode, 0, out var _);
                    
                    var glyphIndex = ch.GlyphIndex!.Value;
                    if (glyphIndex == 0)
                    {
                        glyphIndex = (uint)glyph0DuplicateIndex;
                    }

                    return new OpenTypeCMapRange(unicode, unicode, glyphIndex);
                })
                .DistinctBy(ch => ch.StartUnicode);

            var encodingRecords = new List<CMapEncodingRecord>(2)
            {
                new CMapEncodingRecord
                {
                    PlatformID = OpenTypePlatformID.Windows,
                    EncodingID = 1,
                    Content = OpenTypeCMapEncoder.EncodeFormat4(allChars, out var format4WasSubsetted),
                },
            };

            if (format4WasSubsetted)
            {
                encodingRecords.Add(new CMapEncodingRecord
                {
                    PlatformID = OpenTypePlatformID.Windows,
                    EncodingID = 10,
                    Content = OpenTypeCMapEncoder.EncodeFormat12(allChars),
                });
            }

            cmapTable.EncodingRecords = encodingRecords

                // Order stipulated by spec
                .OrderBy(x => x.PlatformID)
                .ThenBy(x => x.EncodingID)

                .ToArray();

            font.Tables.Remove<CMapTable>();
            font.Tables.Add(cmapTable);
        }

        private static BaseFont CreateCore(PdfDictionary fontDict, Glyph0Options glyph0Options, CancellationToken cancellationToken)
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
            font.glyph0Options = glyph0Options;

            return font;
        }

        public static BaseFont Create(PdfDictionary fontDict, FontResolver fontResolver, CancellationToken cancellationToken)
        {
            return Create(fontDict, fontResolver, Glyph0Options.None, cancellationToken);
        }

        private static BaseFont Create(PdfDictionary fontDict, FontResolver fontResolver, Glyph0Options glyph0Options, CancellationToken cancellationToken)
        {
            var font = CreateCore(fontDict, glyph0Options, cancellationToken);
            font.OnInit(cancellationToken);
            font.SubstituteFont = fontResolver.ResolveFont(font, cancellationToken);
            font.OnPostInit(cancellationToken);
            return font;
        }

        public static Task<BaseFont> CreateAsync(PdfDictionary fontDict, FontResolver fontResolver, CancellationToken cancellationToken)
        {
            var font = CreateCore(fontDict, Glyph0Options.None, cancellationToken);
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

            chars.TryPopulate(GetChars, toUnicode, pdfFontEncoding ?? openTypeFontEncoding, widthMap, optimizeForEmbeddedFont: true);

            var preparedFont = new OpenTypeFont();

            foreach (var table in openTypeFont.Tables)
            {
                preparedFont.Tables.Add(table);
            }

            // Some PDFs are using the zero glyph as a real character (#35). In a reported case, U+FFFF is mapped to the
            // zero glyph, and then used inside the document. The problem is that browsers will try to use a fallback font
            // instead of our font if it encounters a mapping to glyph 0. If we encounter such a mapping, use a duplicate
            // of glyph 0, which won't be replaced by any browser.
            var duplicatedGlyph0Index = -1;
            if (glyph0Options == Glyph0Options.Duplicate)
            {
                duplicatedGlyph0Index = DuplicateGlyph0(preparedFont);
            }
            
            RecreateOpenTypeCMap(preparedFont, duplicatedGlyph0Index);
            OverwriteOpenTypeGlyphWidths(preparedFont, duplicatedGlyph0Index);

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

        public DecodedString DecodeString(PdfString value) => DecodeString(value, splitWords: false)[0];

        /// <remarks>
        /// The resulting list will always contain at least one string.
        /// </remarks>
        public List<DecodedString> DecodeString(PdfString value, bool splitWords) => DecodeString(value, splitWords, out _);

        /// <remarks>
        /// The resulting list will always contain at least one string.
        /// </remarks>
        public List<DecodedString> DecodeString(PdfString value, bool splitWords, out bool hasGlyph0Reference)
        {
            const uint Whitespace = 0x20;

            var result = new List<DecodedString>(1);
            hasGlyph0Reference = false;

            var wordValue = new StringBuilder(value.Length);
            var wordLength = 0;
            var wordWidth = 0d;

            for (var i = 0; i < value.Length;)
            {
                var character = cmap.GetCharCode(value, i);

                if (!character.IsEmpty)
                {
                    if (splitWords &&
                        character.CharCode == Whitespace &&
                        character.CharCodeLength == 1)
                    {
                        result.Add(new DecodedString(wordValue.ToString(), wordLength, wordWidth));
                        wordValue.Clear();
                        wordWidth = 0d;
                        wordLength = 0;
                    }

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
                        if (charInfo.GlyphIndex == 0)
                        {
                            hasGlyph0Reference = true;
                        }

                        i += character.CharCodeLength;
                        wordValue.Append(charInfo.Unicode);
                        wordWidth += charInfo.Width;
                        wordLength++;
                        continue;
                    }
                }

                // TODO width
                wordValue.Append('\ufffd');
                wordLength++;
                i++;
            }

            result.Add(new DecodedString(wordValue.ToString(), wordLength, wordWidth));
            return result;
        }

        public override string ToString()
        {
            var extra =
                openTypeFont == null ? "; Not embedded" :
                isStandardFont ? "; Standard font" :
                "";
            return (Name ?? "Unnamed font") + extra;
        }
    }
}
