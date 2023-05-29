// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpeg
{
    internal class JpegSegmentReader
    {
        private readonly byte[] buffer;
        private int offset;
        private int length;

        private int cursor;

        private int partialByte;
        private int partialByteCursor = int.MinValue;

        public JpegSegmentReader(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (count < 0 || offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            this.buffer = buffer;
            this.offset = offset;
            this.length = count;
        }

        public int Cursor
        {
            get => cursor;
            set
            {
                if (cursor < 0) throw new ArgumentOutOfRangeException(nameof(Cursor));
                if (cursor > length) throw new ArgumentOutOfRangeException(nameof(Cursor));
                cursor = value;
            }
        }

        public int Length => length;

        public int ReadNibble()
        {
            if (cursor - 1 == partialByteCursor)
            {
                partialByteCursor = int.MinValue;
                return partialByte & 0xf;
            }

            if (cursor < length)
            {
                partialByte = buffer[offset + cursor];
                partialByteCursor = cursor++;
                return partialByte >> 4;
            }

            throw new JpegException("Unexpected end of input.");
        }

        public int ReadByte()
        {
            if (cursor < length)
            {
                return buffer[offset + cursor++];
            }
            else
            {
                throw new JpegException("Unexpected end of input.");
            }
        }

        public ArraySegment<byte> ReadBytes(int count)
        {
            if (cursor + count > length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            cursor += count;

            return new ArraySegment<byte>(buffer, offset + cursor - count, count);
        }

        public int ReadUInt16()
        {
            if (cursor + 1 < length)
            {
                var result =
                    (buffer[offset + cursor] << 8) |
                    buffer[offset + cursor + 1];

                cursor += 2;

                return result;
            }
            else
            {
                throw new JpegException("Unexpected end of input.");
            }
        }

        public JpegSegmentReader SliceReader(int length)
        {
            if (cursor + length > this.length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            var reader = new JpegSegmentReader(buffer, offset + cursor, length);
            cursor += length;
            return reader;
        }
    }
}
