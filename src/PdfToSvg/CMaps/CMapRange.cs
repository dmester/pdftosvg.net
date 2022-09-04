// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.CMaps
{
    internal struct CMapRange
    {
        public uint FromCharCode { get; }

        public uint ToCharCode { get; }

        public int CharCodeLength { get; }

        public uint StartValue { get; }

        public bool IsEmpty => CharCodeLength == 0;


        public CMapRange(uint fromCharCode, uint toCharCode, int charCodeLength, uint startValue)
        {
            if (charCodeLength < 1 || charCodeLength > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(charCodeLength));
            }

            FromCharCode = fromCharCode;
            ToCharCode = toCharCode;
            CharCodeLength = charCodeLength;
            StartValue = startValue;
        }

        public override string ToString()
        {
            return
                CMapHelper.FormatCodeRange(FromCharCode, ToCharCode, CharCodeLength) +
                " " +
                StartValue;
        }
    }
}
