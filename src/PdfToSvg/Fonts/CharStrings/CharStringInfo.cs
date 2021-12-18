// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CharStrings
{
    internal class CharStringInfo
    {
        public CharStringPath Path = new CharStringPath();

        public List<CharStringLexeme> Content = new List<CharStringLexeme>();

        public List<CharStringLexeme> ContentInlinedSubrs = new List<CharStringLexeme>();

        public CharStringSeacInfo? Seac;

        public double? Width;

        public int HintCount;
    }
}
