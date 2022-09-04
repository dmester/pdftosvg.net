// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.CMaps
{
    internal struct CMapCodeSpaceRange
    {
        public uint FromCharCode { get; }

        public uint ToCharCode { get; }

        public int CharCodeLength { get; }

        public CMapCodeSpaceRange(uint fromCharCode, uint toCharCode, int charCodeLength)
        {
            if (charCodeLength < 1 || charCodeLength > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(charCodeLength));
            }

            FromCharCode = fromCharCode;
            ToCharCode = toCharCode;
            CharCodeLength = charCodeLength;
        }

        public override string ToString()
        {
            return CMapHelper.FormatCodeRange(FromCharCode, ToCharCode, CharCodeLength);
        }
    }
}
