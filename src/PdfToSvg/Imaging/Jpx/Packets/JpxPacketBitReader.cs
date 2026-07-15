// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Imaging.Jpx.IO;
using System;
using System.IO;

namespace PdfToSvg.Imaging.Jpx.Packets
{
    internal struct JpxPacketBitReader
    {
        private const int BitsPerByte = 8;

        private readonly JpxDataReader inputReader;
        private int startCursor;
        private int currentByte;
        private int bitsLeft;

        public JpxPacketBitReader(JpxDataReader inputReader)
        {
            this.inputReader = inputReader;
            this.startCursor = inputReader.Cursor;
        }

        public int BytesConsumed => inputReader.Cursor - startCursor;

        public int Cursor => BytesConsumed - (bitsLeft > 0 ? 1 : 0);

        public int ReadBit()
        {
            if (bitsLeft == 0)
            {
                currentByte = inputReader.ReadByte();
                bitsLeft = BitsPerByte;
            }

            var bit = (currentByte >> (bitsLeft - 1)) & 1;
            bitsLeft--;

            // ITU-T T.800 (06/2019) Section B.10.1 packet headers use bit-stuffing after 0xff bytes
            if (bitsLeft == 0 && currentByte == 0xff)
            {
                // Consume and discard next bit
                currentByte = inputReader.ReadByte();
                bitsLeft = BitsPerByte - 1;
            }

            return bit;
        }

        public int ReadBits(int bitCount)
        {
            var result = 0;
            while (bitCount-- > 0)
            {
                result = (result << 1) | ReadBit();
            }
            return result;
        }
    }
}
