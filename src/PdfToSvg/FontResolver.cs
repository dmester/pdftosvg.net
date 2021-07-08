// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg
{
    /// <summary>
    /// Resolves which font to be used for text in the SVG, for a given PDF font name.
    /// </summary>
    public abstract class FontResolver
    {
        /// <summary>
        /// Gets the default font resolver used when no resolver is explicitly specified.
        /// </summary>
        public static FontResolver Default { get; } = new StandardFontResolver();

        /// <summary>
        /// Resolves which font to be used for text in the SVG, for a given PDF font name.
        /// </summary>
        /// <param name="fontName">Font name used in PDF file.</param>
        /// <param name="cancellationToken">Token for monitoring cancellation requests.</param>
        /// <returns>
        /// The font to be used in the resulting SVG markup.
        /// Can be a <see cref="LocalFont"/> or <see cref="WebFont"/>.
        /// </returns>
        public abstract Font ResolveFont(string fontName, CancellationToken cancellationToken);
    }
}
