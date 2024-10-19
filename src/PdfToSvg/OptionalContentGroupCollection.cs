// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PdfToSvg
{
    /// <summary>
    /// Contains a readonly collection of optional content groups from a PDF document.
    /// </summary>
    /// <seealso cref="PdfDocument.OptionalContentGroups">PdfDocument.OptionalContentGroups Property</seealso>
    public sealed class OptionalContentGroupCollection : ReadOnlyCollection<OptionalContentGroup>
    {
        internal OptionalContentGroupCollection(IList<OptionalContentGroup> groups) : base(groups)
        {
        }
    }
}
