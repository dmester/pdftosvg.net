// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Encodings;
using PdfToSvg.Fonts.CompactFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType.Tables
{
    internal class CffTable : IBaseTable
    {
        public string Tag => "CFF ";

        public CompactFontSet? Content;

        public void Write(OpenTypeWriter writer)
        {
            if (Content != null)
            {
                CompactFontSanitizer.Sanitize(Content);

                // OTS does not support supplemental codes, so let's skip writing an encoding to the font. The CFF
                // encoding has no meaning in an OpenType font.
                foreach (var font in Content.Fonts)
                {
                    font.Encoding = SingleByteEncoding.Standard;
                }

                var data = CompactFontBuilder.Build(Content);
                writer.WriteBytes(data);
            }
        }
    }
}
