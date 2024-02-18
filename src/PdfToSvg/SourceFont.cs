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
        /// Indicates whether this font is one of the 14 standard PDF fonts. If so, the font returned by
        /// <see cref="ToOpenType"/> and <see cref="ToWoff"/> is not extracted from the PDF, but rather provided by
        /// PdfToSvg.NET.
        /// </summary>
        /// <remarks>
        /// The standard fonts used by PdfToSvg.NET were originally developed by Foxit for Pdfium and are subject to the
        /// <see href="https://github.com/dmester/pdftosvg.net/blob/41f9ccc52790966fd6c77da1685b5938f5fe13a4/third-party/StandardFonts/LICENSE">
        /// BSD-3 license</see>. PdfToSvg.NET embeds the license in the extracted font file. You agree to follow the
        /// license when distributing the resulting SVG file if it contains one of the standard fonts.
        /// </remarks>
        /// <example>
        /// <para>
        ///     In the following example, local fonts will be used instead of the standard fonts provided by
        ///     PdfToSvg.NET.
        /// </para>
        /// <code language="cs" title="Use local standard fonts">
        /// public class LocalStandardFontsFontResolver : FontResolver
        /// {
        ///     public override Font ResolveFont(SourceFont sourceFont, CancellationToken cancellationToken)
        ///     {
        ///         var resolver = sourceFont.IsStandardFont ? FontResolver.LocalFonts : FontResolver.EmbedWoff;
        ///         return resolver.ResolveFont(sourceFont, cancellationToken);
        ///     }
        /// }
        /// </code>
        /// <code language="cs" title="Using font resolver">
        /// using (var doc = PdfDocument.Open("input.pdf"))
        /// {
        ///     var pageIndex = 0;
        /// 
        ///     foreach (var page in doc.Pages)
        ///     {
        ///         page.SaveAsSvg($"output-{pageIndex++}.svg", new SvgConversionOptions
        ///         {
        ///             FontResolver = new LocalStandardFontsFontResolver(),
        ///         });
        ///     }
        /// }
        /// </code>
        /// </example>
        public virtual bool IsStandardFont => false;

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
