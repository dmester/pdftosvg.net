// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg
{
    /// <summary>
    /// Uses a web font in the resulting SVG markup.
    /// </summary>
    public class WebFont : Font
    {
        /// <summary>
        /// Creates an instance of a <see cref="WebFont"/>.
        /// </summary>
        /// <param name="fallbackFont">Fallback font to be used if the font files cannot be loaded.</param>
        /// <param name="woffUrl">URL to a WOFF font file to be included as a @font-face.</param>
        /// <param name="woff2Url">URL to a WOFF2 font file to be included as a @font-face.</param>
        /// <param name="trueTypeUrl">URL to a TypeType font file to be included as a @font-face.</param>
        /// <remarks>
        /// Note that standalone SVGs must not have external resources. If you intend to create standalone SVG files,
        /// ensure the font URLs are data URIs.
        /// </remarks>
        public WebFont(LocalFont? fallbackFont = null,
            string? woffUrl = null, string? woff2Url = null, string? trueTypeUrl = null)
        {
            if (woffUrl == null && woff2Url == null && trueTypeUrl == null)
            {
                throw new ArgumentException("At least one URL must be specified.");
            }

            FontFamily = StableID.Generate("f", woffUrl, woff2Url, trueTypeUrl);
            FallbackFont = fallbackFont;
            WoffUrl = woffUrl;
            Woff2Url = woff2Url;
            TrueTypeUrl = trueTypeUrl;
        }

        /// <summary>
        /// Gets the fallback font that is used to render the text if the web font files cannot be downloaded.
        /// </summary>
        public LocalFont? FallbackFont { get; }

        /// <summary>
        /// Gets the generated font name of the web font.
        /// </summary>
        public override string FontFamily { get; }

        /// <summary>
        /// Gets an URL to a WOFF font file to be included as a @font-face.
        /// </summary>
        public string? WoffUrl { get; }

        /// <summary>
        /// Gets an URL to a WOFF2 font file to be included as a @font-face.
        /// </summary>
        public string? Woff2Url { get; }

        /// <summary>
        /// Gets an URL to a TrueType font file to be included as a @font-face.
        /// </summary>
        public string? TrueTypeUrl { get; }

        /// <summary>
        /// Determines whether the specified font is equal to the current one.
        /// </summary>
        public override bool Equals(object obj)
        {
            return
                obj is WebFont font &&
                font.FontFamily == FontFamily &&
                font.WoffUrl == WoffUrl &&
                font.Woff2Url == Woff2Url &&
                font.TrueTypeUrl == TrueTypeUrl &&
                Equals(font.FallbackFont, FallbackFont);
        }

        /// <summary>
        /// Gets a hash code for this font.
        /// </summary>
        public override int GetHashCode()
        {
            return FontFamily.GetHashCode();
        }

        /// <summary>
        /// Gets a string representation of this font.
        /// </summary>
        public override string ToString()
        {
            return FontFamily;
        }
    }
}
