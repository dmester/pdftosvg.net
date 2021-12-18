// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace PdfToSvg.IO
{
    internal abstract class MemoryWriter
    {
        private const int StartBufferSize = 1024;

        protected byte[] buffer;
        protected int cursor;
        protected int length;

        public MemoryWriter() : this(StartBufferSize) { }

        public MemoryWriter(int capacity)
        {
            this.buffer = new byte[capacity];
        }

        public int Position
        {
            get => cursor;
            set
            {
                cursor = value;

                EnsureCapacity(cursor);

                if (length < cursor)
                {
                    length = cursor;
                }
            }
        }

        public int Length => length;

        public int Capacity => buffer.Length;

        private void ExpandBuffer(int minimumCapacity)
        {
            var newSize = Math.Max(buffer.Length * 2, minimumCapacity + 1024);
            var newBuffer = new byte[newSize];
            Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length);
            buffer = newBuffer;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void EnsureCapacity(int minimumCapacity)
        {
            if (minimumCapacity > buffer.Length)
            {
                ExpandBuffer(minimumCapacity);
            }
        }

        public ArraySegment<byte> GetBuffer()
        {
            return new ArraySegment<byte>(buffer, 0, length);
        }

        public byte[] ToArray()
        {
            var result = new byte[length];
            Buffer.BlockCopy(buffer, 0, result, 0, length);
            return result;
        }
    }
}
