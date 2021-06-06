// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Filters
{
    internal class AsciiHexDecodeStream : DecodeStream
    {
        private readonly Stream stream;
        private int hi = -1;

        public AsciiHexDecodeStream(Stream stream, int bufferSize = 2048)
        {
            if (bufferSize < 1) throw new ArgumentOutOfRangeException(nameof(bufferSize), "Buffer size must be at least 1 byte.");

            this.stream = stream;
            this.buffer = new byte[bufferSize];
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                stream.Dispose();
            }
        }

        private void FlushFinalByte()
        {
            if (hi >= 0)
            {
                buffer[bufferLength] = (byte)(hi);
                bufferLength++;
            }
        }

        protected override void FillBuffer()
        {
            var read = stream.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                FlushFinalByte();
                endOfStream = true;
                return;
            }

            bufferLength = 0;
            bufferCursor = 0;

            int digit;

            for (var i = 0; i < read; i++)
            {
                var ch = (char)buffer[i];
                if (ch == '>')
                {
                    FlushFinalByte();
                    endOfStream = true;
                    break;
                }
                else if (ch >= '0' && ch <= '9')
                {
                    digit = ch - '0';
                }
                else if (ch >= 'a' && ch <= 'f')
                {
                    digit = ch - 'a' + 10;
                }
                else if (ch >= 'A' && ch <= 'F')
                {
                    digit = ch - 'A' + 10;
                }
                else if (PdfCharacters.IsWhiteSpace(ch))
                {
                    continue;
                }
                else
                {
                    endOfStream = true;
                    throw new FilterException("AsciiHex", (byte)ch);
                }

                if (hi >= 0)
                {
                    buffer[bufferLength] = (byte)(hi | digit);
                    bufferLength++;
                    hi = -1;
                }
                else
                {
                    hi = digit << 4;
                }
            }
        }
    }
}
