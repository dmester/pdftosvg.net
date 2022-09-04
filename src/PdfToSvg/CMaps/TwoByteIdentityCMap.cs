// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.CMaps
{
    internal class TwoByteIdentityCMap : CMap
    {
        public override uint? GetCid(uint charCode) => charCode;
        public override uint? GetNotDef(uint charCode) => null;

        public override IEnumerable<uint> GetCharCodes(uint cid)
        {
            yield return cid;
        }

        public override CMapCharCode GetCharCode(PdfString str, int offset)
        {
            if (offset + 1 < str.Length)
            {
                return new CMapCharCode((uint)(str[offset] << 8) | str[offset + 1], 2);
            }
            else
            {
                return default;
            }
        }
    }
}
