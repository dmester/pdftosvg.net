// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PdfToSvg.Imaging.Png
{
    /// <summary>
    /// A stream for writing the data of an individual PNG chunk.
    /// </summary>
    internal class PngChunkStream : Stream
    {
        private readonly Stream outputStream;
        private readonly MemoryStream buffer;
        private readonly string name;

        public PngChunkStream(Stream outputStream, string name, int capacity = 0)
        {
            this.outputStream = outputStream;
            this.name = name;
            this.buffer = new MemoryStream(capacity);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                var data = buffer.GetBufferOrArray();

                var dataLength = (int)buffer.Length;
                var crc = new Crc32();

                // Length 32
                outputStream.WriteBigEndian(dataLength);

                // Name
                var binaryName = Encoding.UTF8.GetBytes(name);
                outputStream.Write(binaryName);
                crc.Update(binaryName, 0, binaryName.Length);

                // Data
                outputStream.Write(data, 0, dataLength);
                crc.Update(data, 0, dataLength);

                // crc32: type + data
                outputStream.WriteBigEndian(crc.Value);
            }
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => buffer.Length;

        public override long Position { get => buffer.Position; set => buffer.Position = value; }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.buffer.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return buffer.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            buffer.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.buffer.Write(buffer, offset, count);
        }
    }
}
