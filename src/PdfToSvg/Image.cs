// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
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
    /// <see cref="ImageResolver"/>
    /// <see cref="SvgConversionOptions.ImageResolver">SvgConversionOptions.ImageResolver</see>
    public abstract class Image
    {
        internal Image(PdfDictionary imageDictionary, string contentType, string extension) : this(
            contentType, extension,
            imageDictionary.GetValueOrDefault(Names.Width, 0),
            imageDictionary.GetValueOrDefault(Names.Height, 0))
        { }

        /// <summary>
        /// Creates a new <see cref="Image"/> instance.
        /// </summary>
        /// <param name="contentType">The IANA media type for the image.</param>
        /// <param name="extension">Recommended file name extension for this image. If a leading "." is missing, it will be prepended.</param>
        /// <param name="width">Image width in pixels as specified in the PDF metadata.</param>
        /// <param name="height">Image height in pixels as specified in the PDF metadata.</param>
        /// <exception cref="ArgumentNullException"><paramref name="contentType"/> or <paramref name="extension"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> or <paramref name="height"/> is negative.</exception>
        /// <exception cref="ArgumentException"><paramref name="contentType"/> is empty.</exception>
        protected Image(string contentType, string extension, int width, int height)
        {
            if (contentType == null) throw new ArgumentNullException(nameof(contentType));
            if (contentType.Length == 0) throw new ArgumentException("The content type must not be empty.", nameof(contentType));
            if (extension == null) throw new ArgumentNullException(nameof(extension));
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));

            ContentType = contentType;
            Extension = extension;
            Width = width;
            Height = height;

            if (extension.Length != 0 && !extension.StartsWith(".", StringComparison.Ordinal))
            {
                Extension = "." + Extension;
            }
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
        /// Recommended file name extension (including leading ".") for this image.
        /// </summary>
        public string Extension { get; }

        /// <summary>
        /// Gets the width of the image in pixels as specified in the PDF metadata.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the height of the image in pixels as specified in the PDF metadata.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets the binary content of the image.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the operation.</param>
        /// <returns>Binary content of the image.</returns>
        /// <exception cref="PermissionException">
        ///     Content extraction from this document is forbidden by the document author. 
        ///     Not thrown if the document is opened with the owner password (see <see cref="OpenOptions.Password"/>).
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     The operation was cancelled because the cancellation token was triggered.
        /// </exception>
        public abstract byte[] GetContent(CancellationToken cancellationToken = default);

#if HAVE_ASYNC
        /// <summary>
        /// Gets the binary content of the image asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the operation.</param>
        /// <returns>Binary content of the image.</returns>
        /// <exception cref="PermissionException">
        ///     Content extraction from this document is forbidden by the document author. 
        ///     Not thrown if the document is opened with the owner password (see <see cref="OpenOptions.Password"/>).
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     The operation was cancelled because the cancellation token was triggered.
        /// </exception>
        public abstract Task<byte[]> GetContentAsync(CancellationToken cancellationToken = default);
#endif

        /// <summary>
        /// Generates a data URL for this image.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the operation.</param>
        /// <returns>Data URL for this image.</returns>
        /// <exception cref="PermissionException">
        ///     Content extraction from this document is forbidden by the document author. 
        ///     Not thrown if the document is opened with the owner password (see <see cref="OpenOptions.Password"/>).
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     The operation was cancelled because the cancellation token was triggered.
        /// </exception>
        public string ToDataUrl(CancellationToken cancellationToken = default)
        {
            return "data:" + ContentType + ";base64," + Convert.ToBase64String(GetContent(cancellationToken));
        }

#if HAVE_ASYNC
        /// <summary>
        /// Generates a data URL for this image asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the operation.</param>
        /// <returns>Data URL for this image.</returns>
        /// <exception cref="PermissionException">
        ///     Content extraction from this document is forbidden by the document author. 
        ///     Not thrown if the document is opened with the owner password (see <see cref="OpenOptions.Password"/>).
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     The operation was cancelled because the cancellation token was triggered.
        /// </exception>
        public async Task<string> ToDataUrlAsync(CancellationToken cancellationToken = default)
        {
            return "data:" + ContentType + ";base64," + Convert.ToBase64String(await GetContentAsync(cancellationToken).ConfigureAwait(false));
        }
#endif
    }
}
