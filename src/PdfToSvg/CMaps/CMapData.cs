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
    internal class CMapData
    {
        public string? Name { get; set; }

        public string? UseCMap { get; set; }

        public bool IsUnicodeCMap { get; set; }

        public List<CMapCodeSpaceRange> CodeSpaceRanges { get; } = new();

        public List<CMapChar> NotDefChars { get; } = new();

        public List<CMapRange> NotDefRanges { get; } = new();

        public List<CMapChar> BfChars { get; } = new();

        public List<CMapRange> BfRanges { get; } = new();

        public List<CMapChar> CidChars { get; } = new();

        public List<CMapRange> CidRanges { get; } = new();
    }
}
