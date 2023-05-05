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
using System.Threading.Tasks;

namespace PdfToSvg.IO
{
    /// <summary>
    /// Reads integers from a stream, packed with 1 to 32 bits per value. The number of bits per value can vary.
    /// </summary>
    internal class BitReader : IDisposable
    {
        private const int BitsPerByte = 8;

        private Stream? stream;

        private readonly byte[] readBuffer;

        /// <summary>
        /// Number of populated bytes in <see cref="readBuffer"/>.
        /// </summary>
        private int readBufferLength;

        /// <summary>
        /// Next byte to read in <see cref="readBuffer"/>.
        /// </summary>
        private int byteCursor;

        /// <summary>
        /// Cursor within the byte that <see cref="byteCursor"/> is pointing at.
        /// </summary>
        private int bitCursor;

        private bool endOfStream;

        private int bitsPerValue;

        // The currently used specialized value reader implementation.
        private Func<uint[], int, int, int> uintReader;
        private Func<float[], int, int, int> floatReader;

#pragma warning disable 8618
        public BitReader(Stream stream, int bitsPerValue, int bufferSize = 1024)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.readBuffer = new byte[bufferSize];
            BitsPerValue = bitsPerValue;
        }
#pragma warning restore 8618

        /// <summary>
        /// Gets or sets the size of the next read values in bits.
        /// </summary>
        /// <remarks>
        /// <para>Allowed values are 1 - 32.</para>
        /// <para>
        ///     Note that if a value larger than ~22 bits is used, the values will lose precision if read to a <see cref="float"/> array.
        /// </para>
        /// </remarks>
        public int BitsPerValue
        {
            get => bitsPerValue;
            set
            {
                if (bitsPerValue != value)
                {
                    if (value < 1 || value > 32)
                    {
                        throw new ArgumentOutOfRangeException(nameof(BitsPerValue), $"Only {nameof(BitsPerValue)} between 1 and 32 are allowed.");
                    }

                    bitsPerValue = value;

                    // The specialized implementations cannot be used if the cursor is not aligned correctly.
                    if (bitCursor == 0 && value == 8)
                    {
                        uintReader = Read8Bit;
                        floatReader = Read8Bit;
                    }
                    else if (bitCursor == 0 && value == 16)
                    {
                        uintReader = Read16Bit;
                        floatReader = Read16Bit;
                    }
                    else if (bitCursor == 0 && value == 32)
                    {
                        uintReader = Read32Bit;
                        floatReader = Read32Bit;
                    }
                    else if ((bitCursor % value) == 0 && (value == 1 || value == 2 || value == 4))
                    {
                        uintReader = ReadAlignedPartial;
                        floatReader = ReadAlignedPartial;
                    }
                    else
                    {
                        uintReader = ReadUnaligned;
                        floatReader = ReadUnaligned;
                    }
                }
            }
        }

