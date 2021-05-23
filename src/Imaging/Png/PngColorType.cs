using System;
using System.Collections.Generic;
using System.Text;

namespace PdfToSvg.Imaging.Png
{
    internal enum PngColorType : byte
    {
        Greyscale = 0,
        Truecolour = 2,
        IndexedColour = 3,
        GreyscaleWithAlpha = 4,
        TruecolourWithAlpha = 6,
    }
}
