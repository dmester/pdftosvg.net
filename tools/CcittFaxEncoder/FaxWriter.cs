// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcittFaxEncoder
{
    internal class FaxWriter
    {
        private readonly MemoryStream stream = new MemoryStream();

        private int byteValue;
        private int bitCursor;

        public void WriteCode(IEnumerable<int> codes)
        {
            foreach (var code in codes)
            {
                WriteCode(code);
            }
        }

        /// <summary>
        /// Write codes prepended with a 1 bit for termination.
        /// </summary>
        public void WriteCode(int code)
        {
            if (code <= 1)
            {
                throw new ArgumentOutOfRangeException();
            }

            // Code is prepended with a 1 bit to determine the code length
            var codeLength = 1;
            while ((code >> codeLength) != 1)
            {
                codeLength++;
            }

            WriteBits(code, codeLength);
        }

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
                    stream.WriteByte((byte)byteValue);
                    byteValue = 0;
                    bitCursor = 0;
                }
            }
        }

        public void ByteAlign(int padding)
        {
            if (bitCursor > 0)
            {
                WriteBits(padding, 8 - bitCursor);
            }
        }

        public byte[] ToArray()
        {
            ByteAlign(0);
            return stream.ToArray();
        }
    }
}