        public int Read(uint[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

            return uintReader(buffer, offset, count);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

            return floatReader(buffer, offset, count);
        }

        private void FillBuffer()
        {
            if (stream == null)
            {
                throw new ObjectDisposedException(nameof(BitReader));
            }

            if (endOfStream)
            {
                return;
            }

            var saveBytes = readBufferLength - byteCursor;
            for (var i = 0; i < saveBytes; i++)
            {
                readBuffer[i] = readBuffer[readBufferLength - saveBytes + i];
            }

            byteCursor = 0;
            readBufferLength = saveBytes;

            do
            {
                var readFromStream = stream.Read(readBuffer, readBufferLength, readBuffer.Length - readBufferLength);

                // Check end of stream
                if (readFromStream == 0)
                {
                    endOfStream = true;
                    return;
                }

                readBufferLength += readFromStream;
            }
            while (readBufferLength < readBuffer.Length);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private bool EnsureBufferBytes(int bytes)
        {
            if (byteCursor + bytes > readBufferLength)
            {
                FillBuffer();

                if (byteCursor + bytes > readBufferLength)
                {
                    return false;
                }
            }

            return true;
        }

        public void SkipPartialByte()
        {
            if (bitCursor > 0)
            {
                bitCursor = 0;
                byteCursor++;
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private uint ReadBits(int bits)
        {
            var result = 0u;

            // First partial byte
            if (bitCursor > 0)
            {
                var bitsFirstRead = BitsPerByte - bitCursor;
                if (bitsFirstRead > bits)
                {
                    bitsFirstRead = bits;
                }

                result =
                    ((uint)readBuffer[byteCursor] >> (BitsPerByte - bitCursor - bitsFirstRead)) &
                    ((1u << bitsFirstRead) - 1);

                if (bitCursor + bitsFirstRead == BitsPerByte)
                {
                    byteCursor++;
                    bitCursor = 0;
                }
                else
                {
                    bitCursor += bitsFirstRead;
                }

                bits -= bitsFirstRead;
            }

            // Whole bytes
            while (bits >= BitsPerByte)
            {
                result = (result << BitsPerByte) | readBuffer[byteCursor++];
                bits -= BitsPerByte;
            }

            // Last partial byte
            if (bits > 0)
            {
                result = (result << bits) | ((uint)readBuffer[byteCursor] >> (BitsPerByte - bits));
                bitCursor = bits;
            }

            return result;
        }

        private int Read8Bit(uint[] buffer, int offset, int count)
        {
            var read = 0;

            while (read < count && EnsureBufferBytes(1))
            {
                buffer[offset + read] = readBuffer[byteCursor];

                read++;
                byteCursor++;
            }

            return read;
        }

        private int Read8Bit(float[] buffer, int offset, int count)
        {
            var read = 0;

            while (read < count && EnsureBufferBytes(1))
            {
                buffer[offset + read] = readBuffer[byteCursor];

                read++;
                byteCursor++;
            }

            return read;
        }

        private int Read16Bit(uint[] buffer, int offset, int count)
        {
            var read = 0;

            while (read < count && EnsureBufferBytes(2))
            {
                buffer[offset + read] =
                    ((uint)readBuffer[byteCursor] << 8) |
                    readBuffer[byteCursor + 1];

                byteCursor += 2;
                read++;
            }

            return read;
        }

        private int Read16Bit(float[] buffer, int offset, int count)
        {
            var read = 0;

            while (read < count && EnsureBufferBytes(2))
            {
                buffer[offset + read] =
                    ((uint)readBuffer[byteCursor] << 8) |
                    readBuffer[byteCursor + 1];

                byteCursor += 2;
                read++;
            }

            return read;
        }

        private int Read32Bit(uint[] buffer, int offset, int count)
        {
            var read = 0;

            while (read < count && EnsureBufferBytes(4))
            {
                buffer[offset + read] =
                    ((uint)readBuffer[byteCursor] << 24) |
                    ((uint)readBuffer[byteCursor + 1] << 16) |
                    ((uint)readBuffer[byteCursor + 2] << 8) |
                    readBuffer[byteCursor + 3];

                byteCursor += 4;
                read++;
            }

            return read;
        }

        private int Read32Bit(float[] buffer, int offset, int count)
        {
            var read = 0;

            while (read < count && EnsureBufferBytes(4))
            {
                buffer[offset + read] =
                    ((uint)readBuffer[byteCursor] << 24) |
                    ((uint)readBuffer[byteCursor + 1] << 16) |
                    ((uint)readBuffer[byteCursor + 2] << 8) |
                    readBuffer[byteCursor + 3];

                byteCursor += 4;
                read++;
            }

            return read;
        }

        private int ReadUnaligned(uint[] buffer, int offset, int count)
        {
            var read = 0;

            while (read < count && EnsureBufferBytes(MathUtils.BitsToBytes(bitCursor + bitsPerValue)))
            {
                buffer[offset + read] = ReadBits(bitsPerValue);
                read++;
            }

            return read;
        }

        private int ReadUnaligned(float[] buffer, int offset, int count)
        {
            var read = 0;

            while (read < count && EnsureBufferBytes(MathUtils.BitsToBytes(bitCursor + bitsPerValue)))
            {
                buffer[offset + read] = ReadBits(bitsPerValue);
                read++;
            }

            return read;
        }

        private int ReadAlignedPartial(uint[] buffer, int offset, int count)
        {
            var read = 0;
            var valueMask = (uint)((1L << bitsPerValue) - 1);

            while (read < count && EnsureBufferBytes(1))
            {
                var byteValue = (uint)readBuffer[byteCursor];

                do
                {
                    buffer[offset + read] = (byteValue >> (BitsPerByte - bitCursor - bitsPerValue)) & valueMask;
                    bitCursor += bitsPerValue;
                    read++;

                    if (bitCursor == BitsPerByte)
                    {
                        bitCursor = 0;
                        byteCursor++;
                        break;
                    }
                }
                while (read < count);
            }

            return read;
        }

        private int ReadAlignedPartial(float[] buffer, int offset, int count)
        {
            var read = 0;
            var valueMask = (uint)((1L << bitsPerValue) - 1);

            while (read < count && EnsureBufferBytes(1))
            {
                var byteValue = (uint)readBuffer[byteCursor];

                do
                {
                    buffer[offset + read] = (byteValue >> (BitsPerByte - bitCursor - bitsPerValue)) & valueMask;
                    bitCursor += bitsPerValue;
                    read++;

                    if (bitCursor == BitsPerByte)
                    {
                        bitCursor = 0;
                        byteCursor++;
                        break;
                    }
                }
                while (read < count);
            }

            return read;
        }

        public void Dispose()
        {
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }
    }
}
