// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CompactFonts
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class CompactFontDictOperatorAttribute : Attribute
    {
        public CompactFontDictOperatorAttribute(int value)
        {
            Value = value;
        }

        public CompactFontDictOperatorAttribute(int value1, int value2)
        {
            Value = (value1 << 8) | value2;
        }

        public int Value { get; }
    }
}
