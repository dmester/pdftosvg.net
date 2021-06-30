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
    /// Specifies font styles.
    /// </summary>
    public enum FontStyle
    {
        /// <summary>
        /// The normal style of the font.
        /// </summary>
        Normal = 0,

        /// <summary>
        /// Italic version of the font, or the oblique version if no italic version exists.
        /// </summary>
        Italic,

        /// <summary>
        /// Oblique version of the font, or the italic version if no oblique version exists.
        /// </summary>
        Oblique,
    }
}
