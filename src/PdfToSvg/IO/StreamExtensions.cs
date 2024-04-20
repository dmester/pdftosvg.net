// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.IO
{
    internal static class StreamExtensions
    {
        // Same as in .NET
        private const int DefaultCopyToBuffer = 81920;

        public static uint ReadCompactUInt32(this BinaryReader reader)
        {
            // Basically the same as Read7BitEncodedInt, but available for all .NET versions

            var result = 0u;
            var shift = 0;
            byte b;

            do
            {
                b = reader.ReadByte();
                result |= ((uint)b & 0x7f) << shift;
                shift += 7;
            }
            while ((b & 0x80) != 0);

            return result;
        }

        public static void WriteCompactUInt32(this BinaryWriter writer, uint value)
        {
            // Basically the same as Write7BitEncodedInt, but available for all .NET versions

            while (value > 0x7f)
            {
                writer.Write((byte)(value | 0x80));
                value >>= 7;
            }

            writer.Write((byte)value);
        }

        public static void CopyTo(this Stream stream, Stream destination, CancellationToken cancellationToken)
        {
            var buffer = new byte[DefaultCopyToBuffer];

            int read;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                read = stream.Read(buffer, 0, DefaultCopyToBuffer);
                destination.Write(buffer, 0, read);
            }
            while (read > 0);
        }

#if HAVE_ASYNC
        public static Task CopyToAsync(this Stream stream, Stream destination, CancellationToken cancellationToken)
        {
            return stream.CopyToAsync(destination, DefaultCopyToBuffer, cancellationToken);
        }
#endif

        public static int ReadAll(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            var totalRead = 0;
            int read;

            do
            {
                read = stream.Read(buffer, offset, count);
                offset += read;
                count -= read;
                totalRead += read;

                cancellationToken.ThrowIfCancellationRequested();
            }
            while (read > 0);

            return totalRead;
        }

#if HAVE_ASYNC
        public static async Task<int> ReadAllAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            var totalRead = 0;
            int read;

            do
            {
                read = await stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                offset += read;
                count -= read;
                totalRead += read;
            }
            while (read > 0);

            return totalRead;
        }
#endif

        public static MemoryStream ToMemoryStream(this Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream.CanSeek)
            {
                var length = stream.Length - stream.Position;

                if (length > int.MaxValue)
                {
                    throw new ArgumentException("The stream is too large to be converted to a MemoryStream.", nameof(stream));
                }

                var buffer = new byte[(int)length];
                var read = stream.ReadAll(buffer, 0, buffer.Length, cancellationToken);
                return new MemoryStream(buffer, 0, read);
            }
            else
            {
                var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream, cancellationToken);
                memoryStream.Position = 0;
                return memoryStream;
            }
        }

#if HAVE_ASYNC
        public static async Task<MemoryStream> ToMemoryStreamAsync(this Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream.CanSeek)
            {
                var length = stream.Length - stream.Position;

                if (length > int.MaxValue)
                {
                    throw new ArgumentException("The stream is too large to be converted to a MemoryStream.", nameof(stream));
                }

                var buffer = new byte[(int)length];
                var read = await stream.ReadAllAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                return new MemoryStream(buffer, 0, read);
            }
            else
            {
                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
                memoryStream.Position = 0;
                return memoryStream;
            }
        }
#endif

        /// <summary>
        /// Reads the stream to a byte array. If the stream is seekable, it will first be rewinded
        /// to its start.
        /// </summary>
        public static byte[] ToArray(this Stream stream)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;

                var length = stream.Length;

                if (length > int.MaxValue)
                {
                    throw new ArgumentException("The stream is too large to be converted to an array.", nameof(stream));
                }

                var buffer = new byte[(int)length];
                var read = stream.ReadAll(buffer, 0, buffer.Length);
                if (read == buffer.Length)
                {
                    return buffer;
                }

                var newBuffer = new byte[read];
                Buffer.BlockCopy(buffer, 0, newBuffer, 0, read);
                return newBuffer;
            }
            else
            {
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        public static void Skip(this Stream stream, long offset)
        {
            if (stream.CanSeek)
            {
                stream.Seek(offset, SeekOrigin.Current);
            }
            else
            {
                var buffer = new byte[Math.Min(offset, 1024)];
                var read = 1;

                while (offset > 0 && read > 0)
                {
                    read = stream.Read(buffer, 0, (int)Math.Min(offset, buffer.Length));
                    offset -= read;
                }
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static byte[] GetBufferOrArray(this MemoryStream stream)
        {
#if NETSTANDARD1_6
            return stream.ToArray();
#else
            return stream.GetBuffer();
#endif
        }

        /// <summary>
        /// Writes all bytes from a specified buffer to the stream.
        /// </summary>
        /// <param name="stream">Target stream.</param>
        /// <param name="buffer">Buffer whose content will be written to the stream.</param>
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void Write(this Stream stream, byte[] buffer)
        {
            stream.Write(buffer, 0, buffer.Length);
        }

#if HAVE_ASYNC
        /// <summary>
        /// Asynchronously writes all bytes from a specified buffer to the stream.
        /// </summary>
        /// <param name="stream">Target stream.</param>
        /// <param name="buffer">Buffer whose content will be written to the stream.</param>
        /// <param name="cancellationToken">Token for monitoring cancellation requests.</param>
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static Task WriteAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken = default)
        {
            return stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
        }
#endif

        /// <summary>
        /// Writes an <see cref="int"/> in big-endian format to the stream.
        /// </summary>
        public static void WriteBigEndian(this Stream stream, int value)
        {
            WriteBigEndian(stream, unchecked((uint)value));
        }

        /// <summary>
        /// Writes an <see cref="uint"/> in big-endian format to the stream.
        /// </summary>
        public static void WriteBigEndian(this Stream stream, uint uvalue)
        {
            var values = new[]
            {
                (byte)(uvalue >> 24),
                (byte)((uvalue >> 16) & 0xff),
                (byte)((uvalue >> 8) & 0xff),
                (byte)(uvalue & 0xff)
            };
            stream.Write(values, 0, 4);
        }
    }
}
