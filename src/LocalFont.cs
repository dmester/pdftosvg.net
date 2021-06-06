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
    /// Represents a font that is assumed to be installed on the machine viewing the SVG.
    /// </summary>
    public class LocalFont : Font
    {
        /// <summary>
        /// Creates a new instance of <see cref="LocalFont"/>.
        /// </summary>
        /// <param name="fontFamily">Font family name used in SVG file. Can be a list of font family names separated by comma.</param>
        /// <param name="fontWeight">Optional CSS font weight value.</param>
        /// <param name="fontStyle">Optional CSS font style value.</param>
        public LocalFont(string fontFamily, string? fontWeight = null, string? fontStyle = null)
        {
            FontFamily = fontFamily;
            FontWeight = fontWeight;
            FontStyle = fontStyle;
        }

        /// <inheritdoc/>
        public override string FontFamily { get; }

        /// <summary>
        /// Gets the CSS font weight to use.
        /// </summary>
        public string? FontWeight { get; }

        /// <summary>
        /// Gets the CSS font style to use.
        /// </summary>
        public string? FontStyle { get; }
    }
}
