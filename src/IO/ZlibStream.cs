using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PdfToSvg;
using System.IO.Compression;

namespace PdfToSvg.IO
{
    /// <summary>
    /// Wraps a Deflate stream in a zlib header and a Adler32 checksum according to https://tools.ietf.org/html/rfc1950.
    /// </summary>
    internal class ZlibStream : Stream
    {
        Adler32 adler32 = new Adler32();
        Stream outputStream;
        Stream deflateStream;

        public ZlibStream(Stream stream)
        {
            // CMF(Compression Method and flags)
            const byte cmf = 0x78;
            // CM (Compression method) = 8
            // CINFO (Compression info) = 32K window size

            // FLG (FLaGs)
            // FLEVEL = 2, compressor used default algorithm
            // FDICT = 0
            byte flg = 2 << 6;
            
            // FCHECK
            var mod = (cmf * 256 + flg % 31) % 31;
            if (mod != 0) flg += (byte)(31 - mod);

            stream.WriteByte(cmf);
            stream.WriteByte(flg);

            outputStream = stream;

            deflateStream = new DeflateStream(stream, CompressionLevel.Optimal);
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => deflateStream.Length;

        public override long Position { get => deflateStream.Position; set => throw new NotSupportedException(); }

        public override void Flush()
        {
            deflateStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            deflateStream.Write(buffer, offset, count);
            adler32.Update(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && deflateStream != null)
            {
                deflateStream.Flush();
                deflateStream.Dispose();
                deflateStream = null;

                outputStream.WriteBigEndian(adler32.Value);
            }

            base.Dispose(disposing);
        }
    }
}
