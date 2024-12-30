// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jbig2.Coding
{
    internal struct JbigHuffmanCode
    {
        public int Code;
        public int CodeLength;

        public JbigHuffmanCode(int code, int codeLength)
        {
            this.Code = code;
            this.CodeLength = codeLength;
        }

        public override int GetHashCode() => Code ^ (CodeLength << 24);

        public override bool Equals(object? obj)
        {
            return
                obj is JbigHuffmanCode otherPrefix &&
                otherPrefix.CodeLength == CodeLength &&
                otherPrefix.Code == Code;
        }

        public override string ToString()
        {
            return Convert.ToString(Code | (1 << CodeLength), 2).Substring(1);
        }
    }
}
