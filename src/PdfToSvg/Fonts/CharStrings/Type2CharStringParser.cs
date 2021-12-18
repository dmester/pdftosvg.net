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

        private readonly IList<CharStringSubRoutine> globalSubrs, localSubrs;

        private Type2CharStringParser(ArraySegment<byte> data, IList<CharStringSubRoutine> globalSubrs, IList<CharStringSubRoutine> localSubrs)
        {
            Lexer = new Type2CharStringLexer(data);
            this.globalSubrs = globalSubrs;
            this.localSubrs = localSubrs;
        }

        public bool InSubRoutine => parkedLexers.Count > 0;

        public CharStringStack Stack { get; } = new CharStringStack();

        public CharStringPath Path => CharString.Path;

        public Type2CharStringLexer Lexer { get; private set; }

        // The storage is limited to 32 fields according to spec, but it doesn't mention if the 
        // field indexes are zero or one based => allocate up to field 32.
        public double[] Storage => storage ??= new double[33];

        public CharStringInfo CharString { get; } = new CharStringInfo();

        public static CharString Parse(ArraySegment<byte> data, IList<CharStringSubRoutine> globalSubrs, IList<CharStringSubRoutine> localSubrs)
        {
            var parser = new Type2CharStringParser(data, globalSubrs, localSubrs);
            return parser.ReadCharString();
        }

        private CharString ReadCharString()
        {
            ExecCharString();

            return new CharString(CharString);
        }

        private void ExecCharString()
        {
            var lexeme = Lexer.Read();

            while (lexeme.Token != CharStringToken.EndOfInput)
            {
                if (lexeme.Token == CharStringToken.Operand)
                {
                    if (!InSubRoutine)
                    {
                        CharString.Content.Add(lexeme);
                    }

                    Stack.Push(lexeme.Value);
                }
                else if (lexeme.Token == CharStringToken.Operator)
                {
                    if (CharStringOperators.TryGetOperator(lexeme.OpCode, out var op))
                    {
                        if (!InSubRoutine)
                        {
                            CharString.Content.Add(lexeme);
                        }

                        op.Invoke(this);

                        if (op.ClearStack)
                        {
                            var leftArguments = Stack.Count;
                            if (leftArguments > 0)
                            {
                                if (CharString.Width == null)
                                {
                                    CharString.Width = Stack[0];
                                    leftArguments--;
                                }

                                if (leftArguments > 0)
                                {
                                    Log.WriteLine("Char string stack not empty after stack clearing operator.");
                                }

                                Stack.Clear();
                            }
                        }
                    }
                    else
                    {
                        throw new CharStringException("Unknown charstring operator: " + lexeme.OpCode);
                    }
                }

                lexeme = Lexer.Read();
            }
        }

        public void AppendInlinedSubrs(CharStringOpCode code, int? last = null, int? from = null)
        {
            var startAt =
                last.HasValue ? Stack.Count - last.Value :
                from.HasValue ? from.Value :
                Stack.Count;

            for (var i = startAt; i < Stack.Count; i++)
            {
                CharString.ContentInlinedSubrs.Add(CharStringLexeme.Operand(Stack[i]));
            }

            CharString.ContentInlinedSubrs.Add(CharStringLexeme.Operator(code));
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
            var subrs = global ? globalSubrs : localSubrs;

            var bias =
                subrs.Count < 1240 ? 107 :
                subrs.Count < 33900 ? 1131 :
                32768;

            var subrIndex = number + bias;
            if (subrIndex < 0 || subrIndex >= subrs.Count)
            {
                throw new CharStringException((global ? "Global" : "Local") + " subroutine with number " + number + " not found.");
            }

            parkedLexers.Push(Lexer);

            if (parkedLexers.Count > 10)
            {
                throw new CharStringException("Char string subroutine stack overflow.");
            }

            Lexer = new Type2CharStringLexer(subrs[subrIndex].Content);
            subrs[subrIndex].Used = true;

            ExecCharString();

            Lexer = parkedLexers.Count > 0
                ? parkedLexers.Pop()
                : Type2CharStringLexer.EmptyLexer;
        }
    }
}
