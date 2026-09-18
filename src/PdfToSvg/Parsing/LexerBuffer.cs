// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace PdfToSvg.Parsing
{
    internal struct LexerBuffer
    {
        public byte[] Buffer;
        public int Length;

        public LexerBuffer(int initialSize)
        {
            Buffer = new byte[initialSize];
        }

        public void Clear()
        {
            Length = 0;
        }

        public void WriteByte(byte value)
        {
            if (Length >= Buffer.Length)
            {
                Grow(1);
            }

            Buffer[Length++] = value;
        }

        public void WriteChar(char value)
        {
            if (Length >= Buffer.Length)
            {
                Grow(1);
            }

            Buffer[Length++] = (byte)value;
        }

        public PdfString ToPdfString()
        {
            return new PdfString(Buffer, 0, Length);
        }

        public override string ToString()
        {
            var chars = new char[Length];
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = (char)Buffer[i];
            }

            return new string(chars);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Grow(int neededBytes)
        {
            const int ExtraBytes = 128;
            var newCapacity = Math.Max(Buffer.Length * 2, Length + neededBytes + ExtraBytes);
            var newBuffer = new byte[newCapacity];
            System.Buffer.BlockCopy(Buffer, 0, newBuffer, 0, Length);
            Buffer = newBuffer;
        }
    }
}
