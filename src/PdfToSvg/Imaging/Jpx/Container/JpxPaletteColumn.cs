// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;

namespace PdfToSvg.Imaging.Jpx.Container
{
    internal readonly struct JpxPaletteColumn
    {
        public readonly int Precision;
        public readonly bool Signed;
        public int MaxValue => (1 << Precision) - 1;

        public JpxPaletteColumn(int precision, bool signed)
        {
            if (precision > 31)
            {
                // Also validated in JpxBoxParser.ReadPalette
                throw new ArgumentOutOfRangeException(nameof(precision));
            }

            Precision = precision;
            Signed = signed;
        }
    }
}
