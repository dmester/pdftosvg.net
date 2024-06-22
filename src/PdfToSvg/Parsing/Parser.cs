// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PdfToSvg.Parsing
{
    internal abstract class Parser
    {
        protected readonly Lexer lexer;

        public Parser(Lexer lexer)
        {
            this.lexer = lexer;
        }

        protected object?[] ReadArray()
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

                result.Add(ReadValue());
            }

            return result.ToArray();
        }

        protected PdfDictionary ReadDictionary()
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
                    name = PdfName.Create(nextLexeme.Value.ToString());
                }
                else
                {
                    throw new Exception();
                }

                nextLexeme = lexer.Peek();

                if (nextLexeme.Token == Token.EndDictionary)
                {
                    lexer.Read();
                    break;
                }
                else
                {
                    result[name] = ReadValue();
                }
            }

            return result;
        }

        protected PdfDictionary ReadInlineImageDictionary()
        {
            var dictionary = new PdfDictionary();

            var nextLexeme = lexer.Read();

            while (true)
            {
                nextLexeme = lexer.Read();

                PdfName name;

                if (nextLexeme.Token == Token.BeginImageData ||
                    nextLexeme.Token == Token.EndImage)
                {
                    break;
                }
                else if (nextLexeme.Token == Token.Name)
                {
                    name = PdfName.Create(nextLexeme.Value.ToString());
                }
                else
                {
                    throw new Exception();
                }

                nextLexeme = lexer.Peek();

                if (nextLexeme.Token == Token.BeginImageData ||
                    nextLexeme.Token == Token.EndImage)
                {
                    lexer.Read();
                    break;
                }
                else
                {
                    dictionary[name] = ReadValue();
                }
            }

            InlineImageHelper.DeabbreviateInlineImageDictionary(dictionary);

            if (nextLexeme.Token == Token.BeginImageData)
            {
                // According to PDF spec 1.7, page 223, ID should be followed by a single whitespace character.
                if (PdfCharacters.IsWhiteSpace(lexer.Stream.PeekChar()))
                {
                    lexer.Stream.Skip();
                }

                // Detect stream length
                var startPosition = lexer.Stream.Position;
                var streamLength = InlineImageHelper.DetectStreamLength(lexer.Stream, dictionary[Names.Filter]);

                // Read stream data
                var imageData = new byte[streamLength];
                lexer.Stream.Position = startPosition;
                lexer.Stream.Read(imageData, 0, streamLength);

                dictionary.MakeIndirectObject(default, new PdfMemoryStream(dictionary, imageData, streamLength));

                if (lexer.Peek().Token == Token.EndImage)
                {
                    lexer.Read();
                }
            }
            else if (nextLexeme.Token == Token.EndImage)
            {
                // Missing stream
            }

            return dictionary;
        }

        protected object ReadIntegerOrRef()
        {
            var value1Lexeme = lexer.Read();
            var value1 = value1Lexeme.IntValue;

            var next1Lexeme = lexer.Peek(1);

            if (next1Lexeme.Token == Token.Integer)
            {
                var next2Lexeme = lexer.Peek(2);
                if (next2Lexeme.Token == Token.Ref)
                {
                    lexer.Read();
                    lexer.Read();

                    var value2 = next1Lexeme.IntValue;
                    return new PdfRef(value1, value2);
                }
            }

            return value1;
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

        protected bool TryReadInteger(out int result)
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

            if (nextLexeme.Token != Token.Integer)
            {
                result = 0;
                return false;
            }

            lexer.Read();

            result = nextLexeme.IntValue;
            return true;
        }

        protected object? ReadValue()
        {
            var nextLexeme = lexer.Peek();

            switch (nextLexeme.Token)
            {
                case Token.BeginArray:
                    return ReadArray();

                case Token.BeginDictionary:
                    return ReadDictionary();

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
                    lexer.Read();
                    return double.Parse(nextLexeme.Value.ToString(), CultureInfo.InvariantCulture);

                case Token.Integer:
                    return ReadIntegerOrRef();

                case Token.HexString:
                case Token.LiteralString:
                    lexer.Read();
                    return nextLexeme.Value;

                case Token.Name:
                    lexer.Read();
                    return PdfName.Create(nextLexeme.Value.ToString());

                default:
                    throw ParserExceptions.UnexpectedToken(lexer.Stream, nextLexeme);
            }
        }
    }
}
