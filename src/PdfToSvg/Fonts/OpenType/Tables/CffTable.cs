// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

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

                var data = CompactFontBuilder.Build(Content);
                writer.WriteBytes(data);
            }
        }
    }
}
