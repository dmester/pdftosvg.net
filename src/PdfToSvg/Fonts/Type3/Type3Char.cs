// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.Type3
{
    internal struct Type3Char
    {
        public double Width { get; }
        public byte[]? GlyphDefinition { get; }

        public Type3Char(double width, byte[]? glyphDefinition)
        {
            Width = width;
            GlyphDefinition = glyphDefinition;
        }
    }
}
