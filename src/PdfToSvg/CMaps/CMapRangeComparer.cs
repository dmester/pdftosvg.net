// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.CMaps
{
    internal class CMapRangeComparer : IComparer<CMapRange>
    {
        public static readonly CMapRangeComparer Instance = new();

        public int Compare(CMapRange a, CMapRange b)
        {
            if (a.ToCharCode < b.FromCharCode) return -1;
            if (a.FromCharCode > b.ToCharCode) return 1;
            return 0;
        }
    }
}
