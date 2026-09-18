// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Encodings;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

#if HAVE_ASYNC && !NETCOREAPP1_0_OR_GREATER
using ValueTask = System.Threading.Tasks.Task;
#endif

namespace PdfToSvg.Svg
{
    internal abstract class SvgTextWriter(bool utf8Encode = false) : IDisposable
    {
        protected const int InitialBufferCapacity = 4 << 10;
        private const int MaxUtf8CodeLength = 4;

        // In stream mode, non-ASCII chars are stored as UTF-8 encoded bytes in the buffer, one char per byte, allowing
        // Flush to narrow each char to a byte without further encoding. In string mode the buffer holds plain chars.
        protected char[] buffer = new char[InitialBufferCapacity];
        protected int bufferSize;

        public int AsyncLengthThreshold => (buffer.Length - bufferSize) / 2;

        public abstract bool ShouldFlush { get; }

        public void AppendXmlDeclaration()
        {
            WriteAscii("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
        }

        // The intention is that the public WriteAscii methods should be as slim as possible to be inlined and only
        // contain the absolute minimum for the common execution path.

        /// <summary>
        /// Writes a single character without XML escaping. Caller is responsible for only using ASCII characters.
        /// </summary>
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void WriteAscii(char ch)
        {
            var bufferSize = this.bufferSize;
            var buffer = this.buffer;

            // The uint cast ensures the JIT does not need to do a range check in the array access
            if ((uint)bufferSize < (uint)buffer.Length)
            {
                buffer[bufferSize] = ch;
                this.bufferSize = bufferSize + 1;
            }
            else
            {
                WriteAsciiCore(ch);
            }
        }

        /// <summary>
        /// Writes two characters without XML escaping. Caller is responsible for only using ASCII characters.
        /// </summary>
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void WriteAscii(char ch1, char ch2)
        {
            var buffer = this.buffer;

            var index0 = this.bufferSize;
            var index1 = index0 + 1;

            // The uint casts ensures the JIT does not need to do a range check in the array accesses
            if ((uint)index0 < (uint)buffer.Length &&
                (uint)index1 < (uint)buffer.Length)
            {
                buffer[index0] = ch1;
                buffer[index1] = ch2;
                this.bufferSize = index0 + 2;
            }
            else
            {
                WriteAsciiCore(ch1, ch2);
            }
        }

        /// <summary>
        /// Writes a string without XML escaping. Caller is responsible for only using ASCII characters.
        /// </summary>
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void WriteAscii(string value)
        {
            WriteAscii(value, 0, value.Length);
        }

        /// <summary>
        /// Writes a string without XML escaping. Caller is responsible for only using ASCII characters.
        /// </summary>
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void WriteAscii(string value, int offset, int count)
        {
            var bufferSize = this.bufferSize;
            var buffer = this.buffer;

            // The conditions matches the conditions in string.CopyTo =>
            // the conditions in CopyTo are eliminated during JIT compilation
            if (count >= 0 &&
                offset >= 0 &&
                count <= value.Length - offset &&
                bufferSize <= buffer.Length - count &&
                bufferSize >= 0)
            {
                value.CopyTo(offset, buffer, bufferSize, count);
                this.bufferSize = bufferSize + count;
            }
            else
            {
                WriteAsciiCore(value, offset, count);
            }
        }

        /// <summary>
        /// Writes text without XML escaping. Non-ASCII chars are supported.
        /// </summary>
        public void WriteRawText(string text)
        {
            if (utf8Encode)
            {
                var runStart = 0;

                for (var i = 0; i < text.Length; i++)
                {
                    if (text[i] >= '\u0080')
                    {
                        WriteAscii(text, runStart, i - runStart);
                        AppendUtf8Char(text, ref i);
                        runStart = i--;
                    }
                }

                WriteAscii(text, runStart, text.Length - runStart);
            }
            else
            {
                WriteAscii(text);
            }
        }

#if HAVE_ASYNC
        /// <summary>
        /// Writes a string without XML escaping. Caller is responsible for only using ASCII characters.
        /// </summary>
        public abstract ValueTask WriteAsciiAsync(string value, int offset, int count, CancellationToken cancellationToken);

        /// <summary>
        /// Writes text without XML escaping. Non-ASCII chars are supported.
        /// </summary>
        public async ValueTask WriteRawTextAsync(string text, CancellationToken cancellationToken)
        {
            if (utf8Encode)
            {
                var runStart = 0;

                for (var i = 0; i < text.Length; i++)
                {
                    if (text[i] >= '\u0080')
                    {
                        await WriteAsciiAsync(text, runStart, i - runStart, cancellationToken).ConfigureAwait(false);

                        if (bufferSize + MaxUtf8CodeLength >= buffer.Length)
                        {
                            await FlushAsync(cancellationToken).ConfigureAwait(false);
                        }

                        AppendUtf8Char(text, ref i);
                        runStart = i--;
                    }
                }

                await WriteAsciiAsync(text, runStart, text.Length - runStart, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await WriteAsciiAsync(text, 0, text.Length, cancellationToken).ConfigureAwait(false);
            }
        }
#endif

        protected abstract void WriteAsciiCore(char ch);
        protected abstract void WriteAsciiCore(char ch1, char ch2);
        protected abstract void WriteAsciiCore(string value, int offset, int count);
        
        public abstract void Flush();

#if HAVE_ASYNC
        public abstract ValueTask FlushAsync(CancellationToken cancellationToken);
#endif

        /// <summary>
        /// Writes text with XML escaping. Non-ASCII chars are supported.
        /// </summary>
        public void WriteEncodedText(string text, bool escapeQuote)
        {
            var runStart = 0;

            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                string entity;

                if (ch == '&')
                {
                    entity = "&amp;";
                }
                else if (ch == '<')
                {
                    entity = "&lt;";
                }
                else if (ch == '>')
                {
                    entity = "&gt;";
                }
                else if (ch == '"' && escapeQuote)
                {
                    entity = "&quot;";
                }
                else if (ch >= '\u0080' && utf8Encode)
                {
                    WriteAscii(text, runStart, i - runStart);
                    AppendUtf8Char(text, ref i);
                    runStart = i--;
                    continue;
                }
                else
                {
                    continue;
                }

                WriteAscii(text, runStart, i - runStart);
                WriteAscii(entity, 0, entity.Length);
                runStart = i + 1;
            }

            WriteAscii(text, runStart, text.Length - runStart);
        }

#if HAVE_ASYNC
        /// <summary>
        /// Writes text with XML escaping. Non-ASCII chars are supported.
        /// </summary>
        public async ValueTask WriteEncodedTextAsync(string text, bool escapeQuote, CancellationToken cancellationToken)
        {
            var runStart = 0;

            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                string entity;

                if (ch == '&')
                {
                    entity = "&amp;";
                }
                else if (ch == '<')
                {
                    entity = "&lt;";
                }
                else if (ch == '>')
                {
                    entity = "&gt;";
                }
                else if (ch == '"' && escapeQuote)
                {
                    entity = "&quot;";
                }
                else if (ch >= '\u0080' && utf8Encode)
                {
                    await WriteAsciiAsync(text, runStart, i - runStart, cancellationToken).ConfigureAwait(false);

                    if (bufferSize + MaxUtf8CodeLength >= buffer.Length)
                    {
                        await FlushAsync(cancellationToken).ConfigureAwait(false);
                    }

                    AppendUtf8Char(text, ref i);
                    runStart = i--;
                    continue;
                }
                else
                {
                    continue;
                }

                await WriteAsciiAsync(text, runStart, i - runStart, cancellationToken).ConfigureAwait(false);
                await WriteAsciiAsync(entity, 0, entity.Length, cancellationToken).ConfigureAwait(false);
                runStart = i + 1;
            }

            await WriteAsciiAsync(text, runStart, text.Length - runStart, cancellationToken).ConfigureAwait(false);
        }
#endif

