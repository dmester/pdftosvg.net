using PdfToSvg.DocumentModel;
using System;
using System.IO;
using System.IO.Compression;

namespace PdfToSvg.Filters
{
    internal class FlateDecodeFilter : Filter
    {
        public override Stream Decode(Stream stream, PdfDictionary decodeParms)
        {
            var deflateStream = new GZipStream(stream, CompressionMode.Decompress);
            return PredictorStream.Create(deflateStream, decodeParms);
        }
    }
}
