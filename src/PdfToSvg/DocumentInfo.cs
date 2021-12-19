// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Fonts.OpenType;
using PdfToSvg.Fonts.OpenType.Tables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg
{
    /// <summary>
    /// Provides information about a PDF document.
    /// </summary>
    public class DocumentInfo
    {
        private readonly PdfDictionary info;

        internal DocumentInfo(PdfDictionary info)
        {
            this.info = info;
        }

        /// <summary>
        /// Gets the title of the document.
        /// </summary>
        public string? Title => info.GetValueOrDefault<PdfString?>(Names.Title)?.ToString();

        /// <summary>
        /// Gets the author of the document.
        /// </summary>
        public string? Author => info.GetValueOrDefault<PdfString?>(Names.Author)?.ToString();

        /// <summary>
        /// Gets the subject of the document.
        /// </summary>
        public string? Subject => info.GetValueOrDefault<PdfString?>(Names.Subject)?.ToString();

        /// <summary>
        /// Gets keywords specified for this document.
        /// </summary>
        public string? Keywords => info.GetValueOrDefault<PdfString?>(Names.Keywords)?.ToString();

        /// <summary>
        /// Gets the software used for creating the document.
        /// </summary>
        public string? Creator => info.GetValueOrDefault<PdfString?>(Names.Creator)?.ToString();

        /// <summary>
        /// Gets the software used for creating the PDF file.
        /// </summary>
        public string? Producer => info.GetValueOrDefault<PdfString?>(Names.Producer)?.ToString();

        /// <summary>
        /// Gets the date when the document was created.
        /// </summary>
        public DateTimeOffset? CreationDate => info.GetValueOrDefault<DateTimeOffset?>(Names.CreationDate);

        /// <summary>
        /// Gets the date when the document was modified.
        /// </summary>
        public DateTimeOffset? ModDate => info.GetValueOrDefault<DateTimeOffset?>(Names.ModDate);
    }
}
