// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

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

        private readonly ResourceCache? resources;

        private ContentParser(Stream stream, ResourceCache? resources = null) : base(new Lexer(stream, keywords))
        {
            this.resources = resources;
        }

        private ContentParser(ArraySegment<byte> data, ResourceCache? resources = null) : base(new Lexer(data, keywords))
        {
            this.resources = resources;
        }

        private ContentParser(byte[] data, ResourceCache? resources = null) : base(new Lexer(data, keywords))
        {
            this.resources = resources;
        }

        public static IEnumerable<ContentOperation> Parse(Stream stream,
            ResourceCache? resources = null, CancellationToken cancellationToken = default)
        {
            var parser = new ContentParser(stream, resources);
            return parser.ReadContentStream(cancellationToken);
        }

        public static IEnumerable<ContentOperation> Parse(ArraySegment<byte> data,
            ResourceCache? resources = null, CancellationToken cancellationToken = default)
        {
            var parser = new ContentParser(data, resources);
            return parser.ReadContentStream(cancellationToken);
        }

        public static IEnumerable<ContentOperation> Parse(byte[] data,
            ResourceCache? resources = null, CancellationToken cancellationToken = default)
        {
            var parser = new ContentParser(data, resources);
            return parser.ReadContentStream(cancellationToken);
        }

        private PdfDictionary ReadInlineImageDictionary(CancellationToken cancellationToken)
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
                    name = nextLexeme.NameValue;
                }
                else
                {
                    throw new ParserException("Unclosed inline image dictionary", lexer.Stream.Position);
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
                var bytesToRead = InlineImageHelper
                    .DetectStreamLength(lexer.Stream, dictionary, resources, cancellationToken);

                // Read stream data
                var imageData = new byte[bytesToRead];
                lexer.Stream.Position = startPosition;

                var bytesActuallyRead = lexer.Stream.ReadAll(imageData, 0, bytesToRead, cancellationToken);

                dictionary.MakeIndirectObject(default, new PdfMemoryStream(dictionary, imageData, bytesActuallyRead));

                lexer.TryRead(Token.EndImage);
            }
            else if (nextLexeme.Token == Token.EndImage)
            {
                // Missing stream
            }

            return dictionary;
        }

        private IEnumerable<ContentOperation> ReadContentStream(CancellationToken cancellationToken)
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
                        var imageDictionary = ReadInlineImageDictionary(cancellationToken);

                        yield return new ContentOperation("BI", imageDictionary);
                        break;

                    /*
                    case Token.BeginImageData:
                        // This is not ignored, as image data will likely mess a lot with the lexer and parser
                    */

                    case Token.EndImage:
                        // Unexpected at this position, but ignore it
                        lexer.Read();
                        operands.Clear();
                        break;

                    case Token.Keyword:
                        lexer.Read();

                        yield return new ContentOperation(nextLexeme.KeywordValue, operands.ToArray());

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
