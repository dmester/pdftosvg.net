// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Filters
{
    internal class CcittFaxDecodeFilter : Filter
    {
        public override Stream Decode(Stream stream, PdfDictionary? decodeParms)
        {
            throw new NotSupportedException("CCITTFaxDecode is only supported for image streams.");
        }
    }
}
