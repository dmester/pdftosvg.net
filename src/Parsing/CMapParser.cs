// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Parsing
{
    internal class CMapParser : Parser
    {
        private static readonly Dictionary<string, Token> keywords = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase)
        {
            { "begincodespacerange", Token.BeginCodeSpaceRange },
            { "endcodespacerange", Token.EndCodeSpaceRange },
            { "beginbfchar", Token.BeginBfChar },
            { "endbfchar", Token.EndBfChar },
            { "beginbfrange", Token.BeginBfRange },
            { "endbfrange", Token.EndBfRange },
            { "beginnotdefchar", Token.BeginNotDefChar },
            { "endnotdefchar", Token.EndNotDefChar },
            { "beginnotdefrange", Token.BeginNotDefRange },
            { "endnotdefrange", Token.EndNotDefRange },
        };

        private CMapParser(Stream stream) : base(new Lexer(stream, keywords))
        {
        }

        public static CMap Parse(PdfStream stream)
        {
            using (var decodedStream = stream.OpenDecoded())
            {
                var parser = new CMapParser(decodedStream);
                return parser.ReadCMap();
            }
        }

        public CMap ReadCMap()
        {
            var cmap = new CMap();
            Lexeme lexeme;

            do
            {
                lexeme = lexer.Read();

                switch (lexeme.Token)
                {
                    case Token.BeginBfChar:
                        ReadBfChar(cmap);
                        break;

                    case Token.BeginBfRange:
                        ReadBfRange(cmap);
                        break;
                }
            }
            while (lexeme.Token != Token.EndOfInput);

            return cmap;
        }

        private void ReadBfChar(CMap cmap)
        {
            while (true)
            {
                var srcLexeme = lexer.Read();
                if (srcLexeme.Token == Token.EndBfChar)
                {
                    break;
                }

                if (srcLexeme.Token != Token.HexString)
                {
                    throw Exceptions.UnexpectedToken(lexer.Stream, srcLexeme);
                }

                var dstLexeme = lexer.Read();
                if (dstLexeme.Token != Token.HexString)
                {
                    throw Exceptions.UnexpectedToken(lexer.Stream, srcLexeme);
                }

                cmap.AddBfChar(srcLexeme.Value, dstLexeme.Value);
            }
        }

        private void ReadBfRange(CMap cmap)
        {
            while (true)
            {
                var srcLexemeLo = lexer.Read();
                if (srcLexemeLo.Token == Token.EndBfRange)
                {
                    break;
                }

                if (srcLexemeLo.Token != Token.HexString)
                {
                    throw Exceptions.UnexpectedToken(lexer.Stream, srcLexemeLo);
                }

                var srcLexemeHi = lexer.Read();
                if (srcLexemeHi.Token != Token.HexString)
                {
                    throw Exceptions.UnexpectedToken(lexer.Stream, srcLexemeHi);
                }

                var nextLexeme = lexer.Read();
                if (nextLexeme.Token == Token.BeginArray)
                {
                    var dstStrings = new List<PdfString>();

                    while (true)
                    {
                        nextLexeme = lexer.Read();

                        if (nextLexeme.Token == Token.HexString)
                        {
                            dstStrings.Add(nextLexeme.Value);
                        }
                        else if (nextLexeme.Token == Token.EndArray)
                        {
                            break;
                        }
                        else
                        {
                            throw Exceptions.UnexpectedToken(lexer.Stream, nextLexeme);
                        }
                    }

                    cmap.AddBfRange(srcLexemeLo.Value, srcLexemeHi.Value, dstStrings);
                }
                else if (nextLexeme.Token == Token.HexString)
                {
                    cmap.AddBfRange(srcLexemeLo.Value, srcLexemeHi.Value, nextLexeme.Value);
                }
                else
                {
                    throw Exceptions.UnexpectedToken(lexer.Stream, nextLexeme);
                }
            }
        }
    }
}
