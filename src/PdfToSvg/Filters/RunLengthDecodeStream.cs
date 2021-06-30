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
    internal class RunLengthDecodeStream : DecodeStream
    {
        private readonly Stream stream;

        private int copyBytesLeft;

        private byte repeatedByte;
        private int repeatsLeft;

        private const byte EodMarker = 128;

        public RunLengthDecodeStream(Stream stream)
        {
            this.stream = stream;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                stream.Dispose();
            }
        }

        protected override void FillBuffer()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = 0;

            while (read < count && !endOfStream)
            {
                if (repeatsLeft > 0)
                {
                    buffer[read] = repeatedByte;
                    read++;
                    repeatsLeft--;
                }
                else if (copyBytesLeft > 0)
                {
                    var readThisIteration = stream.Read(buffer, offset + read, Math.Min(copyBytesLeft, count - read));
                    if (readThisIteration == 0)
                    {
                        endOfStream = true;
                        throw new FilterException("Unexpected end of stream in RunLengthDecode.");
                    }
                    else
                    {
                        read += readThisIteration;
                        copyBytesLeft -= readThisIteration;
                    }
                }
                else
                {
                    var nextByte = stream.ReadByte();
                    if (nextByte < 0 || nextByte == EodMarker)
                    {
                        endOfStream = true;
                        break;
                    }

                    if (nextByte < EodMarker)
                    {
                        // Copy data
                        copyBytesLeft = nextByte + 1;
                    }
                    else
                    {
                        // Repeated byte
                        repeatsLeft = 257 - nextByte;

                        nextByte = stream.ReadByte();
                        if (nextByte < 0)
                        {
                            throw new FilterException("Unexpected end of stream in RunLengthDecode.");
                        }

                        repeatedByte = (byte)nextByte;
                    }
                }
            }

            position += read;

            return read;
        }

    }
}
