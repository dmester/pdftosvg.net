// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts
{
    internal class CharInfo
    {
        public const string NotDef = "\ufffd";

        public uint CharCode;

        public uint? Cid;

        public string? GlyphName;

        public uint? GlyphIndex;

        public string Unicode = NotDef;

        public double Width;
        public bool IsExplicitlyMapped;

        public CharInfo Clone() => (CharInfo)MemberwiseClone();

        public override string ToString()
        {
            var result = CharCode.ToString("x4") + " => ";

            if (Cid.HasValue)
            {
                result += Cid.Value.ToString("x4") + " => ";
            }

            return result + "'" + Unicode + "'";
        }
    }
}
