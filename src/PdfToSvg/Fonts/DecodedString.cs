// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts
{
    internal struct DecodedString
    {
        public string Value { get; }
        public int Length { get; }
        public double Width { get; }

        public DecodedString(string value, int length, double width)
        {
            Value = value;
            Length = length;
            Width = width;
        }

        public override string ToString() => Value;
    }
}
