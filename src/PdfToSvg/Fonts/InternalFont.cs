// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing;
using PdfToSvg.Encodings;
using PdfToSvg.Fonts;
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
            DefaultFontResolver.Instance,
            CancellationToken.None);

        private readonly WidthMap widthMap;
        private readonly ITextDecoder textDecoder;

        public InternalFont(PdfDictionary font, IFontResolver fontResolver, CancellationToken cancellationToken)
        {
            if (font == null) throw new ArgumentNullException(nameof(font));
            if (fontResolver == null) throw new ArgumentNullException(nameof(fontResolver));

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
                textDecoder = EncodingFactory.Create(encoding);
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
