// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Png;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Filters
{
    internal class PngDepredictorStream : DecodeStream
    {
        private enum PngFilter : byte
        {
            None = 0,
            Sub = 1,
            Up = 2,
            Average = 3,
            Paeth = 4,
        }

        private readonly Stream stream;
        private readonly int colors;
        private readonly int bitsPerComponent;
        private readonly int bytesPerRow;
        private byte[] previousBuffer;

        public PngDepredictorStream(Stream sourceStream, int colors, int bitsPerComponent, int columns, int bufferSize = 2048)
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
            bytesPerRow = 1 + MathUtils.BitsToBytes(colors * bitsPerComponent * columns);

            buffer = new byte[bytesPerRow];
            previousBuffer = new byte[bytesPerRow];
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                stream.Dispose();
            }
        }

        private void SwapBuffers()
        {
            var tmp = previousBuffer;
            previousBuffer = buffer;
            buffer = tmp;
        }

        protected override void FillBuffer()
        {
            SwapBuffers();

            bufferCursor = 1;
            bufferLength = stream.ReadAll(buffer, 0, buffer.Length);

            if (bufferLength == 0)
            {
                endOfStream = true;
                return;
            }

            var sampleSizeBytes = MathUtils.BitsToBytes(colors * bitsPerComponent);

            switch ((PngFilter)buffer[0])
            {
                case PngFilter.Sub:
                    for (var i = 1 + sampleSizeBytes; i < buffer.Length; i++)
                    {
                        buffer[i] = unchecked((byte)(buffer[i - sampleSizeBytes] + buffer[i]));
                    }
                    break;

                case PngFilter.Up:
                    for (var i = 1; i < buffer.Length; i++)
                    {
                        buffer[i] = unchecked((byte)(previousBuffer[i] + buffer[i]));
                    }
                    break;

                case PngFilter.Average:
                    for (var i = 1; i < 1 + sampleSizeBytes; i++)
                    {
                        buffer[i] = unchecked((byte)(previousBuffer[i] / 2 + buffer[i]));
                    }

                    for (var i = 1 + sampleSizeBytes; i < buffer.Length; i++)
                    {
                        buffer[i] = unchecked((byte)(
                            buffer[i] +
                            (buffer[i - sampleSizeBytes] + previousBuffer[i]) / 2
                            ));
                    }

                    break;

                case PngFilter.Paeth:
                    for (var i = 1; i < 1 + sampleSizeBytes; i++)
                    {
                        buffer[i] = unchecked((byte)(buffer[i] + PngUtils.PaethPredictor(0, previousBuffer[i], 0)));
                    }

                    for (var i = 1 + sampleSizeBytes; i < buffer.Length; i++)
                    {
                        buffer[i] = unchecked((byte)(buffer[i] + PngUtils.PaethPredictor(
                            buffer[i - sampleSizeBytes],
                            previousBuffer[i],
                            previousBuffer[i - sampleSizeBytes]
                            )));
                    }
                    break;
            }
        }
    }
}
