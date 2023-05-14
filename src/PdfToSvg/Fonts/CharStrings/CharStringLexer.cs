// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CharStrings
{
    internal class CharStringLexer
    {
        private readonly CharStringType type;
        private readonly byte[] data;
        private int cursor = 0;
        private int endIndex;

        public CharStringLexer(CharStringType type, ArraySegment<byte> data)
        {
            this.type = type;
            this.data = data.Array;
            this.cursor = data.Offset;
            this.endIndex = data.Offset + data.Count;
        }

        public static CharStringLexer EmptyLexer { get; } = new CharStringLexer(
            CharStringType.Type2,
            new ArraySegment<byte>(ArrayUtils.Empty<byte>(), 0, 0));

        public int Position => cursor;

        public bool EndOfInput => cursor >= endIndex;

        public byte ReadByte()
        {
            if (cursor < endIndex)
            {
                return data[cursor++];
            }

            return 0;
        }

        public CharStringLexeme Read()
        {
            if (cursor >= endIndex)
            {
                return CharStringLexeme.EndOfInput;
            }

            var val = data[cursor++];

            if (val <= 27)
            {
                if (val != 12)
                {
                    return CharStringLexeme.Operator(val);
                }
                else if (cursor >= endIndex)
                {
                    return CharStringLexeme.EndOfInput;
                }
                else
                {
                    return CharStringLexeme.Operator(val, data[cursor++]);
                }
            }

            if (val == 28)
            {
                cursor += 2;

                if (cursor > endIndex)
                {
                    return CharStringLexeme.EndOfInput;
                }
                else
                {
                    return CharStringLexeme.Operand(unchecked((short)((data[cursor - 2] << 8) | data[cursor - 1])));
                }
            }

            if (val <= 31)
            {
                return CharStringLexeme.Operator(val);
            }

            if (val <= 246)
            {
                return CharStringLexeme.Operand(val - 139);
            }

            if (val <= 250)
            {
                if (cursor >= endIndex)
                {
                    return CharStringLexeme.EndOfInput;
                }
                else
                {
                    var w = data[cursor++];
                    return CharStringLexeme.Operand(((val - 247) << 8) + w + 108);
                }
            }

            if (val <= 254)
            {
                if (cursor >= endIndex)
                {
                    return CharStringLexeme.EndOfInput;
                }
                else
                {
                    var w = data[cursor++];
                    return CharStringLexeme.Operand(-((val - 251) << 8) - w - 108);
                }
            }

            cursor += 4;

            if (cursor > endIndex)
            {
                return CharStringLexeme.EndOfInput;
            }

            var num = (double)(
                (data[cursor - 4] << 24) |
                (data[cursor - 3] << 16) |
                (data[cursor - 2] << 8) |
                (data[cursor - 1])
                );

            if (type == CharStringType.Type2)
            {
                num *= 1 / (double)(1 << 16);
            }

            return CharStringLexeme.Operand(num);
        }
    }
}
