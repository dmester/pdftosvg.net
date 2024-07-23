// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Encodings;
using PdfToSvg.Fonts.CompactFonts;
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

        public static void ReplaceSeacChars(CompactFont font)
        {
            for (var glyphIndex = 0; glyphIndex < font.Glyphs.Count; glyphIndex++)
            {
                var glyph = font.Glyphs[glyphIndex];
                var seac = glyph.CharString.Seac;
                if (seac != null)
                {
                    var content = glyph.CharString.Content;
                    var standardEncoding = SingleByteEncoding.Standard;

                    var acharValue = standardEncoding.GetString(new byte[] { (byte)seac.Achar });
                    var bcharValue = standardEncoding.GetString(new byte[] { (byte)seac.Bchar });

                    var achar = font.Glyphs.FirstOrDefault(x => x.Unicode == acharValue);
                    var bchar = font.Glyphs.FirstOrDefault(x => x.Unicode == bcharValue);

                    if (achar == null || bchar == null)
                    {
                        continue;
                    }

                    var mergedCharString = SeacMerger.Merge(achar.CharString, bchar.CharString, seac.Adx, seac.Ady);

                    content.Clear();

                    foreach (var lexeme in mergedCharString)
                    {
                        content.Add(lexeme);
                    }

                    if (content.LastOrDefault().OpCode != CharStringOpCode.EndChar)
                    {
                        content.Add(CharStringLexeme.Operator(CharStringOpCode.EndChar));
                    }
                }
            }
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
