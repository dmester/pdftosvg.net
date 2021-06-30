// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Parsing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Functions.PostScript
{
    internal class PostScriptParser
    {
        private readonly Lexer lexer;

        private PostScriptParser(Stream stream)
        {
            lexer = new Lexer(stream);
        }

        public static PostScriptExpression Parse(Stream stream)
        {
            var parser = new PostScriptParser(stream);
            return parser.ReadFunction();
        }

        private PostScriptExpression ReadFunction()
        {
            var nextLexeme = lexer.Read();

            if (nextLexeme.Token != Token.BeginBlock)
            {
                throw new PdfParserException($"Expected starting '{{' but found {nextLexeme} in PostScript function.", nextLexeme.Position);
            }

            return ReadBlock();
        }

        private PostScriptExpression ReadBlock()
        {
            var instructions = new List<PostScriptInstruction>();
            var continueReading = true;

            do
            {
                var nextLexeme = lexer.Read();

                switch (nextLexeme.Token)
                {
                    case Token.BeginBlock:
                        var block = ReadBlock();
                        instructions.Add(stack => stack.Push(block));
                        break;

                    case Token.EndBlock:
                        continueReading = false;
                        break;

                    case Token.EndOfInput:
                        continueReading = false;
                        break;

                    case Token.Integer:
                        var iValue = int.Parse(nextLexeme.Value.ToString(), CultureInfo.InvariantCulture);
                        instructions.Add(stack => stack.Push(iValue));
                        break;

                    case Token.Real:
                        var dblValue = double.Parse(nextLexeme.Value.ToString(), CultureInfo.InvariantCulture);
                        instructions.Add(stack => stack.Push(dblValue));
                        break;

                    case Token.Keyword:
                        var operatorName = nextLexeme.Value.ToString();
                        if (PostScriptOperators.TryGetOperator(operatorName, out var op))
                        {
                            instructions.Add(op);
                        }
                        else
                        {
                            throw new PdfParserException($"Unknown PostScript function operator {operatorName}.", nextLexeme.Position);
                        }
                        break;

                    default:
                        throw new PdfParserException($"Unexpected token {nextLexeme} in PostScript function.", nextLexeme.Position);
                }
            }
            while (continueReading);

            return new PostScriptExpression(instructions);
        }
    }
}
