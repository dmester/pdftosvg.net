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
    /// Contains information about a font used in the PDF document.
    /// </summary>
    public abstract class SourceFont
    {
        /// <summary>
        /// The name of the font.
        /// </summary>
        public abstract string? Name { get; }

        /// <summary>
        /// Specifies whether this font can be inlined. When a font is inlined, its glyphs will be embedded as paths
        /// and other elements in the SVG markup.
        /// </summary>
        public virtual bool CanBeInlined => false;

        /// <summary>
        /// Specifies whether this font can be extracted to a font file that can be embedded in the SVG.
        /// </summary>
        public virtual bool CanBeExtracted => false;

        /// <summary>
        /// Tries to convert this source font to an OpenType (.otf) font.
        /// </summary>
        /// <returns>Binary content of the OpenType font.</returns>
        /// <exception cref="NotSupportedException">This font cannot be converted to an OpenType font.</exception>
        /// <exception cref="PdfException">The conversion failed.</exception>
        public abstract byte[] ToOpenType();

        /// <summary>
        /// Tries to convert this source font to a WOFF font.
        /// </summary>
        /// <returns>Binary content of the WOFF font.</returns>
        /// <exception cref="NotSupportedException">This font cannot be converted to a WOFF font.</exception>
        /// <exception cref="PdfException">The conversion failed.</exception>
        public abstract byte[] ToWoff();
    }
}
