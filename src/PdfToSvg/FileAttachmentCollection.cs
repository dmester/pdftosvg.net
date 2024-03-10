// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PdfToSvg
{
    /// <summary>
    /// Read only collection of files attached to a PDF page.
    /// </summary>
    public class FileAttachmentCollection : ReadOnlyCollection<FileAttachment>
    {
        internal FileAttachmentCollection(IList<FileAttachment> list) : base(list)
        {
        }
    }
}
