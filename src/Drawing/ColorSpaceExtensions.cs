using PdfToSvg.ColorSpaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    internal static class ColorSpaceExtensions
    {
        public static RgbColor GetDefaultRgbColor(this ColorSpace colorSpace) => new RgbColor(colorSpace, colorSpace.DefaultColor);
    }
}
