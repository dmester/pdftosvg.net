﻿// Copyright (c) PdfToSvg.NET contributors.
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
    internal class RunLengthDecodeFilter : Filter
    {
        private const byte EodMarker = 128;

        public override Stream Decode(Stream stream, PdfDictionary? decodeParms)
        {
            return new RunLengthDecodeStream(stream);
        }

        public override bool CanDetectStreamLength => true;

        public override int DetectStreamLength(Stream stream)
        {
            // PDF spec 1.7, 7.4.5, page 37
            // Stream should end with > EOD marker. We will simply mark the stream as ended at any non-valid hex char.

            var buffer = new byte[2048];
            var totalRead = 0;
            var skipBytes = 0;
            int read;

            do
            {
                read = stream.Read(buffer, 0, buffer.Length);

                for (var i = 0; i < read;)
                {
                    if (skipBytes > 0)
                    {
                        var skipThisIteration = Math.Min(skipBytes, read - i);
                        i += skipThisIteration;
                        skipBytes -= skipThisIteration;
                    }
                    else if (buffer[i] == EodMarker)
                    {
                        // EOD marker
                        return totalRead + i + 1;
                    }
                    else if (buffer[i] < EodMarker)
                    {
                        // Copy data
                        skipBytes = buffer[i] + 2;
                    }
                    else
                    {
                        // Repeated byte
                        skipBytes = 2;
                    }
                }

                totalRead += read;
            }
            while (read > 0);

            return totalRead;
        }
    }
}
