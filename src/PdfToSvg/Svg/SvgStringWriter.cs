// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if HAVE_ASYNC && !NETCOREAPP1_0_OR_GREATER
using ValueTask = System.Threading.Tasks.Task;
#endif

namespace PdfToSvg.Svg
{
    internal sealed class SvgStringWriter : SvgTextWriter
    {
        private readonly StringBuilder sb = new StringBuilder(InitialBufferCapacity);

        public override bool ShouldFlush => false;

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected override void WriteAsciiCore(char ch)
        {
            sb.Append(buffer, 0, bufferSize);
            bufferSize = 0;

            buffer[bufferSize] = ch;
            bufferSize += 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected override void WriteAsciiCore(char ch1, char ch2)
        {
            sb.Append(buffer, 0, bufferSize);
            bufferSize = 0;

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

            sb.Append(buffer, 0, bufferSize);
            bufferSize = 0;

            // Might throw
            sb.Append(value, offset, count);
        }

        public override void Flush()
        {
            if (bufferSize > 0)
            {
                sb.Append(buffer, 0, bufferSize);
                bufferSize = 0;
            }
        }

#if HAVE_ASYNC
        public override async ValueTask WriteAsciiAsync(string value, int offset, int count, CancellationToken cancellationToken)
        {
            WriteAscii(value, offset, count);
        }

        public override async ValueTask FlushAsync(CancellationToken cancellationToken)
        {
            Flush();
        }
#endif

        public override string ToString()
        {
            Flush();
            return sb.ToString();
        }
    }
}
