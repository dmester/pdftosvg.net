// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg
{
    /// <summary>
    /// Represents a font whose glyphs are inlined as paths and other elements within the SVG markup.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <see cref="InlinedFont"/> differs from <see cref="WebFont"/>, where the latter embeds fonts as OpenType or
    ///     WOFF font files in the generated SVG. Side effects from inlining glyphs are that exported text will not be
    ///     selectable, and that the generated SVG will typically be larger than if the font is embedded as a font file.
    /// </para>
    /// <para>
    ///     Currently only PDF fonts of type 3 can be inlined, but more font types might be supported in the future.
    /// </para>
    /// <para>
    ///     PdfToSvg.NET is able to convert most type 3 fonts to OpenType or WOFF, but a subset of type 3 fonts
    ///     cannot be converted, e.g., bitmap fonts. <see cref="InlinedFont"/> is mainly intended for this subset.
    /// </para>
    /// </remarks>
    public class InlinedFont : Font
    {
        /// <summary>
        /// Creates an instance of <see cref="InlinedFont"/>.
        /// </summary>
        /// <param name="font">Source font that should be inlined in the exported SVG.</param>
        /// <exception cref="ArgumentNullException"><paramref name="font"/> was <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="font"/> cannot be inlined. Currently only Type 3 fonts can be inlined.
        ///     Check <see cref="SourceFont.CanBeInlined"/> to determine whether a font can be inlined.
        /// </exception>
        public InlinedFont(SourceFont font)
        {
            if (font == null)
            {
                throw new ArgumentNullException(nameof(font));
            }

            if (!font.CanBeInlined)
            {
                throw new ArgumentException("The specified font cannot be inlined.");
            }

            FontFamily = font.Name ?? "";
            SourceFont = font;
        }

        /// <summary>
        /// Name of the font.
        /// </summary>
        public override string FontFamily { get; }

        internal SourceFont SourceFont { get; }
    }
}
