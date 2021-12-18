// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CharStrings
{
    internal class CharStringSeacInfo
    {
        public CharStringSeacInfo(double adx, double ady, int bchar, int achar)
        {
            Adx = adx;
            Ady = ady;
            Bchar = bchar;
            Achar = achar;
        }

        public double Adx { get; }
        public double Ady { get; }
        public int Bchar { get; }
        public int Achar { get; }
    }
}
