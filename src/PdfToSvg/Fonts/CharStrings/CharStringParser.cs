// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfToSvg.Fonts.CharStrings
{
    internal class CharStringParser
    {
        private readonly Stack<CharStringLexer> parkedLexers = new Stack<CharStringLexer>();

        private double[]? storage;

        private readonly IList<CharStringSubRoutine> globalSubrs, localSubrs;

        private CharStringParser(CharStringType type, ArraySegment<byte> data, IList<CharStringSubRoutine> globalSubrs, IList<CharStringSubRoutine> localSubrs)
        {
            Type = type;
            Lexer = new CharStringLexer(type, data);
            this.globalSubrs = globalSubrs;
            this.localSubrs = localSubrs;
        }

        public CharStringType Type { get; }

        public Stack<double> PostScriptStack { get; } = new Stack<double>();

        public CharStringStack Stack { get; } = new CharStringStack();

        public CharStringPath Path => CharString.Path;

        public List<Point>? FlexPoints { get; set; }

        public CharStringLexer Lexer { get; private set; }

        // The storage is limited to 32 fields according to spec, but it doesn't mention if the 
        // field indexes are zero or one based => allocate up to field 32.
        public double[] Storage => storage ??= new double[33];

        public CharStringInfo CharString { get; } = new CharStringInfo();

        public static CharString Parse(
            CharStringType type, ArraySegment<byte> data,
            IList<CharStringSubRoutine> globalSubrs, IList<CharStringSubRoutine> localSubrs)
        {
            var parser = new CharStringParser(type, data, globalSubrs, localSubrs);
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
                    Stack.Push(lexeme.Value);
                }
                else if (lexeme.Token == CharStringToken.Operator)
                {
                    if (CharStringOperators.TryGetOperator(lexeme.OpCode, out var op))
                    {
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

        public void AppendContent(CharStringOpCode code, int? last = null, int? from = null)
        {
            var startAt =
                last.HasValue ? Stack.Count - last.Value :
                from.HasValue ? from.Value :
                Stack.Count;

            for (var i = startAt; i < Stack.Count; i++)
            {
                CharString.Content.Add(CharStringLexeme.Operand(Stack[i]));
            }

            CharString.Content.Add(CharStringLexeme.Operator(code));
        }

        public void Return()
        {
            Lexer = CharStringLexer.EmptyLexer;
        }

        public void EndChar()
        {
            Lexer = CharStringLexer.EmptyLexer;
            parkedLexers.Clear();
        }

        public void CallSubr(int number, bool global)
        {
            var subrs = global ? globalSubrs : localSubrs;
            var subrIndex = number;

            if (Type == CharStringType.Type2)
            {
                var bias =
                    subrs.Count < 1240 ? 107 :
                    subrs.Count < 33900 ? 1131 :
                    32768;

                subrIndex += bias;
            }

            if (subrIndex < 0 || subrIndex >= subrs.Count || subrs[subrIndex] == null)
            {
                throw new CharStringException((global ? "Global" : "Local") + " subroutine with number " + number + " not found.");
            }

            parkedLexers.Push(Lexer);

            if (parkedLexers.Count > 10)
            {
                throw new CharStringException("Char string subroutine stack overflow.");
            }

            Lexer = new CharStringLexer(Type, subrs[subrIndex].Content);
            subrs[subrIndex].Used = true;

            ExecCharString();

            Lexer = parkedLexers.Count > 0
                ? parkedLexers.Pop()
                : CharStringLexer.EmptyLexer;
        }
    }
}
