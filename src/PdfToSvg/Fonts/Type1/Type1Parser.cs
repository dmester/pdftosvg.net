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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace PdfToSvg.Fonts.Type1
{
    internal class Type1Parser
    {
        private readonly Lexer lexer;

        private static readonly Dictionary<string, Action<Type1FontInfo, Lexer>> readers = new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Token> keywords = new(StringComparer.OrdinalIgnoreCase)
        {
            { "def", Token.Def },
            { "dup", Token.Dup },
            { "array", Token.Array },
            { "begin", Token.Begin },
            { "dict", Token.Dict },
            { "RD", Token.RD },
            { "ND", Token.ND },
            { "NP", Token.NP },
        };

        static Type1Parser()
        {
            var fields = typeof(Type1FontInfo)
                .GetTypeInfo()
                .GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(string))
                {
                    readers[field.Name] = (info, lexer) =>
                    {
                        var value = ReadString(lexer);
                        if (value != null)
                        {
                            field.SetValue(info, value);
                        }
                    };
                }
                else if (field.FieldType == typeof(double[]))
                {
                    readers[field.Name] = (info, lexer) => field.SetValue(info, ReadDoubleArray(lexer));
                }
                else if (field.FieldType == typeof(double))
                {
                    readers[field.Name] = (info, lexer) => field.SetValue(info, ReadDouble(lexer));
                }
                else if (field.FieldType == typeof(int))
                {
                    readers[field.Name] = (info, lexer) => field.SetValue(info, ReadInteger(lexer));
                }
                else if (field.FieldType == typeof(SingleByteEncoding))
                {
                    readers[field.Name] = (info, lexer) => field.SetValue(info, ReadEncoding(lexer));
                }
            }
        }

        public Type1Parser(byte[] data)
        {
            lexer = new Lexer(data, keywords);
        }

        private static SingleByteEncoding? ReadEncoding(Lexer lexer)
        {
            var startLexeme = lexer.Read();

            if (startLexeme.Token == Token.Keyword)
            {
                if (startLexeme.Value.ToString() == "StandardEncoding")
                {
                    return SingleByteEncoding.Standard;
                }
                else
                {
                    throw new Exception("Unexpected type 1 encoding '" + startLexeme.Value + "'.");
                }
            }

            if (startLexeme.Token != Token.Integer)
            {
                throw new Exception("Unexpected type 1 encoding token '" + startLexeme + "'.");
            }

            var glyphNames = new string?[256];

            var lexeme = lexer.Read();

            while (lexeme.Token != Token.Def && lexeme.Token != Token.EndOfInput)
            {
                if (lexeme.Token == Token.Dup)
                {
                    var index = lexer.Read(Token.Integer).IntValue;
                    var glyphName = lexer.Read(Token.Name).Value.ToString();

                    if (index >= 0 && index < glyphNames.Length)
                    {
                        glyphNames[index] = glyphName;
                    }
                }

                lexeme = lexer.Read();
            }

            return new Type1Encoding(glyphNames);
        }

        private static string? ReadString(Lexer lexer)
        {
            var lexeme = lexer.Read();

            if (lexeme.Token == Token.LiteralString)
            {
                return lexeme.Value.ToString();
            }

            if (lexeme.Token == Token.Name)
            {
                return lexeme.Value.ToString();
            }

            return null;
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
                var name = nameLexeme.Value.ToString();

                var length = lexer.Read(Token.Integer);
                lexer.Read(Token.RD);

                lexer.Stream.Skip(1);

                var content = new byte[length.IntValue];
                lexer.Stream.ReadAll(content, 0, content.Length);
                Type1Decryptor.DecryptCharString(content, 0, content.Length);

                result[name] = content;

                lexer.Read(Token.ND);
            }

            return result;
        }

        private static byte[][] ReadSubrs(Lexer lexer)
        {
            var count = lexer.Read(Token.Integer);
            var result = new byte[count.IntValue][];

            lexer.Read(Token.Array);

            while (lexer.TryRead(Token.Dup))
            {
                var index = lexer.Read(Token.Integer);
                var length = lexer.Read(Token.Integer);

                lexer.Read(Token.RD);

                lexer.Stream.Skip(1);

                var content = new byte[length.IntValue];
                lexer.Stream.ReadAll(content, 0, content.Length);
                Type1Decryptor.DecryptCharString(content, 0, content.Length);

                result[index.IntValue] = content;

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

        private static double[]? ReadDoubleArray(Lexer lexer)
        {
            var startLexeme = lexer.Peek();

            if (startLexeme.Token == Token.BeginBlock ||
                startLexeme.Token == Token.BeginArray)
            {
                var endToken = startLexeme.Token == Token.BeginBlock
                    ? Token.EndBlock
                    : Token.EndArray;

                lexer.Read();

                var result = new List<double>();
                var nextLexeme = lexer.Read();

                while (
                    nextLexeme.Token != endToken &&
                    nextLexeme.Token != Token.EndOfInput)
                {
                    switch (nextLexeme.Token)
                    {
                        case Token.Integer:
                            result.Add(nextLexeme.IntValue);
                            break;

                        case Token.Real:
                            var dblValue = double.Parse(nextLexeme.Value.ToString(), CultureInfo.InvariantCulture);
                            result.Add(dblValue);
                            break;
                    }

                    nextLexeme = lexer.Read();
                }

                return result.ToArray();
            }

            return null;
        }

        private static int ReadInteger(Lexer lexer)
        {
            return lexer.TryRead(Token.Integer, out var lexeme) ? lexeme.IntValue : 0;
        }

        private static double? ReadDouble(Lexer lexer)
        {
            var lexeme = lexer.Peek();

            switch (lexeme.Token)
            {
                case Token.Integer:
                    lexer.Read();
                    return lexeme.IntValue;

                case Token.Real:
                    lexer.Read();
                    return double.Parse(lexeme.Value.ToString(), CultureInfo.InvariantCulture);

                default:
                    return null;
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
                throw new Exception("Missing header in Type 1 font.");
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
                    var name = nextLexeme.Value.ToString();

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
