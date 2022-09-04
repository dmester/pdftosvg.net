// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.CMaps
{
    internal struct CMapCharCode : IEquatable<CMapCharCode>
    {
        public uint CharCode { get; }

        public int CharCodeLength { get; }


        public bool IsEmpty => CharCodeLength == 0;

        public static CMapCharCode Empty => default;

        public CMapCharCode(uint charCode, int charCodeLength)
        {
            if (charCodeLength < 1 || charCodeLength > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(charCodeLength));
            }

            CharCode = charCode;
            CharCodeLength = charCodeLength;
        }

        public override int GetHashCode() => (int)CharCode;
        public bool Equals(CMapCharCode other) => other.CharCode == CharCode && other.CharCodeLength == CharCodeLength;
        public override bool Equals(object? obj) => obj is CMapCharCode code && Equals(code);
        public override string ToString() => CMapHelper.FormatCode(CharCode, CharCodeLength);
    }
}
