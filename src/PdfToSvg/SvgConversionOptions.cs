// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        /// <list type="table">
        ///     <listheader>
        ///         <term>Resolver</term>
        ///         <description>Description</description>
        ///     </listheader>
        ///     <item>
        ///         <term><see cref="FontResolver.EmbedOpenType"/></term>
        ///         <description>
        ///             Extracts fonts from the PDF, converts them to OpenType format, and then embed them as data URLs
        ///             in the SVG.
        ///         </description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="FontResolver.EmbedWoff"/></term>
        ///         <description>
        ///             Extracts fonts from the PDF, converts them to WOFF format, and then embed them as data URLs in
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
        /// <inheritdoc cref="PdfToSvg.FontResolver" path="example"/>
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

        /// <exclude />
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete(
            "Use " + nameof(SvgConversionOptions) + "." + nameof(CollapseSpaceLocalFont) + " " +
            "and " + nameof(SvgConversionOptions) + "." + nameof(CollapseSpaceEmbeddedFont) + " instead.")]
        public double KerningThreshold
        {
            get => CollapseSpaceLocalFont;
            set
            {
                CollapseSpaceLocalFont = value;
                CollapseSpaceEmbeddedFont = value;
            }
        }

        /// <summary>
        /// Explicit spacing between letters below this threshold will be removed and merged to a single text span when
        /// using a local font.
        /// </summary>
        /// <remarks>
        /// The value is relative to the current font size, where 1.0 represents the font size.
        /// This property affects text using a local font. A high value produces long text spans. This is better for
        /// local fonts, where the actual font metrics might not match the metrics of the original font.
        /// </remarks>
        public double CollapseSpaceLocalFont { get; set; } = 0.2;

        /// <summary>
        /// Explicit spacing between letters below this threshold will be removed and merged to a single text span when
        /// using an embedded font.
        /// </summary>
        /// <remarks>
        /// The value is relative to the current font size, where 1.0 represents the font size.
        /// This property affects text formatted with an embedded font. Text inlined as paths is not affected. A low
        /// value produces a more accurate result, while a high value produces a more compact SVG markup.
        /// </remarks>
        public double CollapseSpaceEmbeddedFont { get; set; } = 0.02;

        /// <summary>
        /// Determines whether web links from the PDF will be included in the generated SVG.
        /// </summary>
        /// <remarks>
        /// Note that other types of links, including links within the document, are currently not supported.
        /// </remarks>
        public bool IncludeLinks { get; set; } = true;

#if DEBUG
        /// <summary>
        /// If <c>true</c>, the content stream operators are logged in the output SVG.
        /// </summary>
        public bool DebugLogOperators { get; set; }
#endif
    }
}
