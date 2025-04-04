// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.IO;
using System;
using System.IO;
using System.IO.Compression;
using CompressionMode = System.IO.Compression.CompressionMode;

namespace PdfToSvg.Filters
{
    internal class FlateDecodeFilter : Filter
    {
        public override Stream Decode(Stream stream, PdfDictionary? decodeParms)
        {
            // Invalid Adler32 checksums seem to be present in some PDf files. Also some producers (see PdfSharp) are completely omitting the checksum (see #47).
            //
            // The following PDF readers don't care about invalid or missing zlib checksums:
            // * Adobe Reader
            // * PDF.js
            // * Pdfium
            // * MuPDF
            //
            // Let's follow their lead and read the header but skip the checksum.
            //

            var cmf = stream.ReadByte();
            var flg = stream.ReadByte();

            if (cmf < 0 || flg < 0)
            {
                return Stream.Null;
            }
            else
            {
                ZLibStream.VerifyHeader(cmf, flg);

                var deflateStream = new DeflateStream(stream, CompressionMode.Decompress);
                return PredictorStream.Create(deflateStream, decodeParms);
            }
        }
    }
}
