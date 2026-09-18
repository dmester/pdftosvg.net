// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

#if NET8_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

#if HAVE_ASYNC && !NETCOREAPP1_0_OR_GREATER
using ValueTask = System.Threading.Tasks.Task;
#endif

namespace PdfToSvg.Svg
{
    internal sealed class SvgStreamWriter : SvgTextWriter
    {
        private const int StreamBufferCapacity = 8 << 10;

        private readonly bool preferAsync;

        private readonly Stream baseStream;
        private readonly byte[] streamBuffer = new byte[StreamBufferCapacity];

        public override bool ShouldFlush => bufferSize >= InitialBufferCapacity / 2;

        public SvgStreamWriter(Stream baseStream, bool preferAsync) : base(utf8Encode: true)
        {
            this.baseStream = baseStream;
            this.preferAsync = preferAsync;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected override void WriteAsciiCore(char ch)
        {
            if (preferAsync)
            {
                // Don't block with a sync call to baseStream.Write => instead increase buffer size as needed
                EnsureCapacity(1);
            }
            else
            {
                Flush();
            }

            buffer[bufferSize] = ch;
            bufferSize += 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected override void WriteAsciiCore(char ch1, char ch2)
        {
            if (preferAsync)
            {
                // Don't block with a sync call to baseStream.Write => instead increase buffer size as needed
                EnsureCapacity(2);
            }
            else
            {
                Flush();
            }

            buffer[bufferSize + 0] = ch1;
            buffer[bufferSize + 1] = ch2;
            bufferSize += 2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected override void WriteAsciiCore(string value, int offset, int count)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (offset < 0 || offset > value.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (count < 0 || count > value.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (preferAsync)
            {
                // Don't block with a sync call to baseStream.Write => instead increase buffer size as needed
                EnsureCapacity(count);
            }
            else
            {
                Flush();

                if (count > buffer.Length / 2)
                {
                    for (var cursor = 0; cursor < count;)
                    {
                        var batchSize = Math.Min(streamBuffer.Length, count - cursor);
                        NarrowToAscii(value, offset + cursor, streamBuffer, batchSize);
                        baseStream.Write(streamBuffer, 0, batchSize);
                        cursor += batchSize;
                    }

                    return;
                }
            }

            value.CopyTo(offset, buffer, bufferSize, count);
            bufferSize += count;
        }

#if HAVE_ASYNC
        public override async ValueTask WriteAsciiAsync(string value, int offset, int count, CancellationToken cancellationToken)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (offset < 0 || offset > value.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (count < 0 || count > value.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (count > buffer.Length - bufferSize)
            {
                await FlushAsync(cancellationToken).ConfigureAwait(false);

                if (count > buffer.Length / 2)
                {
                    for (var cursor = 0; cursor < count;)
                    {
                        var batchSize = Math.Min(streamBuffer.Length, count - cursor);

                        NarrowToAscii(value, offset + cursor, streamBuffer, batchSize);
                        await baseStream
                            .WriteAsync(streamBuffer, 0, batchSize, cancellationToken)
                            .ConfigureAwait(false);

                        cursor += batchSize;
                    }

                    return;
                }
            }

            value.CopyTo(offset, buffer, bufferSize, count);
            bufferSize += count;
        }
#endif

        public override void Flush()
        {
            var buffer = this.buffer;
            var bufferSize = this.bufferSize;
            var streamBuffer = this.streamBuffer;

            for (var cursor = 0; cursor < bufferSize;)
            {
                var batchSize = Math.Min(streamBuffer.Length, bufferSize - cursor);
                NarrowToAscii(buffer, cursor, streamBuffer, batchSize);
                baseStream.Write(streamBuffer, 0, batchSize);
                cursor += batchSize;
            }

            this.bufferSize = 0;
        }

#if HAVE_ASYNC
        public override async ValueTask FlushAsync(CancellationToken cancellationToken)
        {
            var buffer = this.buffer;
            var bufferSize = this.bufferSize;
            var streamBuffer = this.streamBuffer;

            for (var cursor = 0; cursor < bufferSize;)
            {
                var batchSize = Math.Min(streamBuffer.Length, bufferSize - cursor);
                NarrowToAscii(buffer, cursor, streamBuffer, batchSize);
                await baseStream.WriteAsync(streamBuffer, 0, batchSize, cancellationToken).ConfigureAwait(false);
                cursor += batchSize;
            }

            this.bufferSize = 0;
        }
#endif

        private static void NarrowToAscii(char[] charBuffer, int charBufferOffset, byte[] streamBuffer, int count)
        {
            var batchCursor = 0;

#if NET8_0_OR_GREATER
            batchCursor = NarrowToAscii_Intrinsics(charBuffer, charBufferOffset, streamBuffer, count);
#endif

            for (; batchCursor < count; batchCursor++)
            {
                streamBuffer[batchCursor] = (byte)charBuffer[charBufferOffset + batchCursor];
            }
        }

        private static void NarrowToAscii(string source, int sourceOffset, byte[] streamBuffer, int count)
        {
            var batchCursor = 0;

#if NET8_0_OR_GREATER
            batchCursor = NarrowToAscii_Intrinsics(source, sourceOffset, streamBuffer, count);
#endif

            for (; batchCursor < count; batchCursor++)
            {
                streamBuffer[batchCursor] = (byte)source[sourceOffset + batchCursor];
            }
        }

#if NET8_0_OR_GREATER
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static int NarrowToAscii_Intrinsics(ReadOnlySpan<char> source, int sourceOffset, byte[] streamBuffer, int count)
        {
            var batchCursor = 0;

            if (Vector256.IsHardwareAccelerated && Avx2.IsSupported)
            {
                var acceleratedChars = (count / Vector256<byte>.Count) * Vector256<byte>.Count;

                if (acceleratedChars > 0)
                {
                    ref var pChars = ref Unsafe.Add(
                        ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(source)), sourceOffset);
                    ref var pBuffer = ref MemoryMarshal.GetArrayDataReference(streamBuffer);

                    for (var i = 0; i < acceleratedChars; i += Vector256<byte>.Count)
                    {
                        var chunk1 = Vector256.LoadUnsafe(ref pChars, (nuint)i);
                        var chunk2 = Vector256.LoadUnsafe(ref pChars, (nuint)(i + Vector256<short>.Count));

                        // VPACKUSWB packs within each 128 bit lane, producing the packed qwords in the order
                        // (a0, b0, a1, b1). A permute restores linear order.
                        var bytes = Avx2.Permute4x64(
                            Avx2.PackUnsignedSaturate(chunk1, chunk2).AsInt64(),
                            0b_11_01_10_00).AsByte();

                        bytes.StoreUnsafe(ref pBuffer, (nuint)i);
                    }

                    batchCursor += acceleratedChars;
                }
            }
            else if (Vector128.IsHardwareAccelerated && Sse2.IsSupported)
            {
                var acceleratedChars = (count / Vector128<byte>.Count) * Vector128<byte>.Count;

                if (acceleratedChars > 0)
                {
                    ref var pChars = ref Unsafe.Add(
                        ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(source)), sourceOffset);
                    ref var pBuffer = ref MemoryMarshal.GetArrayDataReference(streamBuffer);

                    for (var i = 0; i < acceleratedChars; i += Vector128<byte>.Count)
                    {
                        var chunk1 = Vector128.LoadUnsafe(ref pChars, (nuint)i);
                        var chunk2 = Vector128.LoadUnsafe(ref pChars, (nuint)(i + Vector128<short>.Count));

                        // PACKUSWB packs the two vectors in linear order
                        var bytes = Sse2.PackUnsignedSaturate(chunk1, chunk2);

                        bytes.StoreUnsafe(ref pBuffer, (nuint)i);
                    }

                    batchCursor += acceleratedChars;
                }
            }

            return batchCursor;
        }
#endif

    }
}
