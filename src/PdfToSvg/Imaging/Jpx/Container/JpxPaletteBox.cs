// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;

namespace PdfToSvg.Imaging.Jpx.Container
{
    /// <summary>
    /// <c>pclr</c> box
    /// </summary>
    internal class JpxPaletteBox
    {
        public JpxPaletteColumn[] Columns = [];
        public int[,] Values = new int[0, 0];

        public int EntryCount => Values.GetLength(0);
        public int ColumnCount => Columns.Length;
    }
}
