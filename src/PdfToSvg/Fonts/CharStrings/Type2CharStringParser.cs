// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfToSvg.Fonts.CharStrings
{
    internal class Type2CharStringParser
    {
        private readonly Stack<Type2CharStringLexer> parkedLexers = new Stack<Type2CharStringLexer>();

        private double[]? storage;

        private readonly byte[] data;
        private readonly int[] globalSubrOffsets, localSubrOffsets;

        private Type2CharStringParser(byte[] data, int startIndex, int endIndex, int[] globalSubrOffsets, int[] localSubrOffsets)
        {
            Lexer = new Type2CharStringLexer(data, startIndex, endIndex);

            this.data = data;
            this.globalSubrOffsets = globalSubrOffsets;
            this.localSubrOffsets = localSubrOffsets;
        }

        public CharStringStack Stack { get; } = new CharStringStack();

        public CharStringPath Path { get; } = new CharStringPath();

        public Type2CharStringLexer Lexer { get; private set; }

        // The storage is limited to 32 fields according to spec, but it doesn't mention if the 
        // field indexes are zero or one based => allocate up to field 32.
        public double[] Storage => storage ??= new double[33];

        /// <summary>
        /// Glyph advance width.
        /// </summary>
        public double? Width { get; set; }

        /// <summary>
        /// The last executed operator, excluding operators managing subroutines.
        /// </summary>
        public CharStringOperator? LastOperator { get; private set; }

        public int HintCount { get; set; }

        public static CharString Parse(byte[] data, int startIndex, int endIndex, int[] globalSubrOffsets, int[] localSubrOffsets)
        {
            var parser = new Type2CharStringParser(data, startIndex, endIndex, globalSubrOffsets, localSubrOffsets);
            return parser.ReadCharString();
        }

        private CharString ReadCharString()
        {
            ExecCharString();

            return new CharString(Width, Path.MinX, Path.MaxX, Path.MinY, Path.MaxY);
        }

        private void ExecCharString()
        {
            var lexeme = Lexer.Read();

            while (lexeme.Token != CharStringToken.EndOfInput)
            {
                if (lexeme.Token == CharStringToken.Operand)
                {
                    Stack.Push(lexeme.Value);
                }
                else if (lexeme.Token == CharStringToken.Operator)
                {
                    if (CharStringOperators.TryGetOperator((int)lexeme.Value, out var op))
                    {
                        op.Invoke(this);

                        if (op.ClearStack)
                        {
                            var leftArguments = Stack.Count;
                            if (leftArguments > 0)
                            {
                                if (Width == null)
                                {
                                    Width = Stack[0];
                                    leftArguments--;
                                }

                                if (leftArguments > 0)
                                {
                                    Log.WriteLine("Char string stack not empty after stack clearing operator.");
                                }

                                Stack.Clear();
                            }
                        }

                        if (!op.SubrOperator)
                        {
                            LastOperator = op;
                        }
                    }
                    else
                    {
                        throw new CharStringException("Unknown charstring operator: " + (int)lexeme.Value);
                    }
                }

                lexeme = Lexer.Read();
            }
        }

        public void Return()
        {
            Lexer = Type2CharStringLexer.EmptyLexer;
        }

        public void EndChar()
        {
            Lexer = Type2CharStringLexer.EmptyLexer;
            parkedLexers.Clear();
        }

        public void CallSubr(int number, bool global)
        {
            var subrs = global ? globalSubrOffsets : localSubrOffsets;

            var bias =
                subrs.Length < 1240 ? 107 :
                subrs.Length < 33900 ? 1131 :
                32768;

            var subrIndex = number + bias;
            if (subrIndex < 0 || subrIndex >= subrs.Length)
            {
                throw new CharStringException((global ? "Global" : "Local") + " subroutine with number " + number + " not found.");
            }

            var dataStartIndex = subrs[subrIndex];
            var dataEndIndex = subrIndex + 1 < subrs.Length ? subrs[subrIndex + 1] : data.Length;

            parkedLexers.Push(Lexer);

            if (parkedLexers.Count > 10)
            {
                throw new CharStringException("Char string subroutine stack overflow.");
            }

            Lexer = new Type2CharStringLexer(data, dataStartIndex, dataEndIndex);

            ExecCharString();

            Lexer = parkedLexers.Count > 0
                ? parkedLexers.Pop()
                : Type2CharStringLexer.EmptyLexer;
        }
    }
}
