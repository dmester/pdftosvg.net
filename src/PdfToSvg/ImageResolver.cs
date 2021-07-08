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
    public abstract class ImageResolver
    {
        /// <summary>
        /// Gets the default image resolver used when no resolver is explicitly specified.
        /// </summary>
        public static ImageResolver Default { get; } = new DataUriImageResolver();

        /// <summary>
        /// Creates an URL for the specified image.
        /// </summary>
        /// <param name="image">Found image.</param>
        /// <param name="cancellationToken">Token for monitoring cancellation requests.</param>
        /// <returns>URL for the specified image.</returns>
        public abstract string ResolveImageUrl(Image image, CancellationToken cancellationToken);
    }
}
