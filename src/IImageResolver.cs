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
    /// Resolves an image URL for an image encountered in a PDF document.
    /// </summary>
    public interface IImageResolver
    {
        /// <summary>
        /// Creates an URL for the specified image.
        /// </summary>
        /// <param name="image">Found image.</param>
        /// <returns>URL for the specified image.</returns>
        string ResolveImageUrl(Image image);
    }
}
