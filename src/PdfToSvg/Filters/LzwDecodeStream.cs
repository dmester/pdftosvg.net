// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Filters
{
    internal class LzwDecodeStream : DecodeStream
    {
        // PDF spec 1.7, 7.4.4.2, page 34
        //
        // Implemented based on algorithm description at:
        // https://marknelson.us/posts/1989/10/01/lzw-data-compression.html

        private readonly List<byte[]> dictionary = new List<byte[]>();

        private readonly BitReader reader;
        private readonly uint[] codeBuffer = new uint[1];

        private byte[] sequence = ArrayUtils.Empty<byte>();
        private int sequenceCursor;

        private readonly bool earlyChange;

        private const int BufferSize = 2048;
        private const int ClearTable = 256;
        private const int EndOfDecode = 257;
        private const int FirstDictionaryKey = 258;

        public LzwDecodeStream(Stream stream, bool earlyChange)
        {
            this.earlyChange = earlyChange;
            buffer = new byte[BufferSize];
            reader = new BitReader(stream, bitsPerValue: 9, BufferSize);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                reader.Dispose();
            }
        }

        protected override void FillBuffer()
        {
            bufferCursor = 0;
            bufferLength = 0;

            while (bufferLength < buffer.Length && !endOfStream)
            {
                if (sequenceCursor < sequence.Length)
                {
                    var readFromSequence = Math.Min(sequence.Length - sequenceCursor, buffer.Length - bufferLength);
                    Buffer.BlockCopy(sequence, sequenceCursor, buffer, bufferLength, readFromSequence);
                    bufferLength += readFromSequence;
                    sequenceCursor += readFromSequence;
                    continue;
                }

                if (reader.Read(codeBuffer, 0, 1) == 0)
                {
                    endOfStream = true;
                    break;
                }

                var code = codeBuffer[0];

                if (code == ClearTable)
                {
                    dictionary.Clear();
                    reader.BitsPerValue = 9;
                    sequence = ArrayUtils.Empty<byte>();
                }
                else if (code == EndOfDecode)
                {
                    endOfStream = true;
                    break;
                }
                else if (sequence.Length == 0)
                {
                    buffer[bufferLength++] = unchecked((byte)code);
                    sequence = new byte[] { unchecked((byte)code) };
                    sequenceCursor = 1;
                }
                else
                {
                    var newEntry = new byte[sequence.Length + 1];
                    sequence.CopyTo(newEntry, 0);

                    if (code < ClearTable)
                    {
                        sequence = new byte[] { unchecked((byte)code) };
                        newEntry[newEntry.Length - 1] = sequence[0];
                    }
                    else if (code - FirstDictionaryKey < dictionary.Count)
                    {
                        sequence = dictionary[(int)(code - FirstDictionaryKey)];
                        newEntry[newEntry.Length - 1] = sequence[0];
                    }
                    else
                    {
                        newEntry[newEntry.Length - 1] = sequence[0];
                        sequence = newEntry;
                    }

                    sequenceCursor = 0;
                    dictionary.Add(newEntry);

                    if (earlyChange)
                    {
                        switch (dictionary.Count)
                        {
                            case 253: reader.BitsPerValue = 10; break;
                            case 765: reader.BitsPerValue = 11; break;
                            case 1789: reader.BitsPerValue = 12; break;
                        }
                    }
                    else
                    {
                        switch (dictionary.Count)
                        {
                            case 254: reader.BitsPerValue = 10; break;
                            case 766: reader.BitsPerValue = 11; break;
                            case 1790: reader.BitsPerValue = 12; break;
                        }
                    }
                }
            }
        }
    }
}
