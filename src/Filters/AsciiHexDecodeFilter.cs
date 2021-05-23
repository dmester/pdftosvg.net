using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Filters
{
    internal class AsciiHexDecodeFilter : Filter
    {
        public override Stream Decode(Stream stream, PdfDictionary decodeParms)
        {
            return new AsciiHexDecodeStream(stream);
        }

        public override bool CanDetectStreamLength => true;

        public override int DetectStreamLength(Stream stream)
        {
            // PDF spec 1.7, 7.4.2, page 31
            // Stream should end with > EOD marker. We will simply mark the stream as ended at any non-valid hex char.

            var buffer = new byte[2048];
            var totalRead = 0;
            int read;

            do
            {
                read = stream.Read(buffer, 0, buffer.Length);

                for (var i = 0; i < read; i++)
                { 
                    var ch = (char)buffer[i];

                    if ((ch < '0' || ch > '9') &&
                        (ch < 'A' || ch > 'F') &&
                        (ch < 'a' || ch > 'f') &&
                        !PdfCharacters.IsWhiteSpace(ch))
                    {
                        if (ch == '>')
                        {
                            // Include EOD marker in stream
                            return totalRead + i + 1;
                        }
                        else
                        {
                            return totalRead + i;
                        }
                    }
                }

                totalRead += read;
            }
            while (read > 0);

            return totalRead;
        }
    }
}
