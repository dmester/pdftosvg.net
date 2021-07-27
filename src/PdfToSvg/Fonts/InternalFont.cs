// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing;
using PdfToSvg.Encodings;
using PdfToSvg.Fonts;
using PdfToSvg.Fonts.OpenType;
using PdfToSvg.Parsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Fonts
{
    // TODO Create document wide font cache
    [DebuggerDisplay("{Name,nq}")]
    internal class InternalFont
    {
        public string? Name { get; }

        public Font SubstituteFont { get; }

        public static InternalFont Fallback { get; } = new InternalFont(
            new PdfDictionary {
                { Names.Subtype, Names.Type1 },
                { Names.BaseFont, StandardFonts.TimesRoman },
            },
            FontResolver.Default,
            CancellationToken.None);

        private readonly WidthMap widthMap;
        private readonly ITextDecoder[] textDecoders;

        public InternalFont(PdfDictionary font, FontResolver fontResolver, CancellationToken cancellationToken)
        {
            if (font == null) throw new ArgumentNullException(nameof(font));
            if (fontResolver == null) throw new ArgumentNullException(nameof(fontResolver));

            // Parse TTF
            OpenTypeFont? trueTypeFont = null;

            if (font.TryGetStream(Names.DescendantFonts / Indexes.First / Names.FontDescriptor / Names.FontFile2, out var fontFile2))
            {
                try
                {
                    using var fontFileStream = fontFile2.OpenDecoded(cancellationToken);
                    trueTypeFont = OpenTypeFont.Parse(fontFileStream);
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Failed to parse TrueType font. {0}", ex);
                }
            }

            // Name
            if (font.TryGetName(Names.BaseFont, out var name))
            {
                Name = name.Value;

                if ((string.IsNullOrEmpty(Name) || Name.StartsWith("CIDFont+")) && trueTypeFont != null)
                {
                    Name = trueTypeFont.FontFamily + "-" + trueTypeFont.FontSubfamily;
                }
            }

            // Substitute font
            if (Name == null)
            {
                SubstituteFont = new LocalFont("Sans-Serif");
            }
            else
            {
                SubstituteFont = fontResolver.ResolveFont(Name, cancellationToken);
            }

            // Create text decoders
            // The second and following decoders are used as fallback if the primary decoder fails to decode a
            // character. Some test PDFs had incomplete /ToUnicode maps, but the text might still be decoded properly
            // using a fallback decoder.
            var textDecoders = new List<ITextDecoder>();

            if (font.TryGetDictionary(Names.ToUnicode, out var toUnicode) && toUnicode.Stream != null)
            {
                textDecoders.Add(CMapParser.Parse(toUnicode.Stream, cancellationToken));
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
