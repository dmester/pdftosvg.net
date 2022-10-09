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
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the operation.</param>
        /// <returns>Binary content of the image.</returns>
        public abstract byte[] GetContent(CancellationToken cancellationToken);

#if HAVE_ASYNC
        /// <summary>
        /// Gets the binary content of the image asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the operation.</param>
        /// <returns>Binary content of the image.</returns>
        public abstract Task<byte[]> GetContentAsync(CancellationToken cancellationToken);
#endif

        /// <summary>
        /// Generates a data URL for this image.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the operation.</param>
        /// <returns>Data URL for this image.</returns>
        public string ToDataUrl(CancellationToken cancellationToken)
        {
            return "data:" + ContentType + ";base64," + Convert.ToBase64String(GetContent(cancellationToken));
        }

#if HAVE_ASYNC
        /// <summary>
        /// Generates a data URL for this image asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the operation.</param>
        /// <returns>Data URL for this image.</returns>
        public async Task<string> ToDataUrlAsync(CancellationToken cancellationToken)
        {
            return "data:" + ContentType + ";base64," + Convert.ToBase64String(await GetContentAsync(cancellationToken).ConfigureAwait(false));
        }
#endif
    }
}
