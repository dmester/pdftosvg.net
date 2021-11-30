// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CharStrings
{
    [DebuggerDisplay("{" + nameof(Token) + "} {" + nameof(Value) + "}")]
    internal struct CharStringLexeme
    {
        public CharStringLexeme(CharStringToken token, double value)
        {
            Token = token;
            Value = value;
        }

        public CharStringToken Token { get; }

        public double Value { get; }

        public static CharStringLexeme EndOfInput => new CharStringLexeme();

        public static CharStringLexeme Operand(double value) => new CharStringLexeme(CharStringToken.Operand, value);

        public static CharStringLexeme Operator(int code) => new CharStringLexeme(CharStringToken.Operator, code);

        public static CharStringLexeme Operator(int codeByte1, int codeByte2) => new CharStringLexeme(CharStringToken.Operator, (codeByte1 << 8) | codeByte2);
    }
}
