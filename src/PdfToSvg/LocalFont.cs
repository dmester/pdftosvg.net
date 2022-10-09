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
        /// <param name="fontFamily">
        /// Font family name used in SVG file. Can be a list of font family names separated by comma.
        /// The last font family is recommended to be a generic font family, e.g. <c>sans-serif</c>.
        /// </param>
        /// <param name="fontWeight">Optional CSS font weight value.</param>
        /// <param name="fontStyle">Optional CSS font style value.</param>
        public LocalFont(string fontFamily, FontWeight fontWeight = FontWeight.Normal, FontStyle fontStyle = FontStyle.Normal)
        {
            FontFamily = fontFamily?.Trim() ?? throw new ArgumentNullException(nameof(fontFamily));
            FontWeight = fontWeight;
            FontStyle = fontStyle;

            if (FontFamily == "")
            {
                throw new ArgumentException("The font family must not be an empty string.", nameof(fontFamily));
            }
        }

        /// <inheritdoc/>
        public override string FontFamily { get; }

        /// <summary>
        /// Gets the CSS font weight to use.
        /// </summary>
        public FontWeight FontWeight { get; }

        /// <summary>
        /// Gets the CSS font style to use.
        /// </summary>
        public FontStyle FontStyle { get; }

        /// <summary>
        /// Determines whether the specified font is equal to the current one.
        /// </summary>
        /// <param name="obj">The object to be compared with this object.</param>
        public override bool Equals(object obj)
        {
            return
                obj is LocalFont font &&
                font.FontFamily == FontFamily &&
                font.FontWeight == FontWeight &&
                font.FontStyle == FontStyle;
        }

        /// <summary>
        /// Gets a hash code for this font.
        /// </summary>
        public override int GetHashCode()
        {
            return FontFamily.GetHashCode() ^ ((int)FontWeight * 6047) ^ ((int)FontStyle * 7723);
        }

        /// <summary>
        /// Gets a string representation of this font.
        /// </summary>
        public override string ToString()
        {
            return FontFamily + " " + FontWeight + " " + FontStyle;
        }
    }
}
