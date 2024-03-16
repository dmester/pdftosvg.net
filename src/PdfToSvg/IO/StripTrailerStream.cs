// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
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
    /// <summary>
    /// Prevents the last N bytes from a stream from being consumed.
    /// </summary>
    internal class StripTrailerStream : Stream
    {
        private Stream? baseStream;
        private bool leaveOpen;
        private readonly int trailerLength;
        private readonly byte[] trailerBuffer;
        private int trailerBufferLength;
        private long position;

        public StripTrailerStream(Stream baseStream, int trailerLength, bool leaveOpen = false)
        {
            if (baseStream == null) throw new ArgumentNullException(nameof(baseStream));
            if (trailerLength < 1) throw new ArgumentOutOfRangeException(nameof(trailerLength));

            this.baseStream = baseStream;
            this.trailerLength = trailerLength;
            this.leaveOpen = leaveOpen;

            // Allocate a larger buffer than needed so that Read can use it instead of the argument buffer, in case it is too small
            this.trailerBuffer = new byte[trailerLength * 2];
        }

        public override bool CanRead => baseStream != null && baseStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => baseStream == null ? 0 : Math.Max(0, baseStream.Length - trailerLength);

        public override long Position { get => position; set => throw new NotSupportedException(); }

        public override void Flush() { }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static void LoopCopy(byte[] src, int srcOffset, byte[] dst, int dstOffset, int count)
        {
            for (var i = 0; i < count; i++)
            {
                dst[dstOffset + i] = src[srcOffset + i];
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(count));
            if (offset < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (baseStream == null) throw new ObjectDisposedException(nameof(StripTrailerStream));

            int returnBytes;

            if (count == 0)
            {
                returnBytes = 0;
            }
            else if (count <= trailerLength)
            {
                // The input buffer is too small to fit both the desired bytes and the trailer.
                // Read to the trailer buffer instead.
                var read = trailerLength + count - trailerBufferLength;

                trailerBufferLength += baseStream.ReadAll(trailerBuffer, trailerBufferLength, read);

                returnBytes = trailerBufferLength - trailerLength;

                if (returnBytes > 0)
                {
                    LoopCopy(trailerBuffer, 0, buffer, offset, returnBytes);
                    LoopCopy(trailerBuffer, returnBytes, trailerBuffer, 0, trailerLength);
                    trailerBufferLength -= returnBytes;
                }
                else
                {
                    returnBytes = 0;
                }
            }
            else
            {
                // Read to the input buffer, but strip the potential trailer off. The caller will always get a little 
                // less bytes than requested.
                LoopCopy(trailerBuffer, 0, buffer, offset, trailerBufferLength);

                var cursor = trailerBufferLength;
                if (cursor < count)
                {
                    cursor += baseStream.ReadAll(buffer, offset + cursor, count - cursor);
                }

                trailerBufferLength = Math.Min(cursor, trailerLength);

                LoopCopy(buffer, offset + cursor - trailerBufferLength, trailerBuffer, 0, trailerBufferLength);

                returnBytes = cursor - trailerBufferLength;
            }

            position += returnBytes;
            return returnBytes;
        }

#if HAVE_ASYNC
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(count));
            if (offset < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (baseStream == null) throw new ObjectDisposedException(nameof(StripTrailerStream));

            int returnBytes;

            if (count == 0)
            {
                returnBytes = 0;
            }
            else if (count <= trailerLength)
            {
                // The input buffer is too small to fit both the desired bytes and the trailer.
                // Read to the trailer buffer instead.
                var read = trailerLength + count - trailerBufferLength;

                trailerBufferLength += await baseStream.ReadAllAsync(trailerBuffer, trailerBufferLength, read, cancellationToken).ConfigureAwait(false);

                returnBytes = trailerBufferLength - trailerLength;

                if (returnBytes > 0)
                {
                    LoopCopy(trailerBuffer, 0, buffer, offset, returnBytes);
                    LoopCopy(trailerBuffer, returnBytes, trailerBuffer, 0, trailerLength);
                    trailerBufferLength -= returnBytes;
                }
                else
                {
                    returnBytes = 0;
                }
            }
            else
            {
                // Read to the input buffer, but strip the potential trailer off. The caller will always get a little 
                // less bytes than requested.
                LoopCopy(trailerBuffer, 0, buffer, offset, trailerBufferLength);

                var cursor = trailerBufferLength;
                if (cursor < count)
                {
                    cursor += await baseStream.ReadAllAsync(buffer, offset + cursor, count - cursor, cancellationToken).ConfigureAwait(false);
                }

                trailerBufferLength = Math.Min(cursor, trailerLength);

                LoopCopy(buffer, offset + cursor - trailerBufferLength, trailerBuffer, 0, trailerBufferLength);

                returnBytes = cursor - trailerBufferLength;
            }

            position += returnBytes;
            return returnBytes;
        }

#if HAVE_STREAM_BEGINEND
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return TaskAsyncResult<StripTrailerStream, int>.Begin(ReadAsync(buffer, offset, count), callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return TaskAsyncResult<StripTrailerStream, int>.End(asyncResult);
        }
#endif
#endif

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public byte[] GetTrailer()
        {
            return trailerBuffer.Slice(0, trailerBufferLength);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && baseStream != null)
            {
                if (!leaveOpen)
                {
                    baseStream.Dispose();
                }

                baseStream = null;
            }
        }
    }
}
