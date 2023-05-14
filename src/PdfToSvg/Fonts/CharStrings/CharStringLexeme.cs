// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CharStrings
{
    internal struct CharStringLexeme
    {
        public CharStringLexeme(CharStringToken token, double value)
        {
            Token = token;
            Value = value;
            OpCode = CharStringOpCode.None;
        }

        public CharStringLexeme(CharStringToken token, CharStringOpCode opCode)
        {
            Token = token;
            Value = double.NaN;
            OpCode = opCode;
        }

        public CharStringToken Token { get; }

        public double Value { get; }

        public CharStringOpCode OpCode { get; }

        public static CharStringLexeme EndOfInput => new CharStringLexeme();

        public static CharStringLexeme Operand(double value)
        {
            return new CharStringLexeme(CharStringToken.Operand, value);
        }

        public static CharStringLexeme Operator(CharStringOpCode code)
        {
            return new CharStringLexeme(CharStringToken.Operator, code);
        }

        public static CharStringLexeme Operator(int code)
        {
            return new CharStringLexeme(CharStringToken.Operator, (CharStringOpCode)code);
        }

        public static CharStringLexeme Operator(int codeByte1, int codeByte2)
        {
            return new CharStringLexeme(CharStringToken.Operator, (CharStringOpCode)((codeByte1 << 8) | codeByte2));
        }

        public static CharStringLexeme Mask(byte mask)
        {
            return new CharStringLexeme(CharStringToken.Mask, mask);
        }

        public override string ToString()
        {
            switch (Token)
            {
                case CharStringToken.EndOfInput:
                    return "-|";

                case CharStringToken.Mask:
                    return "0x" + ((int)Value).ToString("x2");

                case CharStringToken.Operator:
                    return Enum.IsDefined(typeof(CharStringOpCode), OpCode) ? OpCode.ToString() : "OP " + OpCode;

                case CharStringToken.Operand:
                    return Value.ToString("0.##", CultureInfo.InvariantCulture);

                default:
                    return "";
            }
        }
    }
}
