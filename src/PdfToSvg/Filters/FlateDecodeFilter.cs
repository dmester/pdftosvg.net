// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.IO;
using System.IO;

namespace PdfToSvg.Filters
{
    internal class FlateDecodeFilter : Filter
    {
        public override Stream Decode(Stream stream, PdfDictionary? decodeParms)
        {
            Stream resultStream = ZLibFactory.Decompress(stream);

            if (resultStream != Stream.Null)
            {
                resultStream = PredictorStream.Create(resultStream, decodeParms);
            }

            return resultStream;
        }
    }
}
