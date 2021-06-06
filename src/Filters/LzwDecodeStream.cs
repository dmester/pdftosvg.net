// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
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

        private readonly Stream stream;
        private readonly List<byte[]> dictionary = new List<byte[]>();

        private byte[] sequence = ArrayUtils.Empty<byte>();
        private int sequenceCursor;

        private readonly byte[] readBuffer;
        private int readBufferLength;
        private int readBufferCursor;
        private int skipBitsNextByte;

        private int nextCodeLengthBits = 9;

        private readonly bool earlyChange;

        private const int BufferSize = 2048;
        private const int ClearTable = 256;
        private const int EndOfDecode = 257;
        private const int FirstDictionaryKey = 258;

        public LzwDecodeStream(Stream stream, bool earlyChange)
        {
            this.stream = stream;
            this.earlyChange = earlyChange;
            buffer = new byte[BufferSize];
            readBuffer = new byte[BufferSize];
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                stream.Dispose();
            }
        }

        private int ReadBits(int bits)
        {
            // Fill buffer
            if (readBufferCursor + (skipBitsNextByte + bits + 7) / 8 > readBufferLength)
            {
                var saveBytes = readBufferLength - readBufferCursor;
                for (var i = 0; i < saveBytes; i++)
                {
                    readBuffer[i] = readBuffer[readBufferLength - saveBytes + i];
                }

                readBufferCursor = 0;
                readBufferLength = saveBytes;

                do
                {
                    var readFromStream = stream.Read(readBuffer, readBufferLength, readBuffer.Length - readBufferLength);

                    // Check end of stream
                    if (readFromStream == 0) return -1;

                    readBufferLength += readFromStream;
                }
                while (readBufferCursor + (skipBitsNextByte + bits + 7) / 8 > readBufferLength);
            }

            // First byte
            var result = readBuffer[readBufferCursor++] & ((1 << (8 - skipBitsNextByte)) - 1);
            bits -= 8 - skipBitsNextByte;

            // Middle bytes
            while (bits >= 8)
            {
                result = (result << 8) | readBuffer[readBufferCursor++];
                bits -= 8;
            }

            // Last byte
            if (bits > 0)
            {
                result = (result << bits) | (readBuffer[readBufferCursor] >> (8 - bits));
            }

            skipBitsNextByte = bits;
            return result;
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

                var code = ReadBits(nextCodeLengthBits);
                if (code < 0)
                {
                    endOfStream = true;
                    break;
                }
                
                if (code == ClearTable)
                {
                    dictionary.Clear();
                    nextCodeLengthBits = 9;
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
                        sequence = dictionary[code - FirstDictionaryKey];
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
                            case 253: nextCodeLengthBits = 10; break;
                            case 765: nextCodeLengthBits = 11; break;
                            case 1789: nextCodeLengthBits = 12; break;
                        }
                    }
                    else
                    {
                        switch (dictionary.Count)
                        {
                            case 254: nextCodeLengthBits = 10; break;
                            case 766: nextCodeLengthBits = 11; break;
                            case 1790: nextCodeLengthBits = 12; break;
                        }
                    }
                }
            }
        }
    }
}
