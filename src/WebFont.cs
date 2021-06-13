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
    // TODO not imlemented. Mark as public when it has been implemented

    /// <summary>
    /// Uses a web font in the resulting SVG markup.
    /// </summary>
    internal class WebFont : Font
    {
        /// <summary>
        /// Creates an instance of a <see cref="WebFont"/>.
        /// </summary>
        /// <param name="fontFamily">Font family name used in SVG file.</param>
        /// <param name="woffUrl">URL to a WOFF font file to be included as a @font-face.</param>
        /// <param name="woff2Url">URL to a WOFF2 font file to be included as a @font-face.</param>
        /// <param name="trueTypeUrl">URL to a TypeType font file to be included as a @font-face.</param>
        /// <remarks>
        /// Note that standalone SVGs must not have external resources. If you intend to create standalone SVG files,
        /// ensure the font URLs are data URIs.
        /// </remarks>
        public WebFont(string fontFamily, string? woffUrl = null, string? woff2Url = null, string? trueTypeUrl = null)
        {
            FontFamily = fontFamily;

            WoffUrl = woffUrl;
            Woff2Url = woff2Url;
            TrueTypeUrl = trueTypeUrl;
        }

        /// <inheritdoc/>
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
    }
}
