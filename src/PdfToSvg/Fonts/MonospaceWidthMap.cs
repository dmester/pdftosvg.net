// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Fonts
{
    internal class MonospaceWidthMap : WidthMap
    {
        private readonly double charWidth;

        public MonospaceWidthMap(double charWidth)
        {
            this.charWidth = charWidth;
        }

        public override double GetWidth(CharacterCode ch)
        {
            return ch.DestinationString != null ?
                ch.DestinationString.Length * charWidth :
                0;
        }
    }
}
