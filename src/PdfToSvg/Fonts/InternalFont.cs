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
    internal class InternalFont : SourceFont
    {
        private static readonly Font fallbackFont = new LocalFont("'Times New Roman',serif");

        private readonly string? name;
        private readonly OpenTypeFont? trueTypeFont;
        private readonly Exception? trueTypeFontException;

        public override string? Name => name;

        public Font SubstituteFont { get; private set; } = fallbackFont;

        public static InternalFont Fallback { get; } = new InternalFont(
            new PdfDictionary {
                { Names.Subtype, Names.Type1 },
                { Names.BaseFont, StandardFonts.TimesRoman },
            },
            CancellationToken.None);

        private readonly WidthMap widthMap;
        private readonly ITextDecoder[] textDecoders;

        private InternalFont(PdfDictionary font, CancellationToken cancellationToken)
        {
            if (font == null) throw new ArgumentNullException(nameof(font));

            CMap? unicodeCMap = null;

            if (font.TryGetDictionary(Names.ToUnicode, out var toUnicode) && toUnicode.Stream != null)
            {
                unicodeCMap = CMapParser.Parse(toUnicode.Stream, cancellationToken);
            }

            // Parse TTF
            if (font.TryGetStream(Names.FontDescriptor / Names.FontFile2, out var fontFile2) ||
                font.TryGetStream(Names.DescendantFonts / Indexes.First / Names.FontDescriptor / Names.FontFile2, out fontFile2))
            {
                try
                {
                    using var fontFileStream = fontFile2.OpenDecoded(cancellationToken);
                    trueTypeFont = OpenTypeFont.Parse(fontFileStream);
                }
                catch (Exception ex)
                {
                    trueTypeFontException = new FontException("Failed to parse TrueType font.", ex);
                }
            }

            if (font.TryGetStream(Names.FontDescriptor / Names.FontFile3, out var fontFile3) ||
                font.TryGetStream(Names.DescendantFonts / Indexes.First / Names.FontDescriptor / Names.FontFile3, out fontFile3))
            {
                try
                {
                    using var fontFileStream = fontFile3.OpenDecoded(cancellationToken);
                    var fontFileData = fontFileStream.ToArray();

                    var compactFontSet = CompactFontParser.Parse(fontFileData,
                        customCMap: unicodeCMap?.ToLookup(),
                        maxFontCount: 1);

                    trueTypeFont = OpenTypeFont.FromCompactFont(compactFontSet.Fonts.First());
                }
                catch (Exception ex)
                {
                    trueTypeFontException = new FontException("Failed to parse CFF font.", ex);
                }
            }

            // Name
            if (font.TryGetName(Names.BaseFont, out var name))
            {
                if ((string.IsNullOrEmpty(name.Value) || name.Value.StartsWith("CIDFont+")) && trueTypeFont != null)
                {
                    this.name = trueTypeFont.Names.FontFamily + "-" + trueTypeFont.Names.FontSubfamily;
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
                if (trueTypeFont != null &&
                    encoding is PdfName encodingName &&
                    (encodingName == Names.IdentityH || encodingName == Names.IdentityV))
                {
                    if (font.TryGetStream(Names.DescendantFonts / Indexes.First / Names.CIDToGIDMap, out var cidToGidMapStream))
                    {
                        using var stream = cidToGidMapStream.OpenDecoded(cancellationToken);
                        textDecoders.Add(TrueTypeEncoding.Create(trueTypeFont, stream) ?? (ITextDecoder)new Utf16Encoding());
                    }
                    else
                    {
                        textDecoders.Add(TrueTypeEncoding.Create(trueTypeFont, Stream.Null) ?? (ITextDecoder)new Utf16Encoding());
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
        }

        public static InternalFont Create(PdfDictionary fontDict, FontResolver fontResolver, CancellationToken cancellationToken)
        {
            var fontTask = CreateAsync(fontDict, fontResolver, cancellationToken);
#if NET40
            return fontTask.Result;
#else
            return fontTask.ConfigureAwait(false).GetAwaiter().GetResult();
#endif
        }

        public static Task<InternalFont> CreateAsync(PdfDictionary fontDict, FontResolver fontResolver, CancellationToken cancellationToken)
        {
            var internalFont = new InternalFont(fontDict, cancellationToken);

            return fontResolver
                .ResolveFontAsync(internalFont, cancellationToken)
                .ContinueWith(t =>
                {
                    internalFont.SubstituteFont = t.Result;
                    return internalFont;
                }, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
        }

        public override byte[] ToOpenType()
        {
            if (trueTypeFont == null)
            {
                throw trueTypeFontException ?? new NotSupportedException("This font cannot be converted to OpenType format.");
            }

            return trueTypeFont.ToByteArray();
        }

        public override byte[] ToWoff()
        {
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
