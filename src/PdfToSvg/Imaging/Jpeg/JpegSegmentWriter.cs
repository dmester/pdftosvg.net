// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpeg
{
    internal class JpegSegmentWriter : IDisposable
    {
        private readonly MemoryStream stream;
        private long lengthPosition;

        private byte partialByte;
        private long partialByteCursor = long.MinValue;

        public JpegSegmentWriter(MemoryStream stream, JpegMarkerCode marker)
        {
            this.stream = stream;

            WriteUInt16((int)marker);

            // Segment length
            lengthPosition = stream.Position;
            WriteUInt16(0); // Will be replaced by Dispose()
        }

        public void WriteNibble(int value)
        {
            if (partialByteCursor == stream.Position - 1)
            {
                stream.Position--;
                stream.WriteByte((byte)(partialByte | (value & 0xf)));
                partialByteCursor = long.MinValue;
            }
            else
            {
                partialByteCursor = stream.Position;
                partialByte = (byte)(value << 4);
                stream.WriteByte(partialByte);
            }
        }

        public void WriteUInt16(int value)
        {
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        public void WriteByte(int value)
        {
            stream.WriteByte((byte)value);
        }

        public void WriteBytes(ArraySegment<byte> bytes)
        {
            if (bytes.Array == null)
            {
                throw new ArgumentException("Array must not be null.", nameof(bytes));
            }

            stream.Write(bytes.Array, bytes.Offset, bytes.Count);
        }

        public void Dispose()
        {
            if (lengthPosition >= 0)
            {
                var currentPosition = stream.Position;
                var segmentLength = (int)(currentPosition - lengthPosition);

                stream.Position = lengthPosition;

                WriteUInt16(segmentLength);

                stream.Position = currentPosition;
            }
        }
    }
}
