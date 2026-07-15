// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.IO;
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
        /// Saves the image to a file.
        /// </summary>
        /// <param name="path">
        ///     Path to the output file. If the file already exists it is overwritten.
        /// </param>
        /// <param name="cancellationToken">
        ///     Cancellation token that can be used to cancel the operation.
        /// </param>
        ///
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="path"/> was <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="path"/> was empty or an invalid path.
        /// </exception>
        /// <exception cref="PathTooLongException">
        ///     The length of <paramref name="path"/> exceeded the system maximum length.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        ///     The directory of <paramref name="path"/> did not exist.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        ///     The user did not have permission to write to <paramref name="path"/>.
        /// </exception>
        /// <exception cref="IOException">
        ///     I/O exception while writing to <paramref name="path"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///     The <paramref name="path"/> was specified on an unsupported format.
        /// </exception>
        /// <inheritdoc cref="GetContent(CancellationToken)" path="/exception"/>
        /// <example>
        /// <para>
        ///     The following example exports images from all pages in the PDF document to image files.
        /// </para>
        /// <code lang="cs">
        /// using (var document = PdfDocument.Open("input.pdf"))
        /// {
        ///     var imageNo = 1;
        ///
        ///     foreach (var image in document.Images)
        ///     {
        ///         var fileName = $"image{imageNo++}{image.Extension}";
        ///         image.Save(fileName);
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// <note type="note">
        ///     The image format can vary between <see cref="Image"/> instances. An appropriate filename extension for
        ///     this particular <see cref="Image"/> can be found in the <see cref="Extension"/> property.
        /// </note>
        /// </remarks>
        /// <seealso cref="Extension">Extension Property</seealso>
        public virtual void Save(string path, CancellationToken cancellationToken = default)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (path.Length == 0)
            {
                throw new ArgumentException("The path must not be empty.", nameof(path));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var content = GetContent(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            File.WriteAllBytes(path, content);
        }

        /// <summary>
        /// Saves the image to a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">
        ///     The stream to which the image will be saved. The stream must be writable, but does not need to be readable or seekable.
        /// </param>
        /// <param name="cancellationToken">
        ///     Cancellation token that can be used to cancel the operation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="stream"/> was <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="stream"/> was not writable.
        /// </exception>
        /// <inheritdoc cref="GetContent(CancellationToken)" path="/exception"/>
        /// <remarks>
        /// <note type="note">The image format can vary between <see cref="Image"/> instances. The format of this particular <see cref="Image"/> can be identified by the <see cref="ContentType"/> property.</note>
        /// </remarks>
        /// <seealso cref="ContentType">ContentType Property</seealso>
        public virtual void Save(Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            if (!stream.CanWrite)
            {
                throw new ArgumentException("The stream must be writable.", nameof(stream));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var content = GetContent(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            stream.Write(content, 0, content.Length);
        }

#if HAVE_ASYNC
        /// <summary>
        /// Saves the image to a file asynchronously.
        /// </summary>
        /// <inheritdoc cref="Save(string, CancellationToken)"/>
        /// <example>
        /// <para>
        ///     The following example exports images from all pages in the PDF document to image files asynchronously.
        /// </para>
        /// <code lang="cs">
        /// using (var document = await PdfDocument.OpenAsync("input.pdf"))
        /// {
        ///     var imageNo = 1;
        ///
        ///     await foreach (var image in document.Images)
        ///     {
        ///         var fileName = $"image{imageNo++}{image.Extension}";
        ///         await image.SaveAsync(fileName);
        ///     }
        /// }
        /// </code>
        /// </example>
        public virtual async Task SaveAsync(string path, CancellationToken cancellationToken = default)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (path.Length == 0)
            {
                throw new ArgumentException("The path must not be empty.", nameof(path));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var content = await GetContentAsync(cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            using (var stream = File.Create(path))
            {
                await stream.WriteAsync(content, 0, content.Length, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Saves the image to a <see cref="Stream"/> asynchronously.
        /// </summary>
        /// <inheritdoc cref="Save(Stream, CancellationToken)"/>
        public virtual async Task SaveAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            if (!stream.CanWrite)
            {
                throw new ArgumentException("The stream must be writable.", nameof(stream));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var content = await GetContentAsync(cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            await stream.WriteAsync(content, 0, content.Length, cancellationToken).ConfigureAwait(false);
        }
#endif

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
        /// <remarks>
        /// <note type="note">The image format can vary between <see cref="Image"/> instances. The format of this particular <see cref="Image"/> can be identified by the <see cref="ContentType"/> property.</note>
        /// </remarks>
        /// <seealso cref="ContentType">ContentType Property</seealso>
        public abstract byte[] GetContent(CancellationToken cancellationToken = default);

#if HAVE_ASYNC
        /// <summary>
        /// Gets the binary content of the image asynchronously.
        /// </summary>
        /// <inheritdoc cref="GetContent(CancellationToken)"/>
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

        internal virtual void Initialize(CancellationToken cancellationToken = default)
        {
        }

#if HAVE_ASYNC
        internal virtual Task InitializeAsync(CancellationToken cancellationToken = default)
        {
#if NET45
            return Task.FromResult(true);
#else
            return Task.CompletedTask;
#endif
        }
#endif
    }
}
