// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CharStrings
{
    internal class CharString
    {
        private readonly CharStringInfo info;

        static CharString()
        {
            var endchar = CharStringLexeme.Operator(14);

            var info = new CharStringInfo();
            info.Content.Add(endchar);
            info.ContentInlinedSubrs.Add(endchar);

            Empty = new CharString(info);
        }

        public CharString(CharStringInfo info)
        {
            this.info = info;
            this.Width = info.Width;
        }

        public IList<CharStringLexeme> Content => info.Content;
        public IList<CharStringLexeme> ContentInlinedSubrs => info.ContentInlinedSubrs;

        public CharStringSeacInfo? Seac => info.Seac;

        public double? Width { get; set; }

        public double MinX => info.Path.MinX;
        public double MaxX => info.Path.MaxX;
        public double MinY => info.Path.MinY;
        public double MaxY => info.Path.MaxY;

        public double LastX => info.Path.LastX;
        public double LastY => info.Path.LastY;

        public double HintCount => info.HintCount;

        public static CharString Empty { get; }
    }
}
