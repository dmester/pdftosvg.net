// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PdfToSvg.Parsing
{
    internal abstract class Parser
    {
        private const int MaxDepth = 100;

        protected readonly Lexer lexer;

        public Parser(Lexer lexer)
        {
            this.lexer = lexer;
        }

        protected object?[] ReadArray(int depth)
        {
            var result = new List<object?>();

            lexer.Read(); // Start

            while (true)
            {
                var nextLexeme = lexer.Peek();

                if (nextLexeme.Token == Token.EndArray)
                {
                    lexer.Read();
                    break;
                }

                result.Add(ReadValue(depth + 1));
            }

            return result.ToArray();
        }

        protected PdfDictionary ReadDictionary(int depth)
        {
            var result = new PdfDictionary();

            lexer.Read(); // Start

            while (true)
            {
                var nextLexeme = lexer.Read();

                PdfName name;

                if (nextLexeme.Token == Token.EndDictionary)
                {
                    break;
                }
                else if (nextLexeme.Token == Token.Name)
                {
                    name = nextLexeme.NameValue;
                }
                else
                {
                    throw ParserExceptions.UnexpectedToken(lexer.Stream, nextLexeme);
                }

                nextLexeme = lexer.Peek();

                if (nextLexeme.Token == Token.EndDictionary)
                {
                    lexer.Read();
                    break;
                }
                else
                {
                    result[name] = ReadValue(depth + 1);
                }
            }

            return result;
        }

        protected static PdfRef? ParseRef(Lexeme objectNumberLexeme, Lexeme generationLexeme)
        {
            static int? ParseNumber(Lexeme lexeme)
            {
                if (lexeme.Token == Token.Integer)
                {
                    return (int)lexeme.NumericValue;
                }
                
                //
                // Real numbers are not allowed in the spec, but we need to support them because:
                //
                // * We parse large numbers as doubles
                // * Other readers tolerates real object references
                //
                if (lexeme.Token == Token.Real &&
                    lexeme.NumericValue >= 0 &&
                    lexeme.NumericValue < int.MaxValue)
                {
                    return (int)lexeme.NumericValue;
                }
                
                return null;
            }
            
            var objectNumber = ParseNumber(objectNumberLexeme);
            if (objectNumber == null)
            {
                return null;
            }

            var generation = ParseNumber(generationLexeme);
            if (generation == null)
            {
                return null;
            }

            return new PdfRef(objectNumber.Value, generation.Value);
        }

        protected object? ReadNumberOrRef()
        {
            var value1Lexeme = lexer.Read();

            var next1Lexeme = lexer.Peek();
            if (next1Lexeme.Token == Token.Integer ||
                next1Lexeme.Token == Token.Real)
            {
                var next2Lexeme = lexer.Peek2();
                if (next2Lexeme.Token == Token.Ref)
                {
                    lexer.Read();
                    lexer.Read();

                    return ParseRef(value1Lexeme, next1Lexeme);
                }
            }

            if (value1Lexeme.Token == Token.Integer)
            {
                return (int)value1Lexeme.NumericValue;
            }

            return value1Lexeme.NumericValue;
        }

        protected bool TryReadToken(Token token)
        {
            Lexeme nextLexeme;

            try
            {
                nextLexeme = lexer.Peek();
            }
            catch
            {
                return false;
            }

            if (nextLexeme.Token != token)
            {
                return false;
            }

            lexer.Read();
            return true;
        }

        protected bool TryReadIntegerRelaxed(out int result)
        {
            Lexeme nextLexeme;

            try
            {
                nextLexeme = lexer.Peek();
            }
            catch
            {
                result = 0;
                return false;
            }

            if (nextLexeme.Token == Token.Integer)
            {
                lexer.Read();

                result = (int)nextLexeme.NumericValue;
                return true;
            }

            // Relaxed parsing:
            // Other readers are quite tolerant accepting real numbers where there should be integers
            if (nextLexeme.Token == Token.Real)
            {
                // Do not allow saturation
                if (nextLexeme.NumericValue >= int.MinValue && nextLexeme.NumericValue <= int.MaxValue)
                {
                    // Do not allow conversion if it alters the value
                    result = (int)nextLexeme.NumericValue;
                    if (result == nextLexeme.NumericValue)
                    {
                        lexer.Read();
                        return true;
                    }
                }
            }

            result = 0;
            return false;
        }

        protected object? ReadValue(int depth = 0)
        {
            if (depth > MaxDepth)
            {
                throw ParserExceptions.MaxDepthExceeded(lexer.Stream);
            }

            var nextLexeme = lexer.Peek();

            switch (nextLexeme.Token)
            {
                case Token.BeginArray:
                    return ReadArray(depth);

                case Token.BeginDictionary:
                    return ReadDictionary(depth);

                case Token.False:
                    lexer.Read();
                    return false;

                case Token.True:
                    lexer.Read();
                    return true;

                case Token.Null:
                    lexer.Read();
                    return null;

                case Token.Real:
                case Token.Integer:
                    return ReadNumberOrRef();

                case Token.HexString:
                case Token.LiteralString:
                    lexer.Read();
                    return nextLexeme.ObjectValue;

                case Token.Name:
                    lexer.Read();
                    return nextLexeme.ObjectValue;

                default:
                    throw ParserExceptions.UnexpectedToken(lexer.Stream, nextLexeme);
            }
        }
    }
}
