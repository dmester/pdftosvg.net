// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CharStrings
{
    internal static class SeacMerger
    {
        private static bool IsHint(CharStringOpCode code)
        {
            return
                code == CharStringOpCode.VStem ||
                code == CharStringOpCode.VStem3 ||
                code == CharStringOpCode.HStem ||
                code == CharStringOpCode.HStem3 ||
                code == CharStringOpCode.VStemHm ||
                code == CharStringOpCode.HStemHm ||
                code == CharStringOpCode.HintMask ||
                code == CharStringOpCode.CntrMask;
        }

        public static List<CharStringLexeme> Merge(CharString? achar, CharString? bchar, double adx, double ady)
        {
            var result = new List<CharStringLexeme>();

            if (bchar != null)
            {
                foreach (var lexeme in bchar.Content)
                {
                    if (lexeme.OpCode == CharStringOpCode.EndChar)
                    {
                        break;
                    }

                    if (lexeme.Token == CharStringToken.Mask || IsHint(lexeme.OpCode))
                    {
                        continue;
                    }

                    result.Add(lexeme);
                }

                adx -= bchar.LastX;
                ady -= bchar.LastY;
            }

            result.Add(CharStringLexeme.Operand(adx));
            result.Add(CharStringLexeme.Operand(ady));
            result.Add(CharStringLexeme.Operator(CharStringOpCode.RMoveTo));

            if (achar != null)
            {
                foreach (var lexeme in achar.Content)
                {
                    if (lexeme.OpCode == CharStringOpCode.EndChar)
                    {
                        break;
                    }

                    if (lexeme.Token == CharStringToken.Mask || IsHint(lexeme.OpCode))
                    {
                        continue;
                    }

                    result.Add(lexeme);
                }
            }

            return result;
        }
    }
}
