// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PdfToSvg.Parsing
{
    internal class Lexer
    {
        private MemoryStream stringBuffer = new MemoryStream();
        private ForwardReadBuffer<Lexeme> forwardLexemeBuffer;
        private Dictionary<string, Token> keywords;

        public Lexer(string source, Dictionary<string, Token>? keywords = null)
        {
            var bytes = Encoding.ASCII.GetBytes(source);
            Stream = new BufferedMemoryReader(bytes);
            forwardLexemeBuffer = new ForwardReadBuffer<Lexeme>(ReadLexeme, 2);
            this.keywords = keywords ?? new Dictionary<string, Token>();
        }

        public Lexer(Stream stream, Dictionary<string, Token>? keywords)
        {
            Stream = stream as BufferedReader ?? new BufferedStreamReader(stream);
            forwardLexemeBuffer = new ForwardReadBuffer<Lexeme>(ReadLexeme, 2);
            this.keywords = keywords ?? new Dictionary<string, Token>();
        }

        public BufferedReader Stream { get; }

        public long Seek(long offset, SeekOrigin origin)
        {
            Reset();
            return Stream.Seek(offset, origin);
        }

        public void Reset()
        {
            forwardLexemeBuffer.Clear();
            stringBuffer.SetLength(0);
        }

        protected Lexeme CreateLexeme(Token token, long position)
        {
            var value = new PdfString(stringBuffer);
            stringBuffer.SetLength(0);
            return new Lexeme(token, position, value);
        }

        protected bool SkipWhiteSpace()
        {
            var foundWhitespace = false;

            while (PdfCharacters.IsWhiteSpace(Stream.PeekChar()))
            {
                foundWhitespace = true;
                Stream.Skip();
            }

            return foundWhitespace;
        }

        protected bool SkipComment()
        {
            if (Stream.PeekChar() == '%')
            {
                char nextChar;

                do
                {
                    Stream.Skip();
                    nextChar = Stream.PeekChar();
                }
                while (nextChar != '\r' && nextChar != '\n' && nextChar != BufferedReader.EndOfStreamMarker);

                return true;
            }

            return false;
        }

        private Lexeme ReadKeyword()
        {
            var startPosition = Stream.Position;
            var nextChar = Stream.ReadChar();

            stringBuffer.WriteByte((byte)nextChar);

            while (true)
            {
                nextChar = Stream.PeekChar();

                if (PdfCharacters.IsLetter(nextChar) ||
                    nextChar == '*' ||
                    nextChar == '\'' ||
                    nextChar == '"')
                {
                    Stream.Skip();
                    stringBuffer.WriteByte((byte)nextChar);
                }
                else
                {
                    break;
                }
            }

            var keywordName = new PdfString(stringBuffer);
            stringBuffer.SetLength(0);
            
            if (keywords.TryGetValue(keywordName.ToString(), out var keyword))
            {
                if (keyword == Token.Stream)
                {
                    nextChar = Stream.PeekChar();

                    if (nextChar == '\r')
                    {
                        Stream.Skip(2); // CR + LF
                    }
                    else if (nextChar == '\n')
                    {
                        Stream.Skip(); // LF
                    }
                }

                return new Lexeme(keyword, startPosition);
            }
            else
            {
                return new Lexeme(Token.Keyword, startPosition, keywordName);
            }
        }

        protected Lexeme ReadNumber()
        {
            var startPosition = Stream.Position;
            var inDecimal = false;
            bool proceed;

            do
            {
                var nextChar = Stream.PeekChar();

                proceed = false;

                switch (nextChar)
                {
                    case '+':
                    case '-':
                        if (stringBuffer.Length == 0)
                        {
                            stringBuffer.WriteByte((byte)nextChar);
                            Stream.Skip();
                            proceed = true;
                        }
                        break;

                    case '.':
                        if (!inDecimal)
                        {
                            stringBuffer.WriteByte((byte)nextChar);
                            Stream.Skip();
                            inDecimal = true;
                            proceed = true;
                        }
                        break;

                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        stringBuffer.WriteByte((byte)nextChar);
                        Stream.Skip();
                        proceed = true;
                        break;
                }
            }
            while (proceed);

            return CreateLexeme(inDecimal ? Token.Real : Token.Integer, startPosition);
        }

        protected byte ReadOctalByte()
        {
            var result = 0;

            for (var i = 0; i < 3; i++)
            {
                var nextChar = Stream.PeekChar();

                if (nextChar >= '0' && nextChar <= '7')
                {
                    Stream.Skip();
                    result = (result << 3) | (nextChar - '0');
                }
            }

            return unchecked((byte)result);
        }

        protected Lexeme ReadLiteralString()
        {
            var startPosition = Stream.Position;
            var expectedEndParentheses = 0;

            Stream.Skip(); // TODO assert? Starting (

            while (true)
            {
                var nextChar = Stream.ReadChar();

                if (nextChar == BufferedReader.EndOfStreamMarker)
                {
                    throw Exceptions.UnexpectedCharacter(Stream, BufferedReader.EndOfStreamMarker);
                }
                else if (nextChar == '(')
                {
                    expectedEndParentheses++;
                    stringBuffer.WriteByte((byte)'(');
                }
                else if (nextChar == ')')
                {
                    if (expectedEndParentheses > 0)
                    {
                        expectedEndParentheses--;
                        stringBuffer.WriteByte((byte)')');
                    }
                    else
                    {
                        break;
                    }
                }
                else if (nextChar == '\r')
                {
                    if (Stream.PeekChar() == '\n')
                    {
                        Stream.Skip();
                    }

                    // Spec says new lines are inserted as LF, regardless of if they are represented
                    // as CR, LF, or both.
                    stringBuffer.WriteByte((byte)'\n');
                }
                else if (nextChar == '\\')
                {
                    nextChar = Stream.PeekChar();

                    if (nextChar == 'n') stringBuffer.WriteByte((byte)'\n');
                    else if (nextChar == 'r') stringBuffer.WriteByte((byte)'\r');
                    else if (nextChar == 't') stringBuffer.WriteByte((byte)'\t');
                    else if (nextChar == 'b') stringBuffer.WriteByte((byte)'\b');
                    else if (nextChar == 'f') stringBuffer.WriteByte((byte)'\f');
                    else if (nextChar == '(') stringBuffer.WriteByte((byte)'(');
                    else if (nextChar == ')') stringBuffer.WriteByte((byte)')');
                    else if (nextChar == '\\') stringBuffer.WriteByte((byte)'\\');
                    else if (nextChar >= '0' && nextChar <= '7') // Octal
                    {
                        stringBuffer.WriteByte(ReadOctalByte());
                        continue;
                    }
                    else if (nextChar == '\n')
                    {
                        // \ at the end of a line indicates that the string continues on the next line.
                        // The line break itself should be ignored.
                        Stream.Skip();
                        continue;
                    }
                    else if (nextChar == '\r')
                    {
                        // \ at the end of a line indicates that the string continues on the next line.
                        // The line break itself should be ignored.
                        Stream.Skip();

                        if (Stream.PeekChar() == '\n')
                        {
                            Stream.Skip();
                        }

                        continue;
                    }
                    else
                    {
                        // Unknown escape character. \ should be ignored according to spec
                        continue;
                    }

                    Stream.Skip();
                }
                else
                {
                    stringBuffer.WriteByte((byte)nextChar);
                }
            }

            return CreateLexeme(Token.LiteralString, startPosition);
        }

        protected Lexeme ReadName()
        {
            var startPosition = Stream.Position;
            Stream.Skip();

            while (true)
            {
                var nextChar = Stream.PeekChar();

                if (nextChar == '#')
                {
                    Stream.Skip();
                    var hexByte = ReadHexByte();
                    stringBuffer.WriteByte(hexByte < 0 ? (byte)'#' : (byte)hexByte);
                }
                else if (
                    nextChar == BufferedReader.EndOfStreamMarker || 
                    PdfCharacters.IsWhiteSpace(nextChar) || 
                    PdfCharacters.IsDelimiter(nextChar))
                {
                    break;
                }
                else
                {
                    Stream.Skip();
                    stringBuffer.WriteByte((byte)nextChar);
                }
            }

            return CreateLexeme(Token.Name, startPosition);
        }

        private int ReadHexByte()
        {
            var result = PdfCharacters.ParseHexDigit(Stream.PeekChar()) << 4;
            if (result >= 0)
            {
                Stream.Skip();

                var lo = PdfCharacters.ParseHexDigit(Stream.PeekChar());
                if (lo >= 0)
                {
                    result |= lo;
                    Stream.Skip();
                }
            }

            return result;
        }

        protected Lexeme ReadHexString()
        {
            var startPosition = Stream.Position;

            Stream.Skip(); // TODO assert
            var nextChar = Stream.PeekChar();

            var hi = -1;
            int digit;

            while (true)
            {
                digit = PdfCharacters.ParseHexDigit(nextChar);

                if (digit >= 0)
                {
                    Stream.Skip();

                    if (hi < 0)
                    {
                        hi = digit << 4;
                    }
                    else
                    {
                        stringBuffer.WriteByte(unchecked((byte)(hi | digit)));
                        hi = -1;
                    }
                }
                else if (PdfCharacters.IsWhiteSpace(nextChar))
                {
                    Stream.Skip();
                }
                else if (nextChar == '>')
                {
                    Stream.Skip();
                    break;
                }
                else
                {
                    throw Exceptions.UnexpectedCharacter(Stream, nextChar);
                }

                nextChar = Stream.PeekChar();
            }

            if (hi >= 0)
            {
                stringBuffer.WriteByte(unchecked((byte)hi));
            }

            return CreateLexeme(Token.HexString, startPosition);
        }

        protected Lexeme ReadDictionaryOrHexString()
        {
            if (Stream.PeekChar(2) == '<')
            {
                var beginDict = CreateLexeme(Token.BeginDictionary, Stream.Position);
                Stream.Skip(2);
                return beginDict;
            }

            return ReadHexString();
        }


        public Lexeme Peek() => forwardLexemeBuffer.Peek();

        public Lexeme Peek(int offset) => forwardLexemeBuffer.Peek(offset);

        public Lexeme Read() => forwardLexemeBuffer.Read();

        private Lexeme ReadLexeme()
        {
            var result = default(Lexeme);

            do
            {
                SkipWhiteSpace();

                var nextChar = Stream.PeekChar();
                if (nextChar == BufferedReader.EndOfStreamMarker)
                {
                    return new Lexeme(Token.EndOfInput, Stream.Position);
                }

                switch (nextChar)
                {
                    case '/':
                        result = ReadName();
                        break;

                    case '(':
                        result = ReadLiteralString();
                        break;

                    case '<':
                        result = ReadDictionaryOrHexString();
                        break;

                    case '>' when Stream.PeekChar(2) == '>':
                        result = CreateLexeme(Token.EndDictionary, Stream.Position);
                        Stream.Skip(2);
                        break;

                    case '[':
                        result = CreateLexeme(Token.BeginArray, Stream.Position);
                        Stream.Skip();
                        break;

                    case ']':
                        result = CreateLexeme(Token.EndArray, Stream.Position);
                        Stream.Skip();
                        break;

                    case '%':
                        SkipComment();
                        break;

                    case '"':
                    case '\'':
                    case char _ when PdfCharacters.IsLetter(nextChar):
                        result = ReadKeyword();
                        break;

                    case '+': case '-': case '.':
                    case '0': case '1': case '2': case '3': case '4':
                    case '5': case '6': case '7': case '8': case '9':
                        result = ReadNumber();
                        break;

                    default:
                        throw Exceptions.UnexpectedCharacter(Stream, nextChar);
                }
            }
            while (result.Token == Token.None);

            return result;
        }
    }
}
