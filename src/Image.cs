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
    /// Provides information about an image in a PDF document.
    /// </summary>
    public abstract class Image
    {
        internal Image(string contentType)
        {
            ContentType = contentType;
        }

        /// <summary>
        /// Gets the IANA media type for the image.
        /// </summary>
        /// <remarks>
        /// Currently the following content types can be used:
        /// <list type="bullet">
        ///     <item>image/png</item>
        ///     <item>image/jpeg</item>
        /// </list>
        /// </remarks>
        public string ContentType { get; }

        /// <summary>
        /// Gets the binary content of the image.
        /// </summary>
        /// <returns>Binary content of the image.</returns>
        public abstract byte[] GetContent();

        /// <summary>
        /// Gets the binary content of the image asynchronously.
        /// </summary>
        /// <returns>Binary content of the image.</returns>
        public abstract Task<byte[]> GetContentAsync();

        /// <summary>
        /// Generates a data URI for this image.
        /// </summary>
        /// <returns>Data URI for this image.</returns>
        public string ToDataUri()
        {
            return "data:" + ContentType + ";base64," + Convert.ToBase64String(GetContent());
        }

        /// <summary>
        /// Generates a data URI for this image asynchronously.
        /// </summary>
        /// <returns>Data URI for this image.</returns>
        public async Task<string> ToDataUriAsync()
        {
            return "data:" + ContentType + ";base64," + Convert.ToBase64String(await GetContentAsync().ConfigureAwait(false));
        }
    }
}
