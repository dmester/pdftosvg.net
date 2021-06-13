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
    internal class TiffDepredictorStream : DecodeStream
    {
        private readonly Stream stream;
        private readonly int colors;
        private readonly int bitsPerComponent;
        private readonly int columns;
        private readonly int bytesPerRow;

        public TiffDepredictorStream(Stream sourceStream, int colors, int bitsPerComponent, int columns, int bufferSize = 2048)
        {
            if (bitsPerComponent != 1 &&
                bitsPerComponent != 2 &&
                bitsPerComponent != 4 &&
                bitsPerComponent != 8 &&
                bitsPerComponent != 16)
            {
                throw new ArgumentOutOfRangeException(nameof(bitsPerComponent));
            }

            stream = sourceStream;
            this.colors = colors;
            this.bitsPerComponent = bitsPerComponent;
            this.columns = columns;
            bytesPerRow = (colors * bitsPerComponent * columns + 7) / 8;

            this.buffer = new byte[Math.Max(bytesPerRow, bufferSize - (bufferSize % bytesPerRow))];
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
            bufferCursor = 0;
            bufferLength = 0;

            while (bufferCursor + bytesPerRow > bufferLength)
            {
                var read = stream.Read(buffer, bufferLength, buffer.Length - bufferLength);
                if (read == 0)
                {
                    endOfStream = true;
                    break;
                }
                bufferLength += read;
            }

            // TODO handle incomplete rows
            if (bitsPerComponent == 8)
            {
                var bytesPerSample = colors;

                for (var rowStart = bufferCursor; rowStart + bytesPerRow <= bufferLength; rowStart += bytesPerRow)
                {
                    for (int colorOffset = rowStart + bytesPerSample; colorOffset < rowStart + bytesPerRow; colorOffset++)
                    {
                        var previous = buffer[colorOffset - colors];
                        var current = buffer[colorOffset] + previous;
                        buffer[colorOffset] = unchecked((byte)current);
                    }
                }
            }
            else if (bitsPerComponent == 16)
            {
                var bytesPerSample = colors * 2;

                for (var rowStart = bufferCursor; rowStart + bytesPerRow <= bufferLength; rowStart += bytesPerRow)
                {
                    for (var colorOffset = rowStart + bytesPerSample; colorOffset < rowStart + bytesPerRow; colorOffset += 2)
                    {
                        var previous = (buffer[colorOffset - bytesPerSample] << 8) | buffer[colorOffset - bytesPerSample + 1];
                        var current = ((buffer[colorOffset] << 8) | buffer[colorOffset + 1]) + previous;
                        buffer[colorOffset] = unchecked((byte)(current >> 8));
                        buffer[colorOffset + 1] = unchecked((byte)current);
                    }
                }
            }
            else // bitsPerComponent is 1, 2 or 4
            {
                var componentMask = (1 << bitsPerComponent) - 1;

                var previousSample = new int[colors];

                for (var rowStart = bufferCursor; rowStart + bytesPerRow <= bufferLength; rowStart += bytesPerRow)
                {
                    Array.Clear(previousSample, 0, previousSample.Length);

                    for (int column = 0, bitIndex = 0; column < columns; column++)
                    {
                        for (var color = 0; color < colors; color++)
                        {
                            var byteIndex = rowStart + (bitIndex >> 3);
                            var byteValue = buffer[byteIndex];

                            var bitOffset = 8 - (bitIndex & 7) - bitsPerComponent;

                            var componentValue = ((byteValue >> bitOffset) + previousSample[color]) & componentMask;

                            previousSample[color] = componentValue;

                            buffer[byteIndex] = unchecked((byte)(
                                (byteValue & ~(componentMask << bitOffset)) |
                                (componentValue << bitOffset)));

                            bitIndex += bitsPerComponent;
                        }
                    }
                }
            }
        }
    }
}
