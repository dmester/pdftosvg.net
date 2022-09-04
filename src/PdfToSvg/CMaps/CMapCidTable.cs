// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.CMaps
{
    internal class CMapCidTable
    {
        public CMapCidTableType Type;
        public uint CharCodeLength;
        public uint EntryCount;

        public uint CharCodeOffset;
        public uint CidOffset;
    }
}
