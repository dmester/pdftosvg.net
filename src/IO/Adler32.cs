// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace PdfToSvg.IO
{
    /// <summary>
    /// Computes Adler32 checksums. Implementation based on https://tools.ietf.org/html/rfc1950 section 2.2.
    /// </summary>
    internal class Adler32
    {
        private uint s1 = 1;
        private uint s2 = 0;

        public uint Value => (s2 << 16) + s1;

        public void Update(byte[] buffer, int offset, int count)
        {
            const uint Modulo = 65521;

            var s1 = this.s1;
            var s2 = this.s2;

            for (var i = 0; i < count; i++)
            {
                s1 += buffer[offset + i];

                if (s2 + s1 < s2)
                {
                    // Addition would cause overflow
                    s1 = s1 % Modulo;
                    s2 = s2 % Modulo;
                }

                s2 += s1;
            }

            this.s1 = s1 % Modulo;
            this.s2 = s2 % Modulo;
        }
    }
}
