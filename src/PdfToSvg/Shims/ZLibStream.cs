// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

#if !NET6_0_OR_GREATER
using PdfToSvg.Common;
using PdfToSvg.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    /// <summary>
    /// Compresses or decompresses an RFC 1950 compatible zlib/deflate stream.
    /// </summary>
    /// <remarks>
    /// The .NET 6 implementation will probably not be usable, since we need to suppress incorrect checksum errors.
    /// </remarks>
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

        private Adler32 adler = new();
        private readonly CompressionMode mode;

        private Stream? baseStream;
        private Stream? deflateStream;

        private bool endOfStream;
        private bool leaveOpen;

        /// <summary>
        /// Computes Adler32 checksums. Implementation based on https://tools.ietf.org/html/rfc1950 section 2.2.
        /// </summary>
        private struct Adler32
        {
            private uint s1;
            private uint s2;

            public uint Value => (s2 << 16) + s1;

            public void Initialize()
            {
                s1 = 1;
                s2 = 0;
            }

            public void Update(byte[] buffer, int offset, int count)
            {
                const uint Modulo = 65521;

                var s1 = this.s1;
                var s2 = this.s2;

                for (var i = 0; i < count; i++)
                {
                    s1 += buffer[offset + i];

                    if (s2 + s1 < s2)
                    {
                        // Addition would cause overflow
                        s1 = s1 % Modulo;
                        s2 = s2 % Modulo;
                    }

                    s2 += s1;
                }

                this.s1 = s1 % Modulo;
                this.s2 = s2 % Modulo;
            }
        }

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
                deflateStream = new DeflateStream(stream, CompressionMode.Compress, leaveOpen: true);

                adler.Initialize();
            }
            else
            {
                var cmf = stream.ReadByte();
                var flg = stream.ReadByte();

                if (cmf < 0 || flg < 0)
                {
                    endOfStream = true;
                    return;
                }
                ZLibFactory.VerifyHeader(cmf, flg);

                deflateStream = new DeflateStream(stream, CompressionMode.Decompress, leaveOpen: true);
            }
        }

        public override bool CanRead => mode == CompressionMode.Decompress;

        public override bool CanSeek => false;

        public override bool CanWrite => mode == CompressionMode.Compress;

        public override long Length => throw new NotSupportedException();

        public override long Position 
        {
            get
            {
                var deflateStream = this.deflateStream;

                if (deflateStream == null)
                {
                    throw new ObjectDisposedException(nameof(ZLibStream));
                }

                return deflateStream.Position;
            }
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            if (deflateStream == null)
            {
                throw new ObjectDisposedException(nameof(ZLibStream));
            }

            deflateStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (deflateStream == null)
            {
                throw new ObjectDisposedException(nameof(ZLibStream));
            }

            if (endOfStream)
            {
                return 0;
            }
            else
            {
                return deflateStream.Read(buffer, offset, count);
            }
        }

#if HAVE_ASYNC
#if HAVE_STREAM_BEGINEND
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return TaskAsyncResult<ZLibStream, int>.Begin(ReadAsync(buffer, offset, count), callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return TaskAsyncResult<ZLibStream, int>.End(asyncResult);
        }
#endif

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (deflateStream == null)
            {
                throw new ObjectDisposedException(nameof(ZLibStream));
            }

            if (endOfStream)
            {
                return Task.FromResult(0);
            }
            else
            {
                return deflateStream.ReadAsync(buffer, offset, count, cancellationToken);
            }
        }
#endif

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (deflateStream == null)
            {
                throw new ObjectDisposedException(nameof(ZLibStream));
            }

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
#endif
