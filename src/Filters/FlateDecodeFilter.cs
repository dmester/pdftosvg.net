// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.IO;
using System;
using System.IO;
using System.IO.Compression;

namespace PdfToSvg.Filters
{
    internal class FlateDecodeFilter : Filter
    {
        public override Stream Decode(Stream stream, PdfDictionary? decodeParms)
        {
            var deflateStream = ZLibStreamFactory.Create(stream, CompressionMode.Decompress);
            return PredictorStream.Create(deflateStream, decodeParms);
        }
    }
}
