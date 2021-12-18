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
                code == CharStringOpCode.HStem ||
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
                foreach (var lexeme in bchar.ContentInlinedSubrs)
                {
                    if (lexeme.OpCode == CharStringOpCode.EndChar)
                    {
                        break;
                    }

                    result.Add(lexeme);
                }

                adx -= bchar.LastX;
                ady -= bchar.LastY;
            }

            result.Add(CharStringLexeme.Operand(adx));
            result.Add(CharStringLexeme.Operand(ady));
            result.Add(CharStringLexeme.Operator(CharStringOpCode.RMoveTo));

            // TODO handle hints

            if (achar != null)
            {
                var startOperandIndex = -1;

                for (var i = 0; i < achar.ContentInlinedSubrs.Count; i++)
                {
                    var lexeme = achar.ContentInlinedSubrs[i];

                    if (lexeme.Token == CharStringToken.Operand)
                    {
                        if (startOperandIndex < 0)
                        {
                            startOperandIndex = i;
                        }
                    }
                    else if (lexeme.Token == CharStringToken.Operator)
                    {
                        if (!IsHint(achar.ContentInlinedSubrs[i].OpCode))
                        {
                            for (var j = startOperandIndex < 0 ? i : startOperandIndex; j <= i; j++)
                            {
                                result.Add(achar.ContentInlinedSubrs[j]);
                            }
                        }

                        startOperandIndex = -1;
                    }
                }
            }

            return result;
        }
    }
}
