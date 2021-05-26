using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PdfToSvg;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Threading;

namespace PdfToSvg.IO
{
    /// <summary>
    /// Compresses or decompresses an RFC 1950 compatible zlib/deflate stream.
    /// </summary>
    internal class ZLibStream : Stream
    {
        // IMPLEMENTATION NOTES
        //
        // The built-in .NET DeflateStream does not emit zlib header and checksum according to RFC 1950. An RFC 1950
        // compatible ZLibStream class is available in .NET 6.0. To be compatible with earlier versions, but still make
        // use of the native zlib implementation used by .NET, this wrapper adds the zlib header and computes an
        // Adler32 checksum, but uses DeflateStream for the compression.
        //
        // Data format specification
        // https://datatracker.ietf.org/doc/html/rfc1950
        //
        // CINFO
        // Specifies the window size used for compression. The .NET DeflateStream class does not expose a setting for
        // controlling the window size during compression or decompression. By checking the source we can conclude that
        // DeflateStream always use window size 32K (2 ^ 15).
        //
        // .NET
        // https://github.com/dotnet/runtime/blob/5922e80f54fc3ec8ed9129a1e7f5edf93df06108/src/libraries/System.IO.Compression/src/System/IO/Compression/DeflateZLib/ZLibNative.cs#L115
        //
        // .NET Framework
        // https://github.com/microsoft/referencesource/blob/9da503f9ef21e8d1f2905c78d4e3e5cbb3d6f85a/System/InternalApis/NDP_FX/inc/ZLibNative.cs#L208
        //
        // This behavior is however not documented and it is never good to make assumptions about an implementation.
        //
        // This is what the zlib manual has to say about the window size for inflate:
        //
        //     "The default value is 15 if inflateInit is used instead. windowBits must be greater than or equal
        //      to the windowBits value provided to deflateInit2() while compressing"
        //
        // (source https://www.zlib.net/manual.html)
        //
        // Let's assume this is true for all decoders.
        //
        // COMPRESSION
        // During compression, it should be safe to always specify 32K as window size in the header. It will be
        // correct, according to the current .NET implementations, and even if .NET would lower the window size, the
        // stream should still decode fine, but consume more memory than necessary for the dictionary.
        //
        // DECOMPRESSION
        // We can read the window size from the header, but cannot feed the value to DeflateStream. However, as we saw
        // in the source, a 32K window is currently used. This is also the maximum allowed window, according to
        // RFC 1950 (section 2.2 CINFO). If Microsoft would lower the window size, they would break decoding all
        // streams produced by earlier versions of .NET. Because of this, we can assume that the window size for
        // decompression in DeflateStream will always be 32K or greater, and would thus be able to decode all window
        // sizes allowed by the RFC.

        private const byte CM_Deflate = 8;
        private const byte FLEVEL_Default = 2;
        private const byte CINFO_32K = 7;

        private readonly Adler32 adler = new Adler32();
        private readonly CompressionMode mode;
        private Stream baseStream;
        private Stream deflateStream;

        private bool checksumVerified;
        private bool leaveOpen;

        public ZLibStream(Stream stream, CompressionMode mode, bool leaveOpen = false)
        {
            this.mode = mode;
            this.baseStream = stream;
            this.leaveOpen = leaveOpen;

            if (mode == CompressionMode.Compress)
            {
                const byte cmf = (CINFO_32K << 4) | CM_Deflate;
                const byte flg = FLEVEL_Default << 6;
                const byte fcheck = (10000 * 31 - cmf * 256 - flg) % 31;
                
                stream.WriteByte(cmf);
                stream.WriteByte(flg | fcheck);

                // Important to leave the stream open, since we need to dispose the DeflateStream
                // before being able to write the checksum.
                deflateStream = new DeflateStream(stream, CompressionLevel.Optimal, true);
            }
            else
            {
                var remainingStreamLength = stream.Length - stream.Position;

                var cmf = stream.ReadByte();
                var flg = stream.ReadByte();

                if (cmf < 0 || flg < 0 || remainingStreamLength < 6)
                {
                    throw new InvalidDataException("Missing header or checksum in ZLib stream.");
                }

                var cm = cmf & 0xf;
                if (cm != CM_Deflate)
                {
                    throw new InvalidDataException("Unsupported compression algorithm in ZLib stream.");
                }

                var fdict = (flg >> 5) & 1;
                if (fdict != 0)
                {
                    throw new InvalidDataException("Unknown dictionary in ZLib stream.");
                }

                if (((cmf * 256 + flg) % 31) != 0)
                {
                    throw new InvalidDataException("Invalid ZLib header.");
                }

                var rawDeflateSlice = new StreamSlice(stream, remainingStreamLength - 6);
                deflateStream = new DeflateStream(rawDeflateSlice, CompressionMode.Decompress, true);
            }
        }

