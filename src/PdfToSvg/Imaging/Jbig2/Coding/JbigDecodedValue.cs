// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jbig2.Coding
{
    internal struct JbigDecodedValue
    {
        public readonly int Value;
        public readonly bool IsOob;

        public static JbigDecodedValue Oob => new JbigDecodedValue(true);

        public JbigDecodedValue(int value)
        {
            this.Value = value;
            this.IsOob = false;
        }

        private JbigDecodedValue(bool isOob)
        {
            this.Value = 0;
            this.IsOob = isOob;
        }

        public override string ToString()
        {
            return IsOob ? "OOB" : Value.ToString();
        }
    }
}
