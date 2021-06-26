// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PdfToSvg.Drawing
{
    internal class BrokenImageSymbol
    {
        private static readonly XNamespace ns = "http://www.w3.org/2000/svg";

        public static XElement Create()
        {
            return new XElement(ns + "symbol",
                new XAttribute("width", "24"),
                new XAttribute("height", "15"),
                new XAttribute("viewBox", "0 0 24 15"),
                new XElement(ns + "path",
                    new XAttribute("d", "m0.5 0.5v14h9.1l1.9-2.95-2.86-4.02 2.9-4.02-2.25-3.02h-8.79zm12.8 0 2.25 3.02-2.9 4.02 2.86 4.02-1.9 2.95h9.9v-14h-10.2z"),
                    new XAttribute("fill", "#fff")
                    ),
                new XElement(ns + "path",
                    new XAttribute("d", "m5.16 4-3.16 6.6v2.4h8.56l0.936-1.45-2.86-4.02 0.859-1.19-0.846-1.23-1.17 1.73-2.32-2.86zm8.34 2.35-0.859 1.19 2.86 4.02-0.936 1.45h7.44v-3.83s-2.92-1.3-5.21-1.01c-0.659 0.0855-1.12 0.316-1.65 0.559l-1.63-2.37z"),
                    new XAttribute("fill", "#d40000")
                    ),
                new XElement(ns + "circle",
                    new XAttribute("cx", "19.8"),
                    new XAttribute("cy", "4.89"),
                    new XAttribute("r", "1.3"),
                    new XAttribute("fill", "#d40000")
                    ),
                new XElement(ns + "path",
                    new XAttribute("d", "m0.5 0.5v14h9.1l1.9-2.95-2.86-4.02 2.9-4.02-2.25-3.02h-8.79zm12.8 0 2.25 3.02-2.9 4.02 2.86 4.02-1.9 2.95h9.9v-14z"),
                    new XAttribute("fill", "none"),
                    new XAttribute("stroke", "#000"),
                    new XAttribute("stroke-width", "0.6")
                    )
                );
        }
    }
}
