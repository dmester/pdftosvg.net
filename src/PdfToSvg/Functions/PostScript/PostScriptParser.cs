// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Parsing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace PdfToSvg.Functions.PostScript
{
    internal class PostScriptParser
    {
        private static readonly Dictionary<string, PostScriptOpCode> operators = new(StringComparer.Ordinal)
        {
            { "add", PostScriptOpCode.Add },
            { "sub", PostScriptOpCode.Sub },
            { "mul", PostScriptOpCode.Mul },
            { "div", PostScriptOpCode.Div },
            { "idiv", PostScriptOpCode.Idiv },
            { "mod", PostScriptOpCode.Mod },
            { "neg", PostScriptOpCode.Neg },
            { "abs", PostScriptOpCode.Abs },
            { "ceiling", PostScriptOpCode.Ceiling },
            { "floor", PostScriptOpCode.Floor },
            { "round", PostScriptOpCode.Round },
            { "truncate", PostScriptOpCode.Truncate },
            { "sqrt", PostScriptOpCode.Sqrt },
            { "sin", PostScriptOpCode.Sin },
            { "cos", PostScriptOpCode.Cos },
            { "atan", PostScriptOpCode.Atan },
            { "exp", PostScriptOpCode.Exp },
            { "ln", PostScriptOpCode.Ln },
            { "log", PostScriptOpCode.Log },
            { "cvi", PostScriptOpCode.Cvi },
            { "cvr", PostScriptOpCode.Cvr },
            { "eq", PostScriptOpCode.Eq },
            { "ne", PostScriptOpCode.Ne },
            { "gt", PostScriptOpCode.Gt },
            { "ge", PostScriptOpCode.Ge },
            { "lt", PostScriptOpCode.Lt },
            { "le", PostScriptOpCode.Le },
            { "and", PostScriptOpCode.And },
            { "or", PostScriptOpCode.Or },
            { "xor", PostScriptOpCode.Xor },
            { "not", PostScriptOpCode.Not },
            { "bitshift", PostScriptOpCode.BitShift },
            { "true", PostScriptOpCode.True },
            { "false", PostScriptOpCode.False },
            { "if", PostScriptOpCode.If },
            { "ifelse", PostScriptOpCode.IfElse },
            { "pop", PostScriptOpCode.Pop },
            { "exch", PostScriptOpCode.Exch },
            { "dup", PostScriptOpCode.Dup },
            { "copy", PostScriptOpCode.Copy },
            { "index", PostScriptOpCode.Index },
            { "roll", PostScriptOpCode.Roll },
        };

        private readonly Lexer lexer;

        private PostScriptParser(Stream stream)
        {
            lexer = new Lexer(stream);
        }

        private PostScriptParser(string source)
        {
            lexer = new Lexer(source);
        }

        public static PostScriptExpression Parse(Stream stream)
        {
            var parser = new PostScriptParser(stream);
            return parser.ReadFunction();
        }

        public static PostScriptExpression Parse(string source)
        {
            var parser = new PostScriptParser(source);
            return parser.ReadFunction();
        }

        private PostScriptExpression ReadFunction()
        {
            var blocks = new List<PostScriptExpression>();
            var nextLexeme = lexer.Read();

            if (nextLexeme.Token != Token.BeginBlock)
            {
                throw new ParserException($"Expected starting '{{' but found {nextLexeme} in PostScript function.", nextLexeme.Position);
            }

            return ReadBlock(blocks, 0);
        }

        private PostScriptExpression ReadBlock(List<PostScriptExpression> blocks, int depth)
        {
            if (depth >= PostScriptLimits.MaxExecutionDepth)
            {
                throw new PostScriptFunctionException("PostScript block nesting limit exceeded.");
            }

            var instructions = new List<PostScriptInstruction>();
            var continueReading = true;

            do
            {
                var nextLexeme = lexer.Read();

                switch (nextLexeme.Token)
                {
                    case Token.BeginBlock:
                        var block = ReadBlock(blocks, depth + 1);
                        instructions.Add(new PostScriptInstruction(PostScriptOpCode.PushValue, new PostScriptValue(PostScriptValueType.Expression, blocks.Count)));
                        blocks.Add(block);
                        break;

                    case Token.EndBlock:
                        continueReading = false;
                        break;

                    case Token.EndOfInput:
                        continueReading = false;
                        break;

                    case Token.Integer:
                    case Token.Real:
                        var value = new PostScriptValue(nextLexeme.NumericValue);
                        instructions.Add(new PostScriptInstruction(PostScriptOpCode.PushValue, value));
                        break;

                    case Token.Keyword:
                        var operatorName = nextLexeme.KeywordValue;
                        if (operators.TryGetValue(operatorName, out var op))
                        {
                            instructions.Add(new PostScriptInstruction(op));
                        }
                        else
                        {
                            throw new ParserException($"Unknown PostScript function operator {operatorName}.", nextLexeme.Position);
                        }
                        break;

                    default:
                        throw new ParserException($"Unexpected token {nextLexeme} in PostScript function.", nextLexeme.Position);
                }
            }
            while (continueReading);

            return new PostScriptExpression(instructions, blocks);
        }
    }
}
