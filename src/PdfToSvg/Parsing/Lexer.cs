// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.IO;
using System;
using System.IO;
using System.Text;

namespace PdfToSvg.Parsing
{
    internal class Lexer
    {
        private LexerBuffer stringBuffer = new LexerBuffer(128);

        private Lexeme forwardLexeme1;
        private Lexeme forwardLexeme2;
        private int forwardLexemeCount;

        private static readonly LexerKeywordTable emptyKeywordTable = new();
        private LexerKeywordTable keywords;
        private bool isFaulted;

        public Lexer(ArraySegment<byte> source, LexerKeywordTable? keywords = null) : this(
            stream: new BufferedMemoryReader(source),
            keywords)
        { }

        public Lexer(byte[] source, LexerKeywordTable? keywords = null) : this(
            stream: new BufferedMemoryReader(source),
            keywords)
        { }

        public Lexer(string source, LexerKeywordTable? keywords = null) : this(
            stream: new BufferedMemoryReader(Encoding.ASCII.GetBytes(source)),
            keywords)
        { }

        public Lexer(Stream stream, LexerKeywordTable? keywords = null)
        {
            Stream = stream as BufferedReader ?? new BufferedStreamReader(stream);
            this.keywords = keywords ?? emptyKeywordTable;
        }

        public BufferedReader Stream { get; }

        public long Seek(long offset, SeekOrigin origin)
        {
            Reset();
            return Stream.Seek(offset, origin);
        }

        public void Reset()
        {
            forwardLexemeCount = 0;
            stringBuffer.Clear();
            isFaulted = false;
        }

        protected void SetFaulted()
        {
            Reset();
            isFaulted = true;
        }

        private Lexeme CreateKeywordLexeme(long startPosition)
        {
            var keyword = stringBuffer.ToString();
            stringBuffer.Clear();
            return new Lexeme(Token.Keyword, keyword, startPosition);
        }

        private Lexeme CreateKeywordLexeme(string keyword, long startPosition)
        {
            stringBuffer.Clear();
            return new Lexeme(Token.Keyword, keyword, startPosition);
        }

        protected Lexeme CreateStringLexeme(Token token, long position)
        {
            var value = stringBuffer.ToPdfString();
            stringBuffer.Clear();
            return new Lexeme(token, value, position);
        }

