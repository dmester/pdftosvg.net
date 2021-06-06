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
    /// Creates data URIs for images.
    /// </summary>
    public class DataUriImageResolver : IImageResolver
    {
        /// <inheritdoc/>
        public string ResolveImageUrl(Image image)
        {
            return image.ToDataUri();
        }
    }
}
