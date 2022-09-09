// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.CMaps
{
    internal class CMapFile
    {
        public string Name = "";
        public string? UseCMap;

        public uint CodeSpaceRangeOffset;
        public uint CidTablesOffset;
    }
}
