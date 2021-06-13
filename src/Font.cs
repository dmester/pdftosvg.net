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
    /// Represents a substitute font to be used in the generated SVG markup.
    /// </summary>
    public abstract class Font
    {
        // Don't allow external implementations
        internal Font() { }

        /// <summary>
        /// Gets the font family name to be used in the SVG markup. Multiple font families
        /// can be separated by comma.
        /// </summary>
        public abstract string FontFamily { get; }

        /// <summary>
        /// Returns the hash code for this font.
        /// </summary>
        public override int GetHashCode() => FontFamily?.GetHashCode() ?? 0;

        /// <summary>
        /// Returns a string representation of this font.
        /// </summary>
        public override string ToString() => FontFamily ?? "";
    }
}