        public override bool CanRead => mode == CompressionMode.Decompress;

        public override bool CanSeek => false;

        public override bool CanWrite => mode == CompressionMode.Compress;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush()
        {
            deflateStream.Flush();
        }

        private void AfterRead(byte[] buffer, int offset, int read)
        {
            if (read > 0)
            {
                adler.Update(buffer, offset, read);
            }
            else if (!checksumVerified)
            {
                checksumVerified = true;

                var checksumBytes = new byte[4];

                baseStream.Seek(-4, SeekOrigin.End);
                read = baseStream.ReadAll(checksumBytes, 0, 4);

                if (read != 4)
                {
                    throw new InvalidDataException("Missing checksum in ZLib stream.");
                }
                else
                {
                    var checksum = unchecked((uint)(
                        (checksumBytes[0] << 24) |
                        (checksumBytes[1] << 16) |
                        (checksumBytes[2] << 8) |
                        (checksumBytes[3])));

                    if (checksum != adler.Value)
                    {
                        throw new InvalidDataException("Invalid checksum in Zlib stream.");
                    }
                }
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = 0;

            if (count > 0)
            {
                read = deflateStream.Read(buffer, offset, count);
                AfterRead(buffer, offset, read);
            }

            return read;
        }

        private class ReadAsyncResult : IAsyncResult
        {
            public IAsyncResult DeflateResult;

            public byte[] Buffer;
            public int Offset;
            public int Count;

            public object AsyncState => DeflateResult.AsyncState;
            public WaitHandle AsyncWaitHandle => DeflateResult.AsyncWaitHandle;
            public bool CompletedSynchronously => DeflateResult.CompletedSynchronously;
            public bool IsCompleted => DeflateResult.IsCompleted;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var result = new ReadAsyncResult
            {
                Buffer = buffer,
                Offset = offset,
                Count = count,
            };

            result.DeflateResult = deflateStream.BeginRead(buffer, offset, count, originalResult =>
            {
                result.DeflateResult = originalResult;

                if (callback != null)
                {
                    callback(result);
                }
            }, state);

            return result;
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            var readOperation = asyncResult as ReadAsyncResult;
            if (readOperation == null)
            {
                throw new ArgumentException("The IAsyncResult was not created by " + nameof(ZLibStream) + ".", nameof(asyncResult));
            }
            
            var read = deflateStream.EndRead(readOperation.DeflateResult);

            if (readOperation.Count > 0)
            {
                AfterRead(readOperation.Buffer, readOperation.Offset, read);
            }

            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var read = 0;

            if (count > 0)
            {
                read = await deflateStream.ReadAsync(buffer, offset, count, cancellationToken);
                AfterRead(buffer, offset, read);
            }

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            deflateStream.Write(buffer, offset, count);
            adler.Update(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (deflateStream != null)
                {
                    deflateStream.Dispose();
                    deflateStream = null;
                }

                if (baseStream != null)
                {
                    if (mode == CompressionMode.Compress)
                    {
                        baseStream.WriteBigEndian(adler.Value);
                    }

                    if (!leaveOpen)
                    {
                        baseStream.Dispose();
                    }

                    baseStream = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}
