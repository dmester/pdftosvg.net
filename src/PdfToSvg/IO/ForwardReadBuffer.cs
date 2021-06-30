// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.IO
{
    internal class ForwardReadBuffer<T>
    {
        private readonly Func<T> reader;

        // Circular buffer
        private T[] buffer;
        private int bufferStartIndex;
        private int bufferLength;

        public ForwardReadBuffer(Func<T> reader, int maxPeekOffset)
        {
            this.reader = reader;
            buffer = new T[maxPeekOffset];
        }

        public void Clear()
        {
            bufferLength = 0;
        }

        public T Read()
        {
            if (bufferLength > 0)
            {
                var previousIndex = bufferStartIndex;
                bufferStartIndex = (bufferStartIndex + 1) % buffer.Length;
                bufferLength--;
                return buffer[previousIndex];
            }

            return reader();
        }

        public T Peek()
        {
            return Peek(1);
        }

        public T Peek(int offset)
        {
            if (offset < 1 || offset > buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));

            while (offset > bufferLength)
            {
                buffer[(bufferStartIndex + bufferLength) % buffer.Length] = reader();
                bufferLength++;
            }

            return buffer[(bufferStartIndex + offset - 1) % buffer.Length];
        }
    }
}
