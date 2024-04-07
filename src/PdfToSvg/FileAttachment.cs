// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.IO;
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
    /// Provides access to an embedded file attached to a PDF document.
    /// </summary>
    /// <seealso cref="PdfPage.FileAttachments"/>
    public class FileAttachment
    {
        private readonly PdfStream stream;

        private FileAttachment(string? name, PdfStream stream)
        {
            this.Name = name;
            this.stream = stream;
        }

        /// <summary>
        /// Gets the name of this file. Note that the name might be missing or be an invalid filename.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Gets the content of the attached file.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Stream"/> containing the data of the file. The caller is responsible for disposing the stream.
        /// </returns>
        /// <exception cref="OperationCanceledException">The operation was cancelled because the cancellation token was triggered.</exception>
        public Stream GetContent(CancellationToken cancellationToken = default)
        {
            // The data is copied to a MemoryStream to prevent callers from being able to cause deadlocks
            var memoryStream = new MemoryStream();

            using (var decodedStream = stream.OpenDecoded(cancellationToken))
            {
                decodedStream.CopyTo(memoryStream);
            }

            memoryStream.Position = 0;
            return memoryStream;
        }

#if HAVE_ASYNC
        /// <inheritdoc cref="GetContent(CancellationToken)"/>
        /// <summary>
        /// Gets the content of the attached file asynchronously.
        /// </summary>
        public async Task<Stream> GetContentAsync(CancellationToken cancellationToken = default)
        {
            // The data is copied to a MemoryStream to prevent callers from being able to cause deadlocks
            var memoryStream = new MemoryStream();

            using (var decodedStream = await stream.OpenDecodedAsync(cancellationToken).ConfigureAwait(false))
            {
                await decodedStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            }

            memoryStream.Position = 0;
            return memoryStream;
        }
#endif

        internal static FileAttachment? Create(PdfDictionary annot)
        {
            if (!annot.TryGetName(Names.Subtype, out var subtype) || subtype != Names.FileAttachment)
            {
                return null;
            }

            if (!annot.TryGetDictionary(Names.FS, out var fs))
            {
                return null;
            }

            if (!fs.TryGetDictionary(Names.EF, out var ef))
            {
                return null;
            }

            if (!ef.TryGetStream(Names.UF, out var stream) &&
                !ef.TryGetStream(Names.F, out stream))
            {
                return null;
            }

            var name = (
                    fs.GetValueOrDefault(Names.UF, (PdfString?)null) ??
                    fs.GetValueOrDefault(Names.F, (PdfString?)null)
                )?.ToString();

            if (name != null)
            {
                var lastDirSeparator = name.LastIndexOfAny(new char[] { '/', '\\' });
                if (lastDirSeparator >= 0)
                {
                    name = name.Substring(lastDirSeparator + 1);
                }
            }

            return new FileAttachment(name?.ToString(), stream);
        }
    }
}
