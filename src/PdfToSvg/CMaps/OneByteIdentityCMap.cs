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
    internal class OneByteIdentityCMap : CMap
    {
        public override uint? GetCid(uint charCode) => charCode;
        public override uint? GetNotDef(uint charCode) => null;

        public override IEnumerable<uint> GetCharCodes(uint cid)
        {
            yield return cid;
        }

        public override CMapCharCode GetCharCode(PdfString str, int offset)
        {
            return offset < str.Length ? new CMapCharCode(str[offset], 1) : default;
        }
    }
}
