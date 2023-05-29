// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpeg
{
    internal struct JpegHuffmanCode : IEquatable<JpegHuffmanCode>
    {
        private int value;

        public int Code => value & 0xffff;

        public int CodeLength => value >> 24;

        public static JpegHuffmanCode Empty => new JpegHuffmanCode();

        public JpegHuffmanCode(int code, int codeLength)
        {
            value = (codeLength << 24) | code;
        }

        public override string ToString()
        {
            if (value == 0)
            {
                return "<empty>";
            }
            else
            {
                return Convert.ToString(Code | (1 << CodeLength), 2).Substring(1);
            }
        }

        public override bool Equals(object obj) => obj is JpegHuffmanCode code && code.value == value;
        public bool Equals(JpegHuffmanCode other) => other.value == value;
        public override int GetHashCode() => value;
    }
}
