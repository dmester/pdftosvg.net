// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpeg
{
    internal class JpegImageDataWriter : IDisposable
    {
        private readonly MemoryStream stream;

        private int byteValue;
        private int bitCursor;

        private int restartMarkerCount;

        public JpegImageDataWriter(MemoryStream stream)
        {
            this.stream = stream;
        }

        private void FlushPendingByte()
        {
            if (bitCursor > 0)
            {
                if (bitCursor < 8)
                {
                    byteValue |= (1 << (8 - bitCursor)) - 1;
                }

                stream.WriteByte((byte)byteValue);

                if (byteValue == 0xff)
                {
                    // Byte stuffing
                    stream.WriteByte(0x00);
                }

                bitCursor = 0;
                byteValue = 0;
            }
        }

        public void WriteBit(int bit) => WriteBits(bit, 1);

        public void WriteCode(JpegHuffmanCode code) => WriteBits(code.Code, code.CodeLength);

        public void WriteBits(int value, int bitCount)
        {
            while (bitCount > 0)
            {
                var iterationBitCount = Math.Min(8 - bitCursor, bitCount);
                var iterationBitMask = (1 << iterationBitCount) - 1;
                var iterationValue = (value >> (bitCount - iterationBitCount)) & iterationBitMask;

                byteValue |= iterationValue << (8 - bitCursor - iterationBitCount);

                bitCount -= iterationBitCount;
                bitCursor += iterationBitCount;

                if (bitCursor >= 8)
                {
                    FlushPendingByte();
                }
            }
        }

        public void WriteValue(int ssss, int value)
        {
            if (ssss == 0)
            {
                return;
            }

            if (value < 0)
            {
                WriteBit(0);

                var lowerBound = ((-1) << ssss) + 1;
                value -= lowerBound;
            }
            else
            {
                WriteBit(1);
            }

            WriteBits(value, ssss - 1);
        }

        public int GetSsss(int value)
        {
            if (value == 0)
            {
                return 0;
            }

            if (value >= -1 && value <= 1)
            {
                return 1;
            }

            if (value >= -3 && value <= 3)
            {
                return 2;
            }

            if (value >= -7 && value <= 7)
            {
                return 3;
            }

            if (value >= -15 && value <= 15)
            {
                return 4;
            }

            if (value >= -31 && value <= 31)
            {
                return 5;
            }

            if (value >= -63 && value <= 63)
            {
                return 6;
            }

            if (value >= -127 && value <= 127)
            {
                return 7;
            }

            if (value >= -255 && value <= 255)
            {
                return 8;
            }

            if (value >= -511 && value <= 511)
            {
                return 9;
            }

            if (value >= -1023 && value <= 1023)
            {
                return 10;
            }

            return 11;
        }

        public void WriteRestartMarker()
        {
            FlushPendingByte();

            stream.WriteByte(0xff);
            stream.WriteByte((byte)(0xd0 + (restartMarkerCount & 7)));

            restartMarkerCount++;
        }

        public void Dispose()
        {
            FlushPendingByte();
        }
    }
}
