using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing;
using PdfToSvg.Encodings;
using PdfToSvg.Fonts;
using PdfToSvg.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Fonts
{
    // TODO Create document wide font cache
    internal class InternalFont
    {
        public string Name { get; }

        public Font SubstituteFont { get; }

        public static InternalFont Fallback { get; } = new InternalFont();

        private readonly WidthMap widthMap;
        private readonly ITextDecoder textDecoder;

        private InternalFont() : this(
            new PdfDictionary { { Names.BaseFont, StandardFonts.TimesRoman } },
            DefaultFontResolver.Instance)
        {
        }

        public InternalFont(PdfDictionary font, IFontResolver fontResolver)
        {
            if (font == null) throw new ArgumentNullException(nameof(font));
            if (fontResolver == null) throw new ArgumentNullException(nameof(fontResolver));

            if (font.TryGetName(Names.BaseFont, out var name))
            {
                Name = name.Value;
            }

            SubstituteFont = fontResolver.ResolveFont(Name);

            if (font.TryGetDictionary(Names.ToUnicode, out var toUnicode))
            {
                textDecoder = CMapParser.Parse(toUnicode.Stream);
            }
            else if (font.TryGetValue(Names.Encoding, out var encoding))
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

            for (var i = 0; i < value.Length; )
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
