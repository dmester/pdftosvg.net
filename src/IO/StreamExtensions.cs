using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.IO
{
    // TODO Add tests
    internal static class StreamExtensions
    {

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

        public static async Task<int> ReadAllAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var totalRead = 0;
            int read;

            do
            {
                read = await stream.ReadAsync(buffer, offset, count, cancellationToken);
                offset += read;
                count -= read;
                totalRead += read;
            }
            while (read > 0);

            return totalRead;
        }

        public static byte[] ToArray(this IEnumerable<Func<Stream>> streamFactories)
        {
            var chunks = new List<byte[]>();
            var totalBytes = 0;
            var bytesThisIteration = 0;

            foreach (var streamFactory in streamFactories)
            {
                using (var stream = streamFactory())
                {
                    do
                    {
                        var chunk = new byte[4096];
                        bytesThisIteration = stream.Read(chunk, 0, chunk.Length);
                        totalBytes += bytesThisIteration;
                        chunks.Add(chunk);
                    }
                    while (bytesThisIteration > 0);
                }
            }

            var result = new byte[totalBytes];
            var resultCursor = 0;

            foreach (var chunk in chunks)
            {
                bytesThisIteration = Math.Min(totalBytes - resultCursor, chunk.Length);
                Buffer.BlockCopy(chunk, 0, result, resultCursor, bytesThisIteration);
                resultCursor += bytesThisIteration;
            }

            return result;
        }

        public static async Task<byte[]> ToArrayAsync(this IEnumerable<Func<Stream>> streamFactories)
        {
            var chunks = new List<byte[]>();
            var totalBytes = 0;
            var bytesThisIteration = 0;

            foreach (var streamFactory in streamFactories)
            {
                using (var stream = streamFactory())
                {
                    do
                    {
                        var chunk = new byte[4096];
                        bytesThisIteration = await stream.ReadAsync(chunk, 0, chunk.Length);
                        totalBytes += bytesThisIteration;
                        chunks.Add(chunk);
                    }
                    while (bytesThisIteration > 0);
                }
            }

            var result = new byte[totalBytes];
            var resultCursor = 0;

            foreach (var chunk in chunks)
            {
                bytesThisIteration = Math.Min(totalBytes - resultCursor, chunk.Length);
                Buffer.BlockCopy(chunk, 0, result, resultCursor, bytesThisIteration);
                resultCursor += bytesThisIteration;
            }

            return result;
        }

        public static byte[] ToArray(this Stream stream)
        {
            if (stream is MemoryStream memStream)
            {
                return memStream.ToArray();
            }

            var chunks = new List<byte[]>();
            var totalBytes = 0;
            var bytesThisIteration = 0;

            do
            {
                var chunk = new byte[4096];
                bytesThisIteration = stream.Read(chunk, 0, chunk.Length);
                totalBytes += bytesThisIteration;
                chunks.Add(chunk);
            }
            while (bytesThisIteration > 0);

            var result = new byte[totalBytes];
            var resultCursor = 0;

            foreach (var chunk in chunks)
            {
                bytesThisIteration = Math.Min(totalBytes - resultCursor, chunk.Length);
                Buffer.BlockCopy(chunk, 0, result, resultCursor, bytesThisIteration);
                resultCursor += bytesThisIteration;
            }

            return result;
        }

        public static async Task<byte[]> ToArrayAsync(this Stream stream)
        {
            if (stream is MemoryStream memStream)
            {
                return memStream.ToArray();
            }

            var chunks = new List<byte[]>();
            var totalBytes = 0;
            var bytesThisIteration = 0;

            do
            {
                var chunk = new byte[4096];
                bytesThisIteration = await stream.ReadAsync(chunk, 0, chunk.Length);
                totalBytes += bytesThisIteration;
                chunks.Add(chunk);
            }
            while (bytesThisIteration > 0);

            var result = new byte[totalBytes];
            var resultCursor = 0;

            foreach (var chunk in chunks)
            {
                bytesThisIteration = Math.Min(totalBytes - resultCursor, chunk.Length);
                Buffer.BlockCopy(chunk, 0, result, resultCursor, bytesThisIteration);
                resultCursor += bytesThisIteration;
            }

            return result;
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
