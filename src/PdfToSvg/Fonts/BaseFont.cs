// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing;
using PdfToSvg.Encodings;
using PdfToSvg.Fonts;
using PdfToSvg.Fonts.CompactFonts;
using PdfToSvg.Fonts.OpenType;
using PdfToSvg.Fonts.OpenType.Enums;
using PdfToSvg.Fonts.OpenType.Tables;
using PdfToSvg.Fonts.Woff;
using PdfToSvg.IO;
using PdfToSvg.Parsing;
using PdfToSvg.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Fonts
{
    [DebuggerDisplay("{Name,nq}")]
    internal class BaseFont : SourceFont
    {
        private static readonly Font fallbackFont = new LocalFont("'Times New Roman',serif");

        private readonly string? name;
        private readonly OpenTypeFont? openTypeFont;
        private readonly Exception? openTypeFontException;

        public override string? Name => name;

        public Font SubstituteFont { get; private set; } = fallbackFont;

        public static BaseFont Fallback { get; } = new BaseFont(
            new PdfDictionary {
                { Names.Subtype, Names.Type1 },
                { Names.BaseFont, StandardFonts.TimesRoman },
            },
            CancellationToken.None);

        private readonly WidthMap widthMap;
        private readonly ITextDecoder[] textDecoders;

        private BaseFont(PdfDictionary font, CancellationToken cancellationToken)
        {
            if (font == null) throw new ArgumentNullException(nameof(font));

            CMap? unicodeCMap = null;

            if (font.TryGetDictionary(Names.ToUnicode, out var toUnicode) && toUnicode.Stream != null)
            {
                unicodeCMap = CMapParser.Parse(toUnicode.Stream, cancellationToken);
            }

            // Read font
            if (font.TryGetDictionary(Names.FontDescriptor, out var fontDescriptor) ||
                font.TryGetDictionary(Names.DescendantFonts / Indexes.First / Names.FontDescriptor, out fontDescriptor))
            {
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
                        openTypeFontException = new FontException("Failed to parse TrueType font.", ex);
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
                            openTypeFont = OpenTypeFont.Parse(fontFileData);
                        }
                        catch (Exception ex)
                        {
                            openTypeFontException = new FontException("Failed to parse TrueType font.", ex);
                        }
                    }
                    else
                    {
                        try
                        {
                            using var fontFileStream = fontFile3.Stream.OpenDecoded(cancellationToken);
                            var fontFileData = fontFileStream.ToArray();

                            var compactFontSet = CompactFontParser.Parse(fontFileData, maxFontCount: 1);
                            openTypeFont = OpenTypeFont.FromCompactFont(compactFontSet.Fonts.First());
                        }
                        catch (Exception ex)
                        {
                            openTypeFontException = new FontException("Failed to parse CFF font.", ex);
                        }
                    }
                }
            }

            // Name
            if (font.TryGetName(Names.BaseFont, out var name))
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

            // Create text decoders
            // The second and following decoders are used as fallback if the primary decoder fails to decode a
            // character. Some test PDFs had incomplete /ToUnicode maps, but the text might still be decoded properly
            // using a fallback decoder.
            var textDecoders = new List<ITextDecoder>();

            if (unicodeCMap != null)
            {
                textDecoders.Add(unicodeCMap);
            }

            if (font.TryGetValue(Names.Encoding, out var encoding) && encoding != null)
            {
                // If ToUnicode is missing, we might be able to extract unicode mapping from a TrueType font.
                // The same could probably be implemented for the other font types but we need to start somewhere.
                if (openTypeFont != null &&
                    encoding is PdfName encodingName &&
                    (encodingName == Names.IdentityH || encodingName == Names.IdentityV))
                {
                    if (font.TryGetStream(Names.DescendantFonts / Indexes.First / Names.CIDToGIDMap, out var cidToGidMapStream))
                    {
                        using var stream = cidToGidMapStream.OpenDecoded(cancellationToken);
                        textDecoders.Add(TrueTypeEncoding.Create(openTypeFont, stream) ?? (ITextDecoder)new Utf16Encoding());
                    }
                    else
                    {
                        textDecoders.Add(TrueTypeEncoding.Create(openTypeFont, Stream.Null) ?? (ITextDecoder)new Utf16Encoding());
                    }
                }
                else
                {
                    textDecoders.Add(EncodingFactory.Create(encoding));
                }
            }

            if (textDecoders.Count == 0)
            {
                textDecoders.Add(new WinAnsiEncoding());
            }

            this.textDecoders = textDecoders.ToArray();
            this.widthMap = WidthMap.Parse(font);

            // Some PDFs contain fonts with incomplete, missing or invalid cmaps. Most browsers will refuse to load
            // those fonts, so let's try to reconstruct the cmap out of the original cmap and the ToUnicode dictionary.
            if (openTypeFont != null && unicodeCMap != null)
            {
                ReconstructOpenTypeCMap(openTypeFont, unicodeCMap);
            }
        }

        private static IEnumerable<uint> Range(uint from, uint to)
        {
            if (from <= to)
            {
                for (; ; )
                {
                    yield return from;

                    if (from < to)
                    {
                        from++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private static void ReconstructOpenTypeCMap(OpenTypeFont openTypeFont, CMap unicodeCMap)
        {
            var maxpTable = openTypeFont.Tables.Get<MaxpTable>();
            var cmapTable = openTypeFont.Tables.GetOrCreate<CMapTable>();
            var nameTable = openTypeFont.Tables.GetOrCreate<NameTable>();

            var numGlyphs = maxpTable?.NumGlyphs ?? ushort.MaxValue;

            var toUnicodeLookup = unicodeCMap.ToLookup();

            var charLookup = openTypeFont.CMaps
                .SelectMany(cmap => cmap.Ranges)
                .SelectMany(range => Range(range.StartGlyphIndex, range.EndGlyphIndex)
                    .Select(glyphIndex =>
                    {
                        var cid = glyphIndex - range.StartGlyphIndex + range.StartUnicode;

                        // PDF spec 1.7, section 9.6.6.4
                        // Chars in symbol fonts are mapped by prepending F0 to the single byte codes.
                        if (cid >= 0xf000 && cid <= 0xf0ff)
                        {
                            cid &= 0xff;
                        }

                        if (toUnicodeLookup.TryGetValue(cid, out var unicode))
                        {
                            var unicodeCodePoint = (uint)unicode[0];
                            return new OpenTypeCMapRange(unicodeCodePoint, unicodeCodePoint, glyphIndex);
                        }

                        return new OpenTypeCMapRange(cid, cid, glyphIndex);
                    }))
                .WhereNotNull()
                .ToLookup(range => range.StartUnicode);

            var missingChars = toUnicodeLookup
                .Where(pair => pair.Value.Length == 1)
                .Select(pair =>
                {
                    if (pair.Value.Length == 1)
                    {
                        var unicodeCodePoint = (uint)pair.Value[0];

                        if (!charLookup[unicodeCodePoint].Any())
                        {
                            // Char is missing in font CMap
                            return new OpenTypeCMapRange(unicodeCodePoint, unicodeCodePoint, pair.Key);
                        }
                    }

                    return null;
                })
                .WhereNotNull();

            var allChars = charLookup
                .Select(ch => ch.First())
                .Concat(missingChars)
                .Where(range => range.StartGlyphIndex < numGlyphs);

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
            var BaseFont = new BaseFont(fontDict, cancellationToken);

            return fontResolver
                .ResolveFontAsync(BaseFont, cancellationToken)
                .ContinueWith(t =>
                {
                    BaseFont.SubstituteFont = t.Result;
                    return BaseFont;
                }, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
        }

        public override byte[] ToOpenType()
        {
            if (openTypeFont == null)
            {
                throw openTypeFontException ?? new NotSupportedException("This font cannot be converted to OpenType format.");
            }

            return openTypeFont.ToByteArray();
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
                CharacterCode character = default;

                for (var ti = 0; ti < textDecoders.Length && character.IsEmpty; ti++)
                {
                    character = textDecoders[ti].GetCharacter(value, i);
                }

                if (character.IsEmpty)
                {
                    // TODO width
                    sb.Append('\ufffd');
                    i++;
                }
                else
                {
                    for (var outIndex = 0; outIndex < character.DestinationString.Length; outIndex++)
                    {
                        var ch = character.DestinationString[outIndex];
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

                    i += character.SourceLength;
                    width += widthMap.GetWidth(character);
                }
            }

            return sb.ToString();
        }
    }
}
