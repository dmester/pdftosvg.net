// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfToSvg.Fonts.CharStrings
{
    internal class Type2CharStringWriter : MemoryWriter
    {
        public Type2CharStringWriter() { }
        public Type2CharStringWriter(int capacity) : base(capacity) { }

        public void WriteLexeme(CharStringLexeme lexeme)
        {
            switch (lexeme.Token)
            {
                case CharStringToken.Operator:
                    WriteOperator((int)lexeme.OpCode);
                    break;

                case CharStringToken.Operand:
                    WriteOperand(lexeme.Value);
                    break;

                case CharStringToken.Mask:
                    WriteMask((byte)lexeme.Value);
                    break;
            }
        }

        public void WriteOperator(int operatorCode)
        {
            EnsureCapacity(cursor + 2);

            if (operatorCode > 255)
            {
                buffer[cursor++] = (byte)(operatorCode >> 8);
            }

            buffer[cursor++] = (byte)(operatorCode);

            if (length < cursor)
            {
                length = cursor;
            }
        }

        public void WriteOperand(double operand)
        {
            if (operand == Math.Truncate(operand))
            {
                WriteInteger((int)operand);
            }
            else
            {
                WriteReal(operand);
            }
        }

        public void WriteReal(double value)
        {
            EnsureCapacity(cursor + 5);

            if (value >= -32768 && value <= 32767)
            {
                var intValue = (int)(value * (1 << 16));

                buffer[cursor + 0] = 255;
                buffer[cursor + 1] = (byte)(intValue >> 24);
                buffer[cursor + 2] = (byte)(intValue >> 16);
                buffer[cursor + 3] = (byte)(intValue >> 8);
                buffer[cursor + 4] = (byte)(intValue);

                cursor += 5;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (length < cursor)
            {
                length = cursor;
            }
        }

        public void WriteInteger(int value)
        {
            EnsureCapacity(cursor + 3);

            if (value >= -107 && value <= 107)
            {
                buffer[cursor++] = (byte)(value + 139);
            }
            else if (value >= 108 && value <= 1131)
            {
                value -= 108;

                buffer[cursor + 0] = (byte)((value >> 8) + 247);
                buffer[cursor + 1] = (byte)(value);

                cursor += 2;
            }
            else if (value >= -1131 && value <= -108)
            {
                value = -value - 108;

                buffer[cursor + 0] = (byte)((value >> 8) + 251);
                buffer[cursor + 1] = (byte)(value);

                cursor += 2;
            }
            else if (value >= -32768 && value <= 32767)
            {
                buffer[cursor + 0] = 28;
                buffer[cursor + 1] = (byte)(value >> 8);
                buffer[cursor + 2] = (byte)(value);

                cursor += 3;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (length < cursor)
            {
                length = cursor;
            }
        }

        public void WriteMask(byte mask)
        {
            EnsureCapacity(cursor + 1);

            buffer[cursor++] = mask;

            if (length < cursor)
            {
                length = cursor;
            }
        }
    }
}
