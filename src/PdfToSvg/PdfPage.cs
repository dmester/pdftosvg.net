// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace PdfToSvg
{
    /// <summary>
    /// Represents a single page in a PDF document.
    /// </summary>
    public sealed class PdfPage
    {
        private readonly PdfDocument owner;
        private readonly PdfDictionary page;
        private FileAttachmentCollection? fileAttachments;

        internal PdfPage(PdfDocument owner, PdfDictionary page)
        {
            this.owner = owner;
            this.page = page;
        }

        /// <summary>
        /// Gets the owner <see cref="PdfDocument"/> that this page is part of.
        /// </summary>
        public PdfDocument Document => owner;

        /// <summary>
        /// Gets a collection of files attached to this page. The generated SVG might refer to an attachment with the
        /// <see cref="SvgConversionOptions.IncludeAnnotations">annot:file-index</see> attribute.
        /// </summary>
        public FileAttachmentCollection FileAttachments
        {
            get
            {
                if (fileAttachments == null)
                {
                    var annots =
                        page.GetArrayOrNull<PdfDictionary>(Names.Annots) ??
                        ArrayUtils.Empty<PdfDictionary>();

                    var files = annots
                        .Select(annot => FileAttachment.Create(annot))
                        .WhereNotNull()
                        .ToList();

                    Interlocked.CompareExchange(ref fileAttachments, new FileAttachmentCollection(files), null);
                }

                return fileAttachments;
            }
        }

        /// <summary>
        /// Converts this page to an SVG string. The string can for example be saved to a file, or inlined in HTML.
        /// </summary>
        /// <param name="options">Additional configuration options for the conversion.</param>
        /// <param name="cancellationToken">Token for monitoring cancellation requests.</param>
        /// <returns>SVG fragment without XML declaration. The fragment can be saved to a file or included as inline SVG in HTML.</returns>
        /// <exception cref="PermissionException">
        ///     Content extraction from this document is forbidden by the document author. 
        ///     Not thrown if the document is opened with the owner password.
        /// </exception>
        /// <exception cref="PdfException">
        ///     The conversion failed, possibly because of a malformed PDF.
        /// </exception>
        /// <remarks>
        ///     Note that if you parse the returned SVG fragment as XML, you need to preserve space and not add indentation. Text content
        ///     will otherwise not render correctly.
        /// </remarks>
        public string ToSvgString(SvgConversionOptions? options = null, CancellationToken cancellationToken = default)
        {
            AssertExtractPermission();

            return ToString(SvgRenderer.Convert(page, options, Document.Cache, cancellationToken));
        }

#if HAVE_ASYNC
        /// <summary>
        /// Converts this page to an SVG string asynchronously. The string can for example be saved to a file, or inlined in HTML.
        /// </summary>
        /// <inheritdoc cref="ToSvgString(SvgConversionOptions, CancellationToken)"/>
        public async Task<string> ToSvgStringAsync(SvgConversionOptions? options = null, CancellationToken cancellationToken = default)
        {
            AssertExtractPermission();

            var element = await SvgRenderer.ConvertAsync(page, options, Document.Cache, cancellationToken).ConfigureAwait(false);
            return ToString(element);
        }
#endif

        /// <summary>
        /// Saves the page as an SVG file.
        /// </summary>
        /// <param name="stream">Stream to write the SVG content to.</param>
        /// <param name="options">Additional configuration options for the conversion.</param>
        /// <param name="cancellationToken">Token for monitoring cancellation requests.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="stream"/> was <c>null</c>.
        /// </exception>
        /// <exception cref="PermissionException">
        ///     Content extraction from this document is forbidden by the document author. 
        ///     Not thrown if the document is opened with the owner password.
        /// </exception>
        /// <exception cref="PdfException">
        ///     The conversion failed, possibly because of a malformed PDF.
        /// </exception>
        public void SaveAsSvg(Stream stream, SvgConversionOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            AssertExtractPermission();

            var content = SvgRenderer.Convert(page, options, Document.Cache, cancellationToken);
            var document = new XDocument(content);

            using var writer = new SvgXmlWriter(stream, ConformanceLevel.Document);
            document.WriteTo(writer);
        }

        /// <summary>
        /// Saves the page as an SVG file.
        /// </summary>
        /// <param name="path">Path to output SVG file. If the file already exists, it will be overwritten.</param>
        /// <param name="options">Additional configuration options for the conversion.</param>
        /// <param name="cancellationToken">Token for monitoring cancellation requests.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="path"/> was <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="path"/> was an empty string.
        /// </exception>
        /// <exception cref="PermissionException">
        ///     Content extraction from this document is forbidden by the document author. 
        ///     Not thrown if the document is opened with the owner password.
        /// </exception>
        /// <exception cref="PdfException">
        ///     The conversion failed, possibly because of a malformed PDF.
        /// </exception>
        public void SaveAsSvg(string path, SvgConversionOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (path.Length == 0) throw new ArgumentException("The path must not be empty.", nameof(path));

            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            SaveAsSvg(stream, options, cancellationToken);
        }

#if HAVE_ASYNC
        /// <summary>
        /// Saves the page as an SVG file asynchronously.
        /// </summary>
        /// <inheritdoc cref="SaveAsSvg(Stream, SvgConversionOptions?, CancellationToken)"/>
        public async Task SaveAsSvgAsync(Stream stream, SvgConversionOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            AssertExtractPermission();

            var content = await SvgRenderer.ConvertAsync(page, options, Document.Cache, cancellationToken).ConfigureAwait(false);
            var document = new XDocument(content);

            // XmlTextWriter does not support async, so buffer the file before writing it to the output stream.
            using var memoryStream = new MemoryStream();
            using var writer = new SvgXmlWriter(memoryStream, ConformanceLevel.Document);

            document.WriteTo(writer);
            writer.Flush();

            var buffer = memoryStream.GetBufferOrArray();
            await stream.WriteAsync(buffer, 0, (int)memoryStream.Length, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Saves the page as an SVG file asynchronously.
        /// </summary>
        /// <inheritdoc cref="SaveAsSvg(string, SvgConversionOptions?, CancellationToken)"/>
        public async Task SaveAsSvgAsync(string path, SvgConversionOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (path.Length == 0) throw new ArgumentException("The path must not be empty.", nameof(path));

            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            await SaveAsSvgAsync(stream, options, cancellationToken).ConfigureAwait(false);
        }
#endif

        private void AssertExtractPermission()
        {
            if (!owner.Permissions.HasOwnerPermission &&
                !owner.Permissions.AllowExtractContent)
            {
                throw new PermissionException(
                    "The document author does not allow content being extracted from this document. " +
                    "If you are the owner of the document, you can specify the owner password in an " + nameof(OpenOptions) + " instance " +
                    "passed to " + nameof(PdfDocument) + "." + nameof(PdfDocument.Open) + " to proceed with the export.");
            }
        }

        private static string ToString(XNode el)
        {
            using var stringWriter = new StringWriter();
            using var writer = new SvgXmlWriter(stringWriter, ConformanceLevel.Fragment);

            el.WriteTo(writer);
            writer.Flush();

            return stringWriter.ToString();
        }
    }
}
