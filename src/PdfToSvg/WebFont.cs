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
    /// <remarks>
    /// <para>
    ///     Objects of this type can be returned from a <see cref="FontResolver"/> to indicate how text will be rendered
    ///     in the generated SVG.
    /// </para>
    /// <note>
    ///     Standalone SVG’s must not reference external resources. If you intend to create standalone SVG files,
    ///     ensure the font URLs are
    ///     <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Basics_of_HTTP/Data_URLs">data URLs</see>.
    /// </note>
    /// <h2>Text representation</h2>
    /// <para>
    ///     PDF documents store text encoded as character codes, and provide mappings from character codes to font
    ///     glyphs and Unicode characters. Some documents map multiple character codes to the same Unicode
    ///     character, giving PdfToSvg.NET a choice. Exporting both character codes as the same Unicode character
    ///     results in good text representation but potentially visually inaccurate SVG’s. Remapping one of
    ///     the character codes to another Unicode character ensures visually accurate SVG’s at the cost of inaccurate
    ///     text representation if text is exported from the SVG.
    /// </para>
    /// <para>
    ///     When exporting text using a <see cref="WebFont"/>, the library will remap duplicate character codes to 
    ///     characters in the 
    ///     <see href="https://en.wikipedia.org/wiki/Private_Use_Areas">Private Use Areas</see>, making sure the
    ///     exported SVG’s are visually accurate, but text might appear as a series of question marks, <c>������</c>,
    ///     in the SVG markup. If you intend to extract text from the SVG, consider exporting the SVG using 
    ///     <see cref="FontResolver.LocalFonts">local fonts</see> instead.
    /// </para>
    /// </remarks>
    /// <seealso cref="FontResolver"/>
    public class WebFont : Font
    {
        /// <summary>
        /// Creates an instance of a <see cref="WebFont"/>.
        /// </summary>
        /// <param name="fallbackFont">Fallback font to be used if the font files cannot be loaded.</param>
        /// <param name="woffUrl">URL to a WOFF font file to be included as a @font-face.</param>
        /// <param name="woff2Url">URL to a WOFF2 font file to be included as a @font-face.</param>
        /// <param name="trueTypeUrl">URL to a TypeType font file to be included as a @font-face.</param>
        /// <param name="openTypeUrl">URL to a OpenType font file to be included as a @font-face.</param>
        /// <remarks>
        /// <note>
        ///     Standalone SVG’s must not reference external resources. If you intend to create standalone SVG files,
        ///     ensure the font URLs are
        ///     <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Basics_of_HTTP/Data_URLs">data URLs</see>.
        /// </note>
        /// </remarks>
        /// <exception cref="ArgumentException">None of the url parameters were specified.</exception>
        public WebFont(LocalFont? fallbackFont = null,
            string? woffUrl = null, string? woff2Url = null, string? trueTypeUrl = null, string? openTypeUrl = null)
        {
            if (woffUrl == null && woff2Url == null && trueTypeUrl == null && openTypeUrl == null)
            {
                throw new ArgumentException("At least one URL must be specified.");
            }

            FontFamily = StableID.Generate("f", woffUrl, woff2Url, trueTypeUrl, openTypeUrl);
            FallbackFont = fallbackFont;
            WoffUrl = woffUrl;
            Woff2Url = woff2Url;
            TrueTypeUrl = trueTypeUrl;
            OpenTypeUrl = openTypeUrl;
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
        /// Gets an URL to an OpenType font file to be included as a @font-face.
        /// </summary>
        public string? OpenTypeUrl { get; }

        /// <summary>
        /// Determines whether the specified font is equal to the current one.
        /// </summary>
        /// <param name="obj">The object to be compared with this object.</param>
        public override bool Equals(object? obj)
        {
            return
                obj is WebFont font &&
                font.FontFamily == FontFamily &&
                font.WoffUrl == WoffUrl &&
                font.Woff2Url == Woff2Url &&
                font.TrueTypeUrl == TrueTypeUrl &&
                font.OpenTypeUrl == OpenTypeUrl &&
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
