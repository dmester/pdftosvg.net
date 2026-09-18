// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;

namespace PdfToSvg.Parsing
{
    // General PDF keywords
    [LexerKeyword("obj", Token.Obj)]
    [LexerKeyword("endobj", Token.EndObj)]
    [LexerKeyword("null", Token.Null)]
    [LexerKeyword("true", Token.True)]
    [LexerKeyword("false", Token.False)]
    [LexerKeyword("R", Token.Ref)]

    // Inline images
    [LexerKeyword("BI", Token.BeginImage)]
    [LexerKeyword("ID", Token.BeginImageData)]
    [LexerKeyword("EI", Token.EndImage)]

    // These are lexed as a keyword followed by a number if not getting special treatment
    [LexerKeyword("d0", Token.Keyword)]
    [LexerKeyword("d1", Token.Keyword)]

    // The following are included to reuse their static keyword string representations
    [LexerKeyword("b", Token.Keyword)]
    [LexerKeyword("BDC", Token.Keyword)]
    [LexerKeyword("BT", Token.Keyword)]
    [LexerKeyword("c", Token.Keyword)]
    [LexerKeyword("cm", Token.Keyword)]
    [LexerKeyword("CS", Token.Keyword)]
    [LexerKeyword("EMC", Token.Keyword)]
    [LexerKeyword("ET", Token.Keyword)]
    [LexerKeyword("f", Token.Keyword)]
    [LexerKeyword("gs", Token.Keyword)]
    [LexerKeyword("h", Token.Keyword)]
    [LexerKeyword("J", Token.Keyword)]
    [LexerKeyword("j", Token.Keyword)]
    [LexerKeyword("l", Token.Keyword)]
    [LexerKeyword("M", Token.Keyword)]
    [LexerKeyword("m", Token.Keyword)]
    [LexerKeyword("n", Token.Keyword)]
    [LexerKeyword("Q", Token.Keyword)]
    [LexerKeyword("q", Token.Keyword)]
    [LexerKeyword("re", Token.Keyword)]
    [LexerKeyword("RG", Token.Keyword)]
    [LexerKeyword("rg", Token.Keyword)]
    [LexerKeyword("S", Token.Keyword)]
    [LexerKeyword("Tc", Token.Keyword)]
    [LexerKeyword("Td", Token.Keyword)]
    [LexerKeyword("Tf", Token.Keyword)]
    [LexerKeyword("TJ", Token.Keyword)]
    [LexerKeyword("Tj", Token.Keyword)]
    [LexerKeyword("Tm", Token.Keyword)]
    [LexerKeyword("Tz", Token.Keyword)]
    [LexerKeyword("W", Token.Keyword)]
    [LexerKeyword("w", Token.Keyword)]
    internal sealed partial class ContentParserKeywordTable : LexerKeywordTable
    {
    }

    internal partial class ContentParser : Parser
    {
        private static readonly LexerKeywordTable keywords = new ContentParserKeywordTable();

        public ContentParser(Stream stream) : base(new Lexer(stream, keywords))
        {
        }

        public ContentParser(ArraySegment<byte> data) : base(new Lexer(data, keywords))
        {
        }

        public ContentParser(byte[] data) : base(new Lexer(data, keywords))
        {
        }

        public static IEnumerable<ContentOperation> Parse(Stream stream)
        {
            var parser = new ContentParser(stream);
            return parser.ReadContentStream();
        }

        public static IEnumerable<ContentOperation> Parse(ArraySegment<byte> data)
        {
            var parser = new ContentParser(data);
            return parser.ReadContentStream();
        }

        public static IEnumerable<ContentOperation> Parse(byte[] data)
        {
            var parser = new ContentParser(data);
            return parser.ReadContentStream();
        }

        private IEnumerable<ContentOperation> ReadContentStream()
        {
            var operands = new List<object?>();

            while (true)
            {
                var nextLexeme = lexer.Peek();

                switch (nextLexeme.Token)
                {
                    case Token.EndOfInput:
                        yield break;

                    case Token.BeginImage:
                        var imageDictionary = ReadInlineImageDictionary();

                        yield return new ContentOperation("BI", imageDictionary);
                        break;

                    case Token.Keyword:
                        lexer.Read();

                        yield return new ContentOperation(nextLexeme.Value.ToString(), operands.ToArray());

                        operands.Clear();
                        break;

                    default:
                        operands.Add(ReadValue());
                        break;
                }
            }
        }
    }
}
