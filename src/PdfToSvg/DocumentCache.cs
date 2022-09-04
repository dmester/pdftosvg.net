// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Fonts;
using PdfToSvg.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace PdfToSvg
{
    internal class DocumentCache
    {
        /// <summary>
        /// Contains cached fonts per <see cref="FontResolver"/>. The key of the inner dictionary is a font dictionary.
        /// </summary>
        public ConditionalWeakTable<FontResolver, Dictionary<PdfDictionary, SharedFactory<BaseFont>>> Fonts { get; } = new();
    }
}
