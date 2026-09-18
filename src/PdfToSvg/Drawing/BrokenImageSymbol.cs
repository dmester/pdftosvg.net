// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Svg;

namespace PdfToSvg.Drawing
{
    internal class BrokenImageSymbol
    {
        public static SvgElement Create()
        {
            var symbol = new SvgElement("symbol");
            symbol.AddAttribute("width", "24", isRaw: true);
            symbol.AddAttribute("height", "15", isRaw: true);
            symbol.AddAttribute("viewBox", "0 0 24 15", isRaw: true);

            var whitePath = new SvgElement("path");
            whitePath.AddAttribute("d", "m0.5 0.5v14h9.1l1.9-2.95-2.86-4.02 2.9-4.02-2.25-3.02h-8.79zm12.8 0 2.25 3.02-2.9 4.02 2.86 4.02-1.9 2.95h9.9v-14h-10.2z", isRaw: true);
            whitePath.AddAttribute("fill", "#fff", isRaw: true);
            symbol.AddChild(whitePath);

            var redPath = new SvgElement("path");
            redPath.AddAttribute("d", "m5.16 4-3.16 6.6v2.4h8.56l0.936-1.45-2.86-4.02 0.859-1.19-0.846-1.23-1.17 1.73-2.32-2.86zm8.34 2.35-0.859 1.19 2.86 4.02-0.936 1.45h7.44v-3.83s-2.92-1.3-5.21-1.01c-0.659 0.0855-1.12 0.316-1.65 0.559l-1.63-2.37z", isRaw: true);
            redPath.AddAttribute("fill", "#d40000", isRaw: true);
            symbol.AddChild(redPath);

            var circle = new SvgElement("circle");
            circle.AddAttribute("cx", "19.8", isRaw: true);
            circle.AddAttribute("cy", "4.89", isRaw: true);
            circle.AddAttribute("r", "1.3", isRaw: true);
            circle.AddAttribute("fill", "#d40000", isRaw: true);
            symbol.AddChild(circle);

            var outlinePath = new SvgElement("path");
            outlinePath.AddAttribute("d", "m0.5 0.5v14h9.1l1.9-2.95-2.86-4.02 2.9-4.02-2.25-3.02h-8.79zm12.8 0 2.25 3.02-2.9 4.02 2.86 4.02-1.9 2.95h9.9v-14z", isRaw: true);
            outlinePath.AddAttribute("fill", "none", isRaw: true);
            outlinePath.AddAttribute("stroke", "#000", isRaw: true);
            outlinePath.AddAttribute("stroke-width", "0.6", isRaw: true);
            symbol.AddChild(outlinePath);

            return symbol;
        }
    }
}
