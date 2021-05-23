using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Filters
{
    internal class Ascii85DecodeStream : DecodeStream
    {
        private readonly Stream stream;
        private readonly byte[] readBuffer;

        private uint group;
        private int groupSize;

        private const int EncodedGroupSize = 5;
        private const int Base = 85;

        public const char FirstChar = '!';
        public const char LastChar = 'u';
        public const char EndMarker1 = '~';
        public const char EndMarker2 = '>';
        public const char ZeroGroup = 'z';

        public Ascii85DecodeStream(Stream stream, int bufferSize = 2048)
        {
            if (bufferSize < 4) throw new ArgumentOutOfRangeException(nameof(bufferSize), "Buffer size must be at least 4 bytes.");

            this.stream = stream;

            // A separate read buffer is required since the "z" token will expand to 4 bytes.
            this.buffer = new byte[bufferSize];
            this.readBuffer = new byte[bufferSize / 4];
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
            var read = stream.Read(readBuffer, 0, readBuffer.Length);
            if (read == 0)
            {
                FlushFinalBytes();
                endOfStream = true;
                return;
            }

            bufferLength = 0;
            bufferCursor = 0;

            for (var i = 0; i < read; i++)
            {
                var ch = (char)readBuffer[i];
                if (ch >= FirstChar && ch <= LastChar)
                {
                    group = unchecked(group * Base + (uint)(ch - FirstChar));
                    groupSize++;

                    if (groupSize == EncodedGroupSize)
                    {
                        FlushBytes();
                    }
                }
                else if (ch == ZeroGroup)
                {
                    if (groupSize > 0)
                    {
                        endOfStream = true;
                        throw new FilterException("Encountered an unexpected zero group ('z') inside another group of an Ascii85 stream.");
                    }

                    FlushZeroBytes();
                }
                else if (ch == EndMarker1)
                {
                    FlushFinalBytes();
                    endOfStream = true;
                    break;
                }
                else if (PdfCharacters.IsWhiteSpace(ch))
                {
                    continue;
                }
                else
                {
                    endOfStream = true;
                    throw new FilterException("Ascii85", (byte)ch);
                }
            }
        }

        private void FlushBytes()
        {
            buffer[bufferLength + 0] = unchecked((byte)(group >> 24));
            buffer[bufferLength + 1] = unchecked((byte)(group >> 16));
            buffer[bufferLength + 2] = unchecked((byte)(group >> 8));
            buffer[bufferLength + 3] = unchecked((byte)group);

            group = 0;
            bufferLength += 4;
            groupSize = 0;
        }

        private void FlushZeroBytes()
        {
            buffer[bufferLength + 0] = 0;
            buffer[bufferLength + 1] = 0;
            buffer[bufferLength + 2] = 0;
            buffer[bufferLength + 3] = 0;
            bufferLength += 4;
            groupSize = 0;
        }

        private void FlushFinalBytes()
        {
            var partialGroupSize = this.groupSize;

            while (groupSize < EncodedGroupSize)
            {
                group = unchecked(group * Base + (Base - 1));
                groupSize++;
            }

            FlushBytes();

            if (partialGroupSize < EncodedGroupSize)
            {
                bufferLength -= EncodedGroupSize - partialGroupSize;
            }
        }
    }
}