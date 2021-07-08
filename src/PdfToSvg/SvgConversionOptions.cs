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
        /// The default implementation <see cref="DataUriImageResolver"/> embeds all images as data URIs.
        /// You can implement a resolver yourself to e.g. save the images to files and instead include URLs to
        /// the separate image files in the SVG markup.
        /// </remarks>
        public ImageResolver ImageResolver
        {
            get => imageResolver;
            set => imageResolver = value ?? ImageResolver.Default;
        }

        /// <summary>
        /// Gets or sets an implementation that will be used for deciding what font to be used for text included in the SVG.
        /// </summary>
        /// <remarks>
        /// The default implementation <see cref="StandardFontResolver"/> will try to detect standard fonts and assume that the
        /// client have those installed. You can implement a custom font resolver for e.g. embedding fonts as WOFF or WOFF2 files.
        /// </remarks>
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
