// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Filters;
using System;
using System.IO;
using System.IO.Compression;

namespace PdfToSvg.Filters
{
    internal class DctDecodeFilter : Filter
    {
        public override Stream Decode(Stream stream, PdfDictionary? decodeParms)
        {
            throw new NotSupportedException("DCTDecode is only supported for image streams.");
        }
    }
}
