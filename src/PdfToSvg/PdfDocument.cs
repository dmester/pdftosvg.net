﻿// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing;
using PdfToSvg.IO;
using PdfToSvg.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PdfToSvg
{
    /// <summary>
    /// Contains information about a loaded PDF file.
    /// </summary>
    /// <example>
    /// <para>
    ///     The following example opens a PDF file and saves each page as an SVG file.
    /// </para>
    /// <code language="cs" title="Convert PDF to SVG">
    /// using (var doc = PdfDocument.Open("input.pdf"))
    /// {
    ///     var pageNo = 1;
    /// 
    ///     foreach (var page in doc.Pages)
    ///     {
    ///         page.SaveAsSvg($"output-{pageNo++}.svg");
    ///     }
    /// }
    /// </code>
    /// <para>
    ///     If the document is password protected, the password must be specified in <see cref="OpenOptions"/> when the
    ///     document is opened to allow conversion.
    /// </para>
    /// <code language="cs" title="Converting a password protected document">
    /// var openOptions = new OpenOptions
    /// {
    ///     Password = "document password"
    /// };
    /// 
    /// using (var doc = PdfDocument.Open("input.pdf", openOptions))
    /// {
    ///     var pageNo = 1;
    ///
    ///     foreach (var page in doc.Pages)
    ///     {
    ///         page.SaveAsSvg($"output-{pageNo++}.svg");
    ///     }
    /// }
    /// </code>
    /// </example>
    public sealed class PdfDocument : IDisposable
    {
        private readonly PdfDictionary root;
        private InputFile? file;

        internal PdfDocument(InputFile file, PdfDictionary? trailer, DocumentPermissions permissions, bool isEncrypted)
        {
            var info = trailer.GetDictionaryOrEmpty(Names.Info);

            this.root = trailer.GetDictionaryOrEmpty(Names.Root);
            this.file = file;

            this.Pages = new PdfPageCollection(PdfReader
                .GetFlattenedPages(root)
                .Select(dict => new PdfPage(this, dict))
                .ToList());

            this.Permissions = permissions;
            this.IsEncrypted = isEncrypted;
            this.Info = new DocumentInfo(info);

            OptionalContentGroupManager.Initialize(root);
        }

        internal DocumentCache Cache { get; } = new();

        internal OptionalContentGroupManager OptionalContentGroupManager { get; } = new();

        /// <summary>
        /// Gets information about which operations that the document author has allowed for this document.
        /// </summary>
        public DocumentPermissions Permissions { get; }

        /// <summary>
        /// Gets a collection of the optional content groups of this PDF document, whose visibility can be controlled
        /// individually.
        /// </summary>
        /// <inheritdoc cref="OptionalContentGroup" path="example"/>
        /// <seealso cref="OptionalContentGroup"/>
        public OptionalContentGroupCollection OptionalContentGroups => OptionalContentGroupManager.PublicGroups;

        /// <summary>
        /// Gets a value indicating whether this document is encrypted.
        /// </summary>
        public bool IsEncrypted { get; }

        /// <summary>
        /// Loads a PDF file from a stream.
        /// </summary>
        /// <param name="stream">Stream to read the PDF content from. The stream must be seekable.</param>
        /// <param name="options">Additional options for loading the PDF.</param>
        /// <param name="leaveOpen">If <c>true</c>, the stream is left open when the returned <see cref="PdfDocument"/> is disposed.</param>
        /// <param name="cancellationToken">Token for monitoring cancellation requests.</param>
        /// <returns><see cref="PdfDocument"/> for the specified stream.</returns>
        /// <exception cref="ArgumentException"><paramref name="stream"/> is not readable and/or seekable.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidCredentialException">The input PDF is encrypted, but an incorrect password was specified, or not specified at all.</exception>
        /// <exception cref="PdfException">The input PDF could not be parsed.</exception>
        /// <exception cref="OperationCanceledException">The operation was cancelled because the cancellation token was triggered.</exception>
        public static PdfDocument Open(Stream stream, bool leaveOpen = false, OpenOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead || !stream.CanSeek) throw new ArgumentException("The stream must be readable and seekable.", nameof(stream));

            return PdfReader.Read(new InputFile(stream, leaveOpen), options ?? new OpenOptions(), cancellationToken);
        }

        /// <summary>
        /// Loads a PDF file from a file path.
        /// </summary>
        /// <param name="path">Path to PDF file to open.</param>
        /// <param name="options">Additional options for loading the PDF.</param>
        /// <param name="cancellationToken">Token for monitoring cancellation requests.</param>
        /// <returns><see cref="PdfDocument"/> for the specified file.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
        /// <exception cref="IOException">An IO error occured while reading the file.</exception>
        /// <exception cref="FileNotFoundException">No file was found at <paramref name="path"/>.</exception>
        /// <exception cref="InvalidCredentialException">The input PDF is encrypted, but an incorrect password was specified, or not specified at all.</exception>
        /// <exception cref="PdfException">The input PDF could not be parsed.</exception>
        /// <exception cref="OperationCanceledException">The operation was cancelled because the cancellation token was triggered.</exception>
        public static PdfDocument Open(string path, OpenOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (path.Length == 0) throw new ArgumentException("The path must not be empty.", nameof(path));

            PdfDocument? result = null;

            var file = new InputFile(path, useAsync: false);

            try
            {
                result = PdfReader.Read(file, options ?? new OpenOptions(), cancellationToken);
            }
            finally
            {
                if (result == null)
                {
                    file.Dispose();
                }
            }

            return result;
        }

#if HAVE_ASYNC
        /// <summary>
        /// Loads a PDF file from a stream asynchronously.
        /// </summary>
        /// <inheritdoc cref="Open(Stream, bool, OpenOptions, CancellationToken)"/>
        public static async Task<PdfDocument> OpenAsync(Stream stream, bool leaveOpen = false, OpenOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead || !stream.CanSeek) throw new ArgumentException("The stream must be readable and seekable.", nameof(stream));

            return await PdfReader.ReadAsync(new InputFile(stream, leaveOpen), options ?? new OpenOptions(), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Loads a PDF file from a file path asynchronously.
        /// </summary>
        /// <inheritdoc cref="Open(string, OpenOptions, CancellationToken)"/>
        public static async Task<PdfDocument> OpenAsync(string path, OpenOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (path.Length == 0) throw new ArgumentException("The path must not be empty.", nameof(path));

            PdfDocument? result = null;

            var file = new InputFile(path, useAsync: true);

            try
            {
                result = await PdfReader.ReadAsync(file, options ?? new OpenOptions(), cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (result == null)
                {
                    file.Dispose();
                }
            }

            return result;
        }
#endif

        /// <summary>
        /// Gets an enumerable of images embedded in this document.
        /// </summary>
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
        ///         var content = image.GetContent();
        ///         var fileName = $"image{imageNo++}{image.Extension}";
        ///         File.WriteAllBytes(fileName, content);
        ///     }
        /// }
        /// </code>
        /// <para>
        ///     The returned enumerable can also be consumed asynchronously if .NET Framework is not targeted.
        /// </para>
        /// <code lang="cs">
        /// using (var document = await PdfDocument.OpenAsync("input.pdf"))
        /// {
        ///     var imageNo = 1;
        /// 
        ///     await foreach (var image in document.Images)
        ///     {
        ///         var content = await image.GetContentAsync();
        ///         var fileName = $"image{imageNo++}{image.Extension}";
        ///         File.WriteAllBytes(fileName, content);
        ///     }
        /// }
        /// </code>
        /// <para>
        ///     Images from a specific page can be accessed from <see cref="PdfPage.Images">PdfPage.Images</see>.
        /// </para>
        /// </example>
        /// <seealso cref="PdfPage.Images">PdfPage.Images</seealso>
        public ImageEnumerable Images
        {
            get
            {
                var pageDicts = Pages.Select(page => page.PageDictionary);
                return new ImageEnumerable(pageDicts, AssertExtractPermission);
            }
        }

        /// <summary>
        /// Gets information about the document provided by the author.
        /// </summary>
        public DocumentInfo Info { get; }

        /// <summary>
        /// Gets a collection of the pages in this PDF document.
        /// </summary>
        public PdfPageCollection Pages { get; }

        internal void AssertExtractPermission()
        {
            if (!Permissions.HasOwnerPermission &&
                !Permissions.AllowExtractContent)
            {
                throw new PermissionException(
                    "The document author does not allow content being extracted from this document. " +
                    "If you are the owner of the document, you can specify the owner password in an " + nameof(OpenOptions) + " instance " +
                    "passed to " + nameof(PdfDocument) + "." + nameof(Open) + " to proceed with the export.");
            }
        }

        /// <summary>
        /// Closes the PDF file.
        /// </summary>
        public void Dispose()
        {
            file?.Dispose();
            file = null;
        }
    }
}
