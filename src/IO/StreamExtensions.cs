// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.IO
{
    internal static class StreamExtensions
    {
        // Same as in .NET
        private const int DefaultCopyToBuffer = 81920;

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

        public static Task CopyToAsync(this Stream stream, Stream destination, CancellationToken cancellationToken)
        {
            return stream.CopyToAsync(destination, DefaultCopyToBuffer, cancellationToken);
        }

        public static int ReadAll(this Stream stream, byte[] buffer, int offset, int count)
        {
            var totalRead = 0;
            int read;

            do
            {
                read = stream.Read(buffer, offset, count);
                offset += read;
                count -= read;
                totalRead += read;
            }
            while (read > 0);

            return totalRead;
        }

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