        private void AppendUtf8Char(string text, ref int index)
        {
            var codePoint = Utf16Encoding.DecodeCodePoint(text, index, out var codePointLength);
            index += codePointLength;

            EnsureCapacity(4);

            var buffer = this.buffer;
            var bufferSize = this.bufferSize;

            if (codePoint < 0x800)
            {
                buffer[bufferSize + 0] = (char)(0xC0 | (codePoint >> 6));
                buffer[bufferSize + 1] = (char)(0x80 | (codePoint & 0x3F));
                bufferSize += 2;
            }
            else if (codePoint < 0x10000)
            {
                buffer[bufferSize + 0] = (char)(0xE0 | (codePoint >> 12));
                buffer[bufferSize + 1] = (char)(0x80 | ((codePoint >> 6) & 0x3F));
                buffer[bufferSize + 2] = (char)(0x80 | (codePoint & 0x3F));
                bufferSize += 3;
            }
            else
            {
                buffer[bufferSize + 0] = (char)(0xF0 | (codePoint >> 18));
                buffer[bufferSize + 1] = (char)(0x80 | ((codePoint >> 12) & 0x3F));
                buffer[bufferSize + 2] = (char)(0x80 | ((codePoint >> 6) & 0x3F));
                buffer[bufferSize + 3] = (char)(0x80 | (codePoint & 0x3F));
                bufferSize += 4;
            }

            this.bufferSize = bufferSize;
        }

        protected void EnsureCapacity(int length)
        {
            if (bufferSize + length > buffer.Length)
            {
                var newCapacity = Math.Max(bufferSize + length + 1024, buffer.Length * 2);
                var newBuffer = new char[newCapacity];
                Array.Copy(buffer, 0, newBuffer, 0, bufferSize);
                buffer = newBuffer;
            }
        }

        public void Dispose()
        {
            Flush();
        }
    }
}
