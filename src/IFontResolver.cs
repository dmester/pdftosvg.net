// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg
{
    /// <summary>
    /// Resolves which font to be used for text in the SVG, for a given PDF font name.
    /// </summary>
    public interface IFontResolver
    {
        /// <summary>
        /// Resolves which font to be used for text in the SVG, for a given PDF font name.
        /// </summary>
        /// <param name="fontName">Font name used in PDF file.</param>
        /// <returns>
        /// The font to be used in the resulting SVG markup.
        /// Can be a <see cref="LocalFont"/> or <see cref="WebFont"/>.
        /// </returns>
        Font ResolveFont(string fontName);
    }
}
