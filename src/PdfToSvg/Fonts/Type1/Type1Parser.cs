// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using PdfToSvg.Fonts.CharStrings;
using PdfToSvg.Functions.PostScript;
using PdfToSvg.IO;
using PdfToSvg.Parsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace PdfToSvg.Fonts.Type1
{
    [LexerKeyword("def", Token.Def)]
    [LexerKeyword("dup", Token.Dup)]
    [LexerKeyword("array", Token.Array)]
    [LexerKeyword("begin", Token.Begin)]
    [LexerKeyword("dict", Token.Dict)]
    [LexerKeyword("RD", Token.RD)]
    [LexerKeyword("ND", Token.ND)]
    [LexerKeyword("NP", Token.NP)]
    internal sealed partial class Type1ParserKeywordTable : LexerKeywordTable
    {
    }

    internal class Type1Parser
    {
        private readonly Lexer lexer;

        private static readonly Dictionary<string, Action<Type1FontInfo, Lexer>> readers = new(StringComparer.Ordinal)
        {
            { "lenIV", (info, lexer) => ReadInteger(lexer, ref info.lenIV) },
            { "Notice", (info, lexer) => ReadString(lexer, ref info.Notice) },
            { "FullName", (info, lexer) => ReadString(lexer, ref info.FullName) },
            { "FamilyName", (info, lexer) => ReadString(lexer, ref info.FamilyName) },
            { "Weight", (info, lexer) => ReadString(lexer, ref info.Weight) },
            { "ItalicAngle", (info, lexer) => ReadDouble(lexer, ref info.ItalicAngle) },
            { "isFixedPitch", (info, lexer) => ReadBoolean(lexer, ref info.isFixedPitch) },
            { "UnderlinePosition", (info, lexer) => ReadDouble(lexer, ref info.UnderlinePosition) },
            { "UnderlineThickness", (info, lexer) => ReadDouble(lexer, ref info.UnderlineThickness) },
            { "FontName", (info, lexer) => ReadString(lexer, ref info.FontName) },
            { "PaintType", (info, lexer) => ReadInteger(lexer, ref info.PaintType) },
            { "WMode", (info, lexer) => ReadInteger(lexer, ref info.WMode) },
            { "FontBBox", (info, lexer) => ReadDoubleArray(lexer, ref info.FontBBox) },
            { "FontType", (info, lexer) => ReadInteger(lexer, ref info.FontType) },
            { "FontMatrix", (info, lexer) => ReadDoubleArray(lexer, ref info.FontMatrix) },
            { "Encoding", (info, lexer) => ReadEncoding(lexer, ref info.Encoding) },

            { "BlueValues", (info, lexer) => ReadDoubleArray(lexer, ref info.BlueValues) },
            { "BlueScale", (info, lexer) => ReadDouble(lexer, ref info.BlueScale) },
            { "StdHW", (info, lexer) => ReadDoubleArray(lexer, ref info.StdHW) },
            { "StdVW", (info, lexer) => ReadDoubleArray(lexer, ref info.StdVW) },
            { "StemSnapH", (info, lexer) => ReadDoubleArray(lexer, ref info.StemSnapH) },
        };

        private static readonly LexerKeywordTable keywords = new Type1ParserKeywordTable();

        public Type1Parser(byte[] data)
        {
            lexer = new Lexer(data, keywords);
        }

        private static void ReadEncoding(Lexer lexer, ref SingleByteEncoding? result)
        {
            var startLexeme = lexer.Read();

            if (startLexeme.Token == Token.Keyword)
            {
                if (startLexeme.KeywordValue == "StandardEncoding")
                {
                    result = SingleByteEncoding.Standard;
                    return;
                }
                else
                {
                    throw new Type1Exception("Unexpected type 1 encoding '" + startLexeme.KeywordValue + "'.");
                }
            }

            if (startLexeme.Token != Token.Integer)
            {
                throw new Type1Exception("Unexpected type 1 encoding token '" + startLexeme + "'.");
            }

            var glyphNames = new string?[256];

            var lexeme = lexer.Read();

            while (lexeme.Token != Token.Def && lexeme.Token != Token.EndOfInput)
            {
                if (lexeme.Token == Token.Dup)
                {
                    var index = (int)lexer.Read(Token.Integer).NumericValue;
                    var glyphName = lexer.Read(Token.Name).NameValue.Value;

                    if (index >= 0 && index < glyphNames.Length)
                    {
                        glyphNames[index] = glyphName;
                    }
                }

                lexeme = lexer.Read();
            }

            result = new Type1Encoding(glyphNames);
        }

        private static void ReadString(Lexer lexer, ref string? result)
        {
            var lexeme = lexer.Read();

            if (lexeme.Token == Token.LiteralString)
            {
                result = lexeme.StringValue.ToString();
                return;
            }

            if (lexeme.Token == Token.Name)
            {
                result = lexeme.NameValue.Value;
                return;
            }
        }

        private static Dictionary<string, byte[]>? ReadCharStrings(Lexer lexer)
        {
            var result = new Dictionary<string, byte[]>(StringComparer.Ordinal);

            lexer.Read(Token.Integer); // Count
            lexer.Read(Token.Dict);
            lexer.Read(Token.Dup);
            lexer.Read(Token.Begin);

            while (lexer.TryRead(Token.Name, out var nameLexeme))
            {
                var name = nameLexeme.NameValue.Value;

                var length = lexer.Read(Token.Integer);
                lexer.Read(Token.RD);

                lexer.Stream.Skip(1);

                const int MaxLength = 128 << 10;

                if (length.NumericValue < 0 ||
                    length.NumericValue > MaxLength)
                {
                    break;
                }

                var content = new byte[(int)length.NumericValue];
                lexer.Stream.ReadAll(content, 0, content.Length);
                Type1Decryptor.DecryptCharString(content, 0, content.Length);

                result[name] = content;

                lexer.Read(Token.ND);
            }

            return result;
        }

        private static byte[][] ReadSubrs(Lexer lexer)
        {
            const int MaxCount = 10000;
            const int SubrMaxLength = 128 << 10;

            var count = lexer.Read(Token.Integer);
            if (count.NumericValue < 0 || count.NumericValue > MaxCount)
            {
                throw new Type1Exception("Invalid subrs count " + count.NumericValue);
            }

            var result = new byte[(int)count.NumericValue][];

            lexer.Read(Token.Array);

            while (lexer.TryRead(Token.Dup))
            {
                var index = lexer.Read(Token.Integer).NumericValue;
                var length = lexer.Read(Token.Integer).NumericValue;

                if (index < 0 || index >= result.Length)
                {
                    throw new Type1Exception("Invalid subr index " + index);
                }
                if (length < 0 || length > SubrMaxLength)
                {
                    throw new Type1Exception("Invalid subr length " + length);
                }

                lexer.Read(Token.RD);

                lexer.Stream.Skip(1);

                var content = new byte[(int)length];
                lexer.Stream.ReadAll(content, 0, content.Length);
                Type1Decryptor.DecryptCharString(content, 0, content.Length);

                result[(int)index] = content;

                lexer.Read(Token.NP);
            }

            for (var i = 0; i < result.Length; i++)
            {
                if (result[i] == null)
                {
                    result[i] = ArrayUtils.Empty<byte>();
                }
            }

            return result;
        }

        private static void ReadDoubleArray(Lexer lexer, ref double[]? result)
        {
            var startLexeme = lexer.Peek();

            if (startLexeme.Token == Token.BeginBlock ||
                startLexeme.Token == Token.BeginArray)
            {
                var endToken = startLexeme.Token == Token.BeginBlock
                    ? Token.EndBlock
                    : Token.EndArray;

                lexer.Read();

                var resultList = new List<double>();
                var nextLexeme = lexer.Read();

                while (
                    nextLexeme.Token != endToken &&
                    nextLexeme.Token != Token.EndOfInput)
                {
                    switch (nextLexeme.Token)
                    {
                        case Token.Integer:
                        case Token.Real:
                            resultList.Add(nextLexeme.NumericValue);
                            break;
                    }

                    nextLexeme = lexer.Read();
                }

                result = resultList.ToArray();
            }
        }

        private static void ReadInteger(Lexer lexer, ref int result)
        {
            var lexeme = lexer.Peek();

            switch (lexeme.Token)
            {
                case Token.Integer:
                case Token.Real:
                    lexer.Read();
                    result = (int)lexeme.NumericValue;
                    break;
            }
        }

        private static void ReadDouble(Lexer lexer, ref double result)
        {
            var lexeme = lexer.Peek();

            switch (lexeme.Token)
            {
                case Token.Integer:
                case Token.Real:
                    lexer.Read();
                    result = lexeme.NumericValue;
                    break;
            }
        }

        private static void ReadBoolean(Lexer lexer, ref bool result)
        {
            var lexeme = lexer.Peek();

            switch (lexeme.Token)
            {
                case Token.Integer:
                case Token.Real:
                    lexer.Read();
                    result = lexeme.NumericValue != 0;
                    break;

                case Token.True:
                    lexer.Read();
                    result = true;
                    break;

                case Token.False:
                    lexer.Read();
                    result = false;
                    break;
            }
        }

        private static byte[] Decrypt(byte[] data, int length1, int length2)
        {
            const int PfbSegmentHeaderLength = 6;
            const int IvLength = 4;
            var header = Encoding.ASCII.GetBytes("%!");

            if (length1 < 0 || length1 >= data.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length1));
            }
            if (length2 < 0 || length1 + length2 > data.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length2));
            }

            var headerStart = data.IndexOf(header, 0, length1);
            if (headerStart < 0)
            {
                throw new Type1Exception("Missing header in Type 1 font.");
            }

            var resultLength = length1 - headerStart + length2;

            var hasPfbSegmentHeaders = data[0] == 128;
            if (hasPfbSegmentHeaders)
            {
                resultLength -= PfbSegmentHeaderLength;
            }

            var result = new byte[resultLength];
            var eexecResultStart = length1 - headerStart;

            Buffer.BlockCopy(data, headerStart, result, 0, eexecResultStart);


            var eexecStart = length1;
            if (hasPfbSegmentHeaders)
            {
                eexecStart += PfbSegmentHeaderLength;
            }

            Buffer.BlockCopy(data, eexecStart, result, eexecResultStart, resultLength - eexecResultStart);

            // According to the PDF spec, ASCII encoded Type 1 fonts are not allowed. However, Pdfium, Adobe and Pdf.js
            // all supports ASCII fonts.
            var eexecLength = Type1Decryptor.DecodeAscii(result, eexecResultStart, resultLength - eexecResultStart);
            Type1Decryptor.DecryptEexec(result, eexecResultStart, eexecLength);

            // Clear IV
            for (var i = 0; i < IvLength; i++)
            {
                result[eexecResultStart + i] = (byte)' ';
            }

            return result;
        }

        public static Type1FontInfo Parse(byte[] data, int length1, int length2)
        {
            var decrypted = Decrypt(data, length1, length2);
            var parser = new Type1Parser(decrypted);
            return parser.Read();
        }

        public Type1FontInfo Read()
        {
            var info = new Type1FontInfo();

            Dictionary<string, byte[]>? charStrings = null;
            var parsedCharStrings = new Dictionary<string, CharString>();

            var rawSubrs = ArrayUtils.Empty<byte[]>();
            var subrs = ArrayUtils.Empty<CharStringSubRoutine>();

            // Extract font information
            // This is a dirty hack that will search for anything that looks like entries of the font and private
            // dictionaries, without having to implement a full PostScript interpreter.
            do
            {
                var nextLexeme = lexer.Read();

                if (nextLexeme.Token == Token.Name)
                {
                    var name = nextLexeme.NameValue.Value;

                    if (name == "Subrs")
                    {
                        rawSubrs = ReadSubrs(lexer);
                    }
                    else if (name == "CharStrings")
                    {
                        charStrings = ReadCharStrings(lexer);
                    }
                    else if (readers.TryGetValue(name, out var reader))
                    {
                        reader(info, lexer);
                    }
                }
                else if (nextLexeme.Token == Token.EndOfInput)
                {
                    break;
                }
            }
            while (true);

            // Wrap subroutines
            subrs = new CharStringSubRoutine[rawSubrs.Length];

            for (var i = 0; i < rawSubrs.Length; i++)
            {
                var content = rawSubrs[i];
                var subrContent = new ArraySegment<byte>(content, info.lenIV, content.Length - info.lenIV);
                subrs[i] = new CharStringSubRoutine(subrContent);
            }

            // Parse char strings
            if (charStrings != null)
            {
                foreach (var ch in charStrings)
                {
                    var content = new ArraySegment<byte>(ch.Value, info.lenIV, ch.Value.Length - info.lenIV);

                    var parsed = CharStringParser.Parse(
                        CharStringType.Type1, content,
                        ArrayUtils.Empty<CharStringSubRoutine>(), subrs);

                    parsedCharStrings[ch.Key] = parsed;
                }
            }

            info.CharStrings = parsedCharStrings;
            info.Subrs = subrs;

            return info;
        }
    }
}
