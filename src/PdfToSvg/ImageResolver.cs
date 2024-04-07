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
    /// Resolves an image URL for an image encountered in a PDF document.
    /// </summary>
    /// <see cref="SvgConversionOptions.ImageResolver"/>
    public abstract class ImageResolver
    {
        /// <summary>
        /// Gets an image resolver that will embed images as data URLs in the resulting SVG.
        /// </summary>
        public static ImageResolver DataUrl { get; } = new DataUrlImageResolver();

        /// <summary>
        /// Gets the default image resolver used when no resolver is explicitly specified.
        /// Currently <see cref="DataUrl"/> is the default font resolver, but this can change in the future.
        /// </summary>
        public static ImageResolver Default { get; } = DataUrl;

        /// <summary>
        /// Creates an URL for the specified image.
        /// </summary>
        /// <param name="image">Found image.</param>
        /// <param name="cancellationToken">Token for monitoring cancellation requests.</param>
        /// <returns>URL for the specified image.</returns>
        /// <exception cref="OperationCanceledException">The operation was cancelled because the cancellation token was triggered.</exception>
        public abstract string ResolveImageUrl(Image image, CancellationToken cancellationToken);
    }
}