        protected Lexeme CreateTokenLexeme(Token token, long position)
        {
            return new Lexeme(token, position: position);
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

            stringBuffer.Clear();
            stringBuffer.WriteChar(nextChar);

            var keyword = default(LexerKeyword);

            while (true)
            {
                nextChar = Stream.PeekChar();

                if (LexerKeywordTable.IsKeywordChar(nextChar))
                {
                    Stream.Skip();
                    stringBuffer.WriteChar(nextChar);
                }
                else
                {
                    if (!keywords.TryGetKeyword(stringBuffer, out keyword) &&
                        keywords.TryGetMaxLength(stringBuffer, out var maxLength))
                    {
                        var lookaheadChars = maxLength - stringBuffer.Length;
                        var initialLength = stringBuffer.Length;
                        var found = false;

                        for (var i = 0; i < lookaheadChars; i++)
                        {
                            nextChar = Stream.PeekChar(i + 1);
                            stringBuffer.WriteChar(nextChar);

                            if (keywords.TryGetKeyword(stringBuffer, out keyword))
                            {
                                Stream.Skip(i + 1);
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            stringBuffer.Length = initialLength;
                        }
                    }

                    break;
                }
            }

            if (stringBuffer.Length == 1)
            {
                keywords.TryGetKeyword(stringBuffer, out keyword);
            }

            if (keyword.Token != Token.None)
            {
                if (keyword.Token == Token.Keyword)
                {
                    return CreateKeywordLexeme(keyword.Keyword, startPosition);
                }

                if (keyword.Token == Token.Stream)
                {
                    // According to ISO 32000-2-2020 section 7.3.8, the `stream` keyword must be followed by either
                    // CR+LF or LF, but not CR alone. In #60 it was however followed by only CR.
                    //
                    // Some PDFs (e.g. SafeDocs corpus 1008550, 2048842, 7931219) also have spaces between the keyword
                    // and the line break.
                    //
                    // Both cases are accepted by other readers, including MuPDF and PDF.js, so we need to handle them
                    // as well.

                    nextChar = Stream.PeekChar();

                    while (nextChar == ' ' || nextChar == '\t' || nextChar == '\f')
                    {
                        Stream.Skip();
                        nextChar = Stream.PeekChar();
                    }

                    if (nextChar == '\r')
                    {
                        // Carriage return, possibly followed by line feed
                        Stream.Skip();
                        nextChar = Stream.PeekChar();
                    }

                    if (nextChar == '\n')
                    {
                        // Line feed
                        Stream.Skip();
                    }
                }

                stringBuffer.Clear();
                return new Lexeme(keyword.Token, position: startPosition);
            }
            else
            {
                return CreateKeywordLexeme(startPosition);
            }
        }

        protected Lexeme ReadNumber()
        {
            var startPosition = Stream.Position;

            const long MaxMantissaBeforeExtension = 1L << 54; // 52 fraction bits in doubles

            Token token;
            var negative = false;
            var mantissa = 0L;
            var exponent = 0;

            // Sign
            var nextChar = Stream.PeekChar();
            if (nextChar == '-' || nextChar == '+')
            {
                negative = nextChar == '-';
                Stream.Skip();
                nextChar = Stream.PeekChar();
            }

            // Integer digits
            while (nextChar >= '0' && nextChar <= '9')
            {
                if (mantissa < MaxMantissaBeforeExtension)
                {
                    mantissa = mantissa * 10 + (nextChar - '0');
                }
                else
                {
                    exponent++;
                }

                Stream.Skip();
                nextChar = Stream.PeekChar();
            }

            // Decimal point
            var hasDecimals = nextChar == '.';
            if (hasDecimals)
            {
                Stream.Skip();
                nextChar = Stream.PeekChar();

                // Decimal digits
                while (nextChar >= '0' && nextChar <= '9')
                {
                    if (mantissa < MaxMantissaBeforeExtension)
                    {
                        mantissa = mantissa * 10 + (nextChar - '0');
                        exponent--;
                    }

                    Stream.Skip();
                    nextChar = Stream.PeekChar();
                }

                token = Token.Real;
            }
            else
            {
                // Let integers that are outside the range of Int32 to be typed as reals
                token = mantissa <= (negative ? -(long)int.MinValue : int.MaxValue)
                    ? Token.Integer
                    : Token.Real;
            }

            double result;

            if (exponent == 0)
            {
                result = mantissa;
            }
            else if (exponent < 0)
            {
                exponent = -exponent;

                var multiplier = exponent switch
                {
                    1 => 1e1,
                    2 => 1e2,
                    3 => 1e3,
                    4 => 1e4,
                    5 => 1e5,
                    6 => 1e6,
                    7 => 1e7,
                    8 => 1e8,
                    9 => 1e9,
                    _ => Math.Pow(10, exponent),
                };

                result = mantissa / multiplier;
            }
            else
            {
                result = mantissa * Math.Pow(10, exponent);
            }

            if (negative)
            {
                result = -result;
            }

            return new Lexeme(token, result, startPosition);
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

            Stream.ReadChar('(');

            while (true)
            {
                var nextChar = Stream.ReadChar();

                if (nextChar == BufferedReader.EndOfStreamMarker)
                {
                    var errorPosition = Stream.Position;
                    Reset();
                    return CreateTokenLexeme(Token.EndOfInput, errorPosition);
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

            return CreateStringLexeme(Token.LiteralString, startPosition);
        }

        protected Lexeme ReadName()
        {
            var startPosition = Stream.Position;
            Stream.ReadChar('/');

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
                    stringBuffer.WriteChar(nextChar);
                }
            }

            if (!Names.TryLookup(stringBuffer.Buffer, stringBuffer.Length, out var value))
            {
                value = new PdfName(stringBuffer.ToString());
            }
            stringBuffer.Clear();

            return new Lexeme(Token.Name, value, startPosition);
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

            Stream.ReadChar('<');
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
                    var errorPosition = Stream.Position;
                    Stream.Skip();
                    SetFaulted();
                    return CreateTokenLexeme(Token.UnexpectedCharacter, errorPosition);
                }

                nextChar = Stream.PeekChar();
            }

