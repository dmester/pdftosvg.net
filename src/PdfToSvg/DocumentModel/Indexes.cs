// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.DocumentModel
{
    /// <summary>
    /// Used in <see cref="PdfNamePath"/> for traversing through arrays.
    /// </summary>
    internal static class Indexes
    {
        public static PdfName First { get; } = new PdfName("$$First");

        public static PdfName Last { get; } = new PdfName("$$Last");
    }
}
