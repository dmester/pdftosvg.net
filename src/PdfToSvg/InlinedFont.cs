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
    /// <para>
    ///     Inlines the glyphs from the font as paths and other elements within the SVG. This differs from web fonts,
    ///     which are embedded as font files in the SVG. The inlined glyphs cannot be selected as text in the SVG.
    ///     Currently only Type 3 fonts can be inlined, but more font types might be supported in the future.
    /// </para>
    /// <para>
    ///     This font type is mainly intended for Type 3 fonts that cannot be represented as OpenType fonts, e.g.
    ///     bitmap fonts.
    /// </para>
    /// </summary>
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