            if (hi >= 0)
            {
                stringBuffer.WriteByte(unchecked((byte)hi));
            }

            return CreateStringLexeme(Token.HexString, startPosition);
        }

        protected Lexeme ReadDictionaryOrHexString()
        {
            if (Stream.PeekChar(2) == '<')
            {
                var beginDict = CreateTokenLexeme(Token.BeginDictionary, Stream.Position);
                Stream.Skip(2);
                return beginDict;
            }

            return ReadHexString();
        }

        public Lexeme Peek()
        {
            if (forwardLexemeCount == 0)
            {
                forwardLexeme1 = ReadLexeme();
                forwardLexemeCount = 1;
            }

            return forwardLexeme1;
        }

        public Lexeme Peek2()
        {
            if (forwardLexemeCount == 0)
            {
                forwardLexeme1 = ReadLexeme();
                forwardLexemeCount = 1;
            }

            if (forwardLexemeCount == 1)
            {
                forwardLexeme2 = ReadLexeme();
                forwardLexemeCount = 2;
            }

            return forwardLexeme2;
        }

        public Lexeme Read()
        {
            if (forwardLexemeCount > 0)
            {
                var result = forwardLexeme1;
                forwardLexeme1 = forwardLexeme2;
                forwardLexemeCount--;
                return result;
            }

            return ReadLexeme();
        }


        public Lexeme Read(Token expectedToken)
        {
            var lexeme = Read();

            if (lexeme.Token != expectedToken)
            {
                throw new ParserException($"Expected {expectedToken} but found {lexeme.Token}.", lexeme.Position);
            }

            return lexeme;
        }

        public bool TryRead(Token expectedToken) => TryRead(expectedToken, out _);

        public bool TryRead(Token expectedToken, out Lexeme lexeme)
        {
            lexeme = Peek();

            if (lexeme.Token == expectedToken)
            {
                Read();
                return true;
            }

            return false;
        }

        private Lexeme ReadLexeme()
        {
            if (isFaulted)
            {
                return new Lexeme(Token.EndOfInput, position: Stream.Position);
            }

            var result = default(Lexeme);

            do
            {
                SkipWhiteSpace();

                var nextChar = Stream.PeekChar();
                if (nextChar == BufferedReader.EndOfStreamMarker)
                {
                    return new Lexeme(Token.EndOfInput, position: Stream.Position);
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
                        result = CreateTokenLexeme(Token.EndDictionary, Stream.Position);
                        Stream.Skip(2);
                        break;

                    case '[':
                        result = CreateTokenLexeme(Token.BeginArray, Stream.Position);
                        Stream.Skip();
                        break;

                    case ']':
                        result = CreateTokenLexeme(Token.EndArray, Stream.Position);
                        Stream.Skip();
                        break;

                    case '{':
                        result = CreateTokenLexeme(Token.BeginBlock, Stream.Position);
                        Stream.Skip();
                        break;

                    case '}':
                        result = CreateTokenLexeme(Token.EndBlock, Stream.Position);
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

                    case '-' when Stream.PeekChar(2) == '|':
                        // In Type 1 fonts, -| can be an alias for RD
                        result = CreateTokenLexeme(Token.RD, Stream.Position);
                        Stream.Skip(2);
                        break;

                    case '|':
                        // In Type 1 fonts, |- can be an alias for ND, and | for NP
                        if (Stream.PeekChar(2) == '-')
                        {
                            result = CreateTokenLexeme(Token.ND, Stream.Position);
                            Stream.Skip(2);
                        }
                        else
                        {
                            result = CreateTokenLexeme(Token.NP, Stream.Position);
                            Stream.Skip(1);
                        }
                        break;

                    case '+':
                    case '-':
                    case '.':
                    case >= '0' and <= '9':
                        result = ReadNumber();
                        break;

                    default:
                        result = CreateTokenLexeme(Token.UnexpectedCharacter, Stream.Position);
                        Stream.Skip();
                        SetFaulted();
                        break;
                }
            }
            while (result.Token == Token.None);

            return result;
        }
    }
}
