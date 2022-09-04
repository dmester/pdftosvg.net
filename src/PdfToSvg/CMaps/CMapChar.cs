// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.CMaps
{
    internal struct CMapChar
    {
        public uint CharCode { get; }

        public int CharCodeLength { get; }

        public uint Cid { get; }

        public string? Unicode { get; }

        public bool IsEmpty => CharCodeLength == 0;

        public CMapChar(uint charCode, int charCodeLength, uint cid) : this(charCode, charCodeLength, cid, null)
        {
        }

        public CMapChar(uint charCode, int charCodeLength, string unicode) : this(charCode, charCodeLength, 0, unicode)
        {
        }

        private CMapChar(uint charCode, int charCodeLength, uint cid, string? unicode)
        {
            if (charCodeLength < 1 || charCodeLength > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(charCodeLength));
            }

            CharCode = charCode;
            CharCodeLength = charCodeLength;
            Cid = cid;
            Unicode = unicode;
        }

        public override string ToString()
        {
            return
                CMapHelper.FormatCode(CharCode, CharCodeLength) +
                " " +
                (CMapHelper.FormatUnicode(Unicode) ?? Cid.ToString());
        }
    }
}
