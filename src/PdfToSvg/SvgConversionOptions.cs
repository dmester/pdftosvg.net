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
    /// Contains options affecting the behavior when a PDF page is converted to SVG.
    /// </summary>
    public class SvgConversionOptions
    {
        private ImageResolver imageResolver = ImageResolver.Default;
        private FontResolver fontResolver = FontResolver.Default;

        /// <summary>
        /// Gets or sets a class that is used to resolve URLs for images.
        /// </summary>
        /// <remarks>
        /// The default implementation <see cref="ImageResolver.DataUrl"/> embeds all images as data URLs.
        /// You can implement a resolver yourself to e.g. save the images to files and instead include URLs to
        /// the separate image files in the SVG markup.
        /// </remarks>
        public ImageResolver ImageResolver
        {
            get => imageResolver;
            set => imageResolver = value ?? ImageResolver.Default;
        }

        /// <summary>
        /// Gets or sets an implementation that will be used for deciding what font to be used for text included in the
        /// SVG.
        /// </summary>
        /// <remarks>
        /// <para>
        ///     The default implementation will in first hand try to embed fonts as WOFF files, and if not possible,
        ///     fallback to detecting standard fonts and assuming the client has those installed. You can implement a
        ///     custom font resolver for e.g. using custom WOFF or WOFF2 files, or saving the embedded fonts as separate
        ///     font files.
        /// files.
        /// </para>
        /// <para>
        ///     Built-in font resolvers:
        /// </para>
        /// <list type="bullet">
        ///     <item>
        ///         <term><see cref="FontResolver.EmbedOpenType"/></term>
        ///         <description>
        ///             Extracts fonts from the PDF, converts them to OpenType format, and then embed them as data URIs
        ///             in the SVG.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="FontResolver.EmbedWoff"/></term>
        ///         <description>
        ///             Extracts fonts from the PDF, converts them to WOFF format, and then embed them as data URIs in
        ///             the SVG.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="FontResolver.LocalFonts"/></term>
        ///         <description>
        ///             Tries to match the PDF fonts with commonly available fonts, and references them by name.
        ///         </description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <para>
        ///     The following example will convert a PDF to SVG without embedding fonts into the extracted SVG. Instead
        ///     local fonts assumed to be installed on the client machine are used.
        /// </para>
        /// <code lang="cs" title="Using local fonts instead of embedding fonts">
        /// var conversionOptions = new SvgConversionOptions
        /// {
        ///     FontResolver = FontResolver.LocalFonts,
        /// };
        /// 
        /// using (var doc = PdfDocument.Open("input.pdf"))
        /// {
        ///     var pageIndex = 0;
        ///
        ///     foreach (var page in doc.Pages)
        ///     {
        ///         page.SaveAsSvg($"output-{pageIndex++}.svg", conversionOptions);
        ///     }
        /// }
        /// </code>
        /// </example>
        public FontResolver FontResolver
        {
            get => fontResolver;
            set => fontResolver = value ?? FontResolver.Default;
        }

        /// <summary>
        /// Gets or sets the minimum stroke width that will be used in the resulting SVG.
        /// If the PDF use a thinner stroke width, it will be replaced with this width.
        /// </summary>
        public double MinStrokeWidth { get; set; } = 0.5;

        /// <summary>
        /// Spacing between letters below this threshold is assumed to be kerning and removed.
        /// The value is relative to the current font size, where 1.0 represents the font size.
        /// </summary>
        public double KerningThreshold { get; set; } = 0.2;

        /// <summary>
        /// Determines whether web links from the PDF will be included in the generated SVG.
        /// </summary>
        /// <remarks>
        /// Note that other types of links, including links within the document, are currently not supported.
        /// </remarks>
        public bool IncludeLinks { get; set; } = true;
    }
}
