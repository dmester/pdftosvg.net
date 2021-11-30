// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType.Utils
{
    internal struct SearchParams
    {
        public SearchParams(int itemCount, int itemSize)
        {
            SearchRange = (ushort)((1 << MathUtils.IntLog2(itemCount)) * itemSize);
            EntrySelector = (ushort)MathUtils.IntLog2(itemCount);
            RangeShift = (ushort)((itemCount * itemSize) - SearchRange);
        }

        public ushort SearchRange { get; }
        public ushort EntrySelector { get; }
        public ushort RangeShift { get; }
    }
}
