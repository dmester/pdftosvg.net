// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using System;
using System.IO;

namespace PdfToSvg.Filters
{
    internal class JpxDecodeFilter : Filter
    {
        public override Stream Decode(Stream stream, PdfDictionary? decodeParms)
        {
            throw new NotSupportedException("JPXDecode is only supported for image streams.");
        }
    }
}
