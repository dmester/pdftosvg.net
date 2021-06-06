// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace PdfToSvg.IO
{
    /// <summary>
    /// Computes CRC32 checksums. Implementation based on https://www.w3.org/TR/PNG/#D-CRCAppendix.
    /// </summary>
    internal class Crc32
    {
        private static readonly uint[] crcTable = MakeCrcTable();
        private uint crc;

        /// <summary>
        /// Creates an instance of <see cref="Crc32"/>.
        /// </summary>
        public Crc32(uint seed = 0xffffffffu)
        {
            crc = seed;
        }

        /// <summary>
        /// Gets the checksum of the processed data so far.
        /// </summary>
        public uint Value => crc ^ 0xffffffff;
        
        private static uint[] MakeCrcTable()
        {
            uint c;
            int n, k;

            var crcTable = new uint[256];

            for (n = 0; n < 256; n++)
            {
                c = (uint)n;
                for (k = 0; k < 8; k++)
                {
                    if ((c & 1) == 1)
                        c = 0xedb88320u ^ (c >> 1);
                    else
                        c = c >> 1;
                }
                crcTable[n] = c;
            }

            return crcTable;
        }

        /// <summary>
        /// Adds the specified data to the checksum.
        /// </summary>
        public void Update(byte[] data, int offset, int count)
        {
            for (var i = 0; i < count; i++)
            {
                crc = crcTable[(crc ^ data[offset + i]) & 0xff] ^ (crc >> 8);
            }
        }
    }
}
