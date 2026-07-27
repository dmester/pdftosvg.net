// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace PdfToSvg.IO
{
    internal static class ZLibFactory
    {
        private const byte CM_Deflate = 8;

        public static Stream Decompress(Stream baseStream)
        {
            // Invalid Adler32 checksums seem to be present in some PDf files. Also some producers (see PdfSharp) are
            // completely omitting the checksum (see #47).
            //
            // The following PDF readers don't care about invalid or missing zlib checksums:
            // * Adobe Reader
            // * PDF.js
            // * Pdfium
            // * MuPDF
            //
            // Let's follow their lead and read the header but skip the checksum.
            //

            var cmf = baseStream.ReadByte();
            var flg = baseStream.ReadByte();

            if (cmf < 0 || flg < 0)
            {
                baseStream.Dispose();
                return Stream.Null;
            }
            else
            {
                VerifyHeader(cmf, flg);
                return new DeflateStream(baseStream, CompressionMode.Decompress);
            }
        }

        public static ZLibStream Compress(Stream baseStream, bool leaveOpen = false)
        {
            return new ZLibStream(baseStream, CompressionMode.Compress, leaveOpen);
        }

        public static void VerifyHeader(int cmf, int flg)
        {
            var cm = cmf & 0xf;
            if (cm != CM_Deflate)
            {
                throw new InvalidDataException("Unsupported compression algorithm in ZLib stream.");
            }

            var fdict = (flg >> 5) & 1;
            if (fdict != 0)
            {
                throw new InvalidDataException("Unknown dictionary in ZLib stream.");
            }

            if (((cmf * 256 + flg) % 31) != 0)
            {
                throw new InvalidDataException("Invalid ZLib header.");
            }
        }
    }
}
