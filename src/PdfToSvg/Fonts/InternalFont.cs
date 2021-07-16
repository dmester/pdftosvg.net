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
        private readonly ITextDecoder textDecoder;

        public InternalFont(PdfDictionary font, FontResolver fontResolver, CancellationToken cancellationToken)
        {
            if (font == null) throw new ArgumentNullException(nameof(font));
            if (fontResolver == null) throw new ArgumentNullException(nameof(fontResolver));

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

            if (font.TryGetName(Names.BaseFont, out var name))
            {
                Name = name.Value;
            }

            if (Name == null)
            {
                SubstituteFont = new LocalFont("Sans-Serif");
            }
            else
            {
                SubstituteFont = fontResolver.ResolveFont(Name, cancellationToken);
            }

            if (font.TryGetDictionary(Names.ToUnicode, out var toUnicode) && toUnicode.Stream != null)
            {
                textDecoder = CMapParser.Parse(toUnicode.Stream, cancellationToken);
            }
            else if (font.TryGetValue(Names.Encoding, out var encoding) && encoding != null)
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
                        textDecoder = TrueTypeEncoding.Create(trueTypeFont, stream) ?? (ITextDecoder)new Utf16Encoding();
                    }
                    else
                    {
                        textDecoder = TrueTypeEncoding.Create(trueTypeFont, Stream.Null) ?? (ITextDecoder)new Utf16Encoding();
                    }
                }
                else
                {
                    textDecoder = EncodingFactory.Create(encoding);
                }
            }
            else
            {
                // TODO check
                textDecoder = new WinAnsiEncoding();
            }

            widthMap = WidthMap.Parse(font);
        }

        public string Decode(PdfString value, out double width)
        {
            var sb = new StringBuilder(value.Length);
            width = 0;

            for (var i = 0; i < value.Length;)
            {
                var character = textDecoder.GetCharacter(value, i);
                if (character.IsEmpty)
                {
                    // TODO width
                    sb.Append('\ufffd');
                    i++;
                }
                else
                {
                    sb.Append(character.DestinationString);
                    i += character.SourceLength;
                    width += widthMap.GetWidth(character);
                }
            }

            return sb.ToString();
        }
    }
}
