// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Filters
{
    internal class Ascii85DecodeFilter : Filter
    {
        public override Stream Decode(Stream stream, PdfDictionary? decodeParms)
        {
            return new Ascii85DecodeStream(stream);
        }

        public override bool CanDetectStreamLength => true;

        public override int DetectStreamLength(Stream stream)
        {
            // PDF spec 1.7, 7.4.3, page 33
            // Stream should end with ~> EOD marker.

            var buffer = new byte[2048];
            var foundEndMarker1 = false;
            var totalRead = 0;
            int read;

            do
            {
                read = stream.Read(buffer, 0, buffer.Length);

                for (var i = 0; i < read; i++)
                {
                    var ch = (char)buffer[i];

                    if (ch == Ascii85DecodeStream.EndMarker1)
                    {
                        foundEndMarker1 = true;
                    }
                    else if (foundEndMarker1)
                    {
                        if (ch == Ascii85DecodeStream.EndMarker2)
                        {
                            // Found complete EOD. Include EOD marker
                            return totalRead + i + 1;
                        }
                        else if (!PdfCharacters.IsWhiteSpace(ch))
                        {
                            // Some other character. Break the stream before this character
                            return totalRead + i;
                        }
                    }
                    else if (
                        (ch < Ascii85DecodeStream.FirstChar || ch > Ascii85DecodeStream.LastChar) &&
                        ch != Ascii85DecodeStream.ZeroGroup &&
                        !PdfCharacters.IsWhiteSpace(ch))
                    {
                        // Invalid Ascii85 character, break stream before this character
                        return totalRead + i;
                    }
                }

                totalRead += read;
            }
            while (read > 0);

            return totalRead;
        }
    }
}
