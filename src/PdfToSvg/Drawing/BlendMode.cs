// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Drawing
{
    internal enum BlendMode
    {
        // PDF 1.7 Table 136
        Normal,
        Compatible = Normal,
        Multiply,
        Screen,
        Overlay,
        Darken,
        Lighten,
        ColorDodge,
        ColorBurn,
        HardLight,
        SoftLight,
        Difference,
        Exclusion,

        // PDF 1.7 Table 137
        Hue,
        Saturation,
        Color,
        Luminosity,
    }
}
