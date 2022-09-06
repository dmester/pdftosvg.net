// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.CMaps;
using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Fonts.WidthMaps
{
    internal class MonospaceWidthMap : WidthMap
    {
        private readonly double charWidth;

        public MonospaceWidthMap(double charWidth)
        {
            this.charWidth = charWidth;
        }

        public override double GetWidth(CharInfo ch)
        {
            var utf16 = ch.Unicode;
            var charCount = 0;

            for (var i = 0; i < utf16.Length; i++)
            {
                if (i == 0 || !char.IsSurrogatePair(utf16[i - 1], utf16[i]))
                {
                    charCount++;
                }
            }

            return charCount * charWidth;
        }
    }
}
