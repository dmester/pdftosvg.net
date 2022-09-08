﻿// Copyright (c) PdfToSvg.NET contributors.
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
        private static readonly Font fallbackFont = new LocalFont("'Times New Roman',serif");

        private readonly Dictionary<uint, CharInfo> chars = new();
        private readonly string? name;

        protected readonly OpenTypeFont? openTypeFont;
        protected readonly Exception? openTypeFontException;

        protected readonly PdfDictionary fontDict;

        protected UnicodeMap toUnicode;
        protected CMap cmap = CMap.OneByteIdentity;
        protected WidthMap widthMap = WidthMap.Empty;

        public static BaseFont Fallback { get; } = new Type1Font(
            new PdfDictionary {
                { Names.Subtype, Names.Type1 },
                { Names.BaseFont, StandardFonts.TimesRoman },
            },
            CancellationToken.None);

        public override string? Name => name;

        public Font SubstituteFont { get; private set; } = fallbackFont;

        public BaseFont(PdfDictionary fontDict, CancellationToken cancellationToken)
        {
            if (fontDict == null) throw new ArgumentNullException(nameof(fontDict));
            this.fontDict = fontDict;

            // Read font
            try
            {
                openTypeFont = GetOpenTypeFont(cancellationToken);
            }
            catch (Exception ex)
            {
                openTypeFontException = ex;
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
                        return OpenTypeFont.Parse(fontFileData);
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
                            return OpenTypeFont.Parse(fontFileData);
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
                            return OpenTypeFont.FromCompactFont(compactFontSet.Fonts.First());
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

        private string ResolveUnicode(CharInfo ch)
        {
            var pdfUnicode = toUnicode.GetUnicode(ch.CharCode);

            // Prio 1: ToUnicode
            if (pdfUnicode != null)
            {
                return pdfUnicode;
            }

            // Prio 2: Unicode from font CMap
            if (ch.Unicode != null)
            {
                return ch.Unicode;
            }

            // Prio 3: Unmicode from glyph name
            if (AdobeGlyphList.TryGetUnicode(ch.GlyphName, out var aglUnicode))
            {
                return aglUnicode;
            }

            return CharInfo.NotDef;
        }

        protected void PopulateChars(IEnumerable<CharInfo> chars)
        {
            const char StartPrivateUseArea = '\uE000';
            const char EndPrivateUseArea = '\uF8FF';

            var usedUnicodeToGidMappings = new Dictionary<string, uint>();
            var usedUnicode = new HashSet<string>();
            var nextReplacementChar = StartPrivateUseArea;

            foreach (var ch in chars)
            {
                if (this.chars.ContainsKey(ch.CharCode))
                {
                    continue;
                }

                ch.Unicode = ResolveUnicode(ch);

                Utf16Encoding.DecodeCodePoint(ch.Unicode, 0, out var codePointLength);

                if (ch.GlyphIndex == null)
                {
                    this.chars[ch.CharCode] = ch;
                }
                else if (
                    ch.Unicode != CharInfo.NotDef &&
                    ch.Unicode.Length == codePointLength &&
                    (
                        !usedUnicodeToGidMappings.TryGetValue(ch.Unicode, out var mappedGid) ||
                        mappedGid == ch.GlyphIndex.Value
                    ))
                {
                    this.chars[ch.CharCode] = ch;
                    usedUnicodeToGidMappings[ch.Unicode] = ch.GlyphIndex.Value;
                }
                else
                {
                    // Remap
                    var replacement = new string(nextReplacementChar, 1);

                    while (!usedUnicode.Add(replacement))
                    {
                        if (nextReplacementChar < EndPrivateUseArea)
                        {
                            nextReplacementChar++;
                            replacement = new string(nextReplacementChar, 1);
                        }
                        else
                        {
                            replacement = null;
                            break;
                        }
                    }

                    if (replacement != null)
                    {
                        ch.Unicode = replacement;
                        nextReplacementChar++;

                        this.chars[ch.CharCode] = ch;
                        usedUnicodeToGidMappings[ch.Unicode] = ch.GlyphIndex.Value;
                    }
                }
            }
        }

        private void ReconstructOpenTypeCMap()
        {
            if (openTypeFont == null)
            {
                return;
            }

            var maxpTable = openTypeFont.Tables.Get<MaxpTable>();
            var cmapTable = openTypeFont.Tables.GetOrCreate<CMapTable>();
            var nameTable = openTypeFont.Tables.GetOrCreate<NameTable>();

            var numGlyphs = maxpTable?.NumGlyphs ?? ushort.MaxValue;

            if (!openTypeFont.Tables.OfType<PostTable>().Any())
            {
                // Required by OTS sanitizer
                openTypeFont.Tables.Add(new PostTableV3());
            }

            var allChars = chars
                .Where(ch => ch.Value.GlyphIndex != null)
                .Select(ch =>
                {
                    var unicode = Utf16Encoding.DecodeCodePoint(ch.Value.Unicode, 0, out var _);
                    return new OpenTypeCMapRange(unicode, unicode, ch.Value.GlyphIndex!.Value);
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
            nameTable.NameRecords = openTypeFont
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
        }

        public static BaseFont Create(PdfDictionary fontDict, FontResolver fontResolver, CancellationToken cancellationToken)
        {
            var fontTask = CreateAsync(fontDict, fontResolver, cancellationToken);
#if NET40
            return fontTask.Result;
#else
            return fontTask.ConfigureAwait(false).GetAwaiter().GetResult();
#endif
        }

        public static Task<BaseFont> CreateAsync(PdfDictionary fontDict, FontResolver fontResolver, CancellationToken cancellationToken)
        {
            BaseFont? font = null;

            var type = fontDict.GetNameOrNull(Names.Subtype);

            if (type == Names.Type0)
            {
                var cidFontType = fontDict.GetNameOrNull(Names.DescendantFonts / Indexes.First / Names.Subtype);

                if (cidFontType == Names.CIDFontType0)
                {
                    font = new CidType0Font(fontDict, cancellationToken);
                }
                else if (cidFontType == Names.CIDFontType2)
                {
                    font = new CidType2Font(fontDict, cancellationToken);
                }
            }
            else if (type == Names.Type1 || type == Names.MMType1)
            {
                font = new Type1Font(fontDict, cancellationToken);
            }
            else if (type == Names.Type3)
            {
                font = new Type3Font(fontDict, cancellationToken);
            }

            if (font == null)
            {
                font = new TrueTypeFont(fontDict, cancellationToken);
            }

            return fontResolver
                .ResolveFontAsync(font, cancellationToken)
                .ContinueWith(t =>
                {
                    font.SubstituteFont = t.Result;
                    return font;
                }, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
        }

        public override byte[] ToOpenType()
        {
            if (openTypeFont == null)
            {
                throw openTypeFontException ?? new NotSupportedException("This font cannot be converted to OpenType format.");
            }

            lock (openTypeFont)
            {
                ReconstructOpenTypeCMap();
                return openTypeFont.ToByteArray();
            }
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
                    if (!chars.TryGetValue(character.CharCode, out var charInfo))
                    {
                        charInfo = new CharInfo
                        {
                            CharCode = character.CharCode,
                            Unicode = toUnicode.GetUnicode(character.CharCode) ?? CharInfo.NotDef,
                        };
                    }

                    if (charInfo.Unicode != null)
                    {
                        for (var outIndex = 0; outIndex < charInfo.Unicode.Length; outIndex++)
                        {
                            var ch = charInfo.Unicode[outIndex];
                            if (ch > '\ufffe' ||
                                ch < '\u0020' &&
                                ch != '\u0009' &&
                                ch != '\u000A' &&
                                ch != '\u000D')
                            {
                                // Invalid XML char according to 
                                // https://www.w3.org/TR/REC-xml/#charsets
                                sb.Append('\ufffd');
                            }
                            else
                            {
                                sb.Append(ch);
                            }
                        }

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