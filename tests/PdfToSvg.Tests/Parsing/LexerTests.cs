// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg;
using PdfToSvg.DocumentModel;
using PdfToSvg.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Parsing
{
    public class LexerTests
    {
        Dictionary<string, Token> basicKeywords = new Dictionary<string, Token>(StringComparer.OrdinalIgnoreCase)
        {
            { "stream", Token.Stream },
            { "true", Token.True },
            { "false", Token.False },
            { "null", Token.Null },
            { "R", Token.Ref },
        };

        [Test]
        public void Stream()
        {
            var lexerLF = new Lexer("  stream\nhello", basicKeywords);
            Assert.AreEqual(new Lexeme(Token.Stream), lexerLF.Read());
            Assert.AreEqual(9, lexerLF.Stream.Position);

            var lexerCRLF = new Lexer("   stream\r\nhello", basicKeywords);
            Assert.AreEqual(new Lexeme(Token.Stream), lexerCRLF.Read());
            Assert.AreEqual(11, lexerCRLF.Stream.Position);
        }

        [Test]
        public void Commands()
        {
            var lexer = new Lexer("ET\nT* 0 0 1 rg /Ti Tj 12 (T*)", basicKeywords);

            Assert.AreEqual(new Lexeme(Token.Keyword, "ET"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Keyword, "T*"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Integer, "0"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Integer, "0"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Integer, "1"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Keyword, "rg"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Name, "Ti"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Keyword, "Tj"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Integer, "12"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.LiteralString, "T*"), lexer.Read());

            Assert.AreEqual(new Lexeme(Token.EndOfInput), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.EndOfInput), lexer.Read());
        }

        [Test]
        public void Dictionary()
        {
            var lexer = new Lexer("  <</Type/Font/Width 5/NestedEmpty<<>>>> ", basicKeywords);

            Assert.AreEqual(new Lexeme(Token.BeginDictionary), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Name, "Type"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Name, "Font"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Name, "Width"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Integer, "5"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Name, "NestedEmpty"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.BeginDictionary), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.EndDictionary), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.EndDictionary), lexer.Read());

            Assert.AreEqual(new Lexeme(Token.EndOfInput), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.EndOfInput), lexer.Read());
        }

        [Test]
        public void Array()
        {
            var lexer = new Lexer(" [ 1R/Name[ (R]R)]] ", basicKeywords);

            Assert.AreEqual(new Lexeme(Token.BeginArray), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Integer, "1"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Ref), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Name, "Name"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.BeginArray), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.LiteralString, "R]R"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.EndArray), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.EndArray), lexer.Read());

            Assert.AreEqual(new Lexeme(Token.EndOfInput), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.EndOfInput), lexer.Read());
        }

        [Test]
        public void Boolean()
        {
            // Uppercase booleans are technically not legal, but we will be nice to malformed input
            var lexer = new Lexer(" true false True TRUE False FALSE ", basicKeywords);

            Assert.AreEqual(new Lexeme(Token.True), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.False), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.True), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.True), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.False), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.False), lexer.Read());

            Assert.AreEqual(new Lexeme(Token.EndOfInput), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.EndOfInput), lexer.Read());
        }

        [Test]
        public void Integer()
        {
            var lexer = new Lexer(" 0  +42-55 1234567890 -1234567890 ", basicKeywords);

            Assert.AreEqual(new Lexeme(Token.Integer, "0"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Integer, "+42"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Integer, "-55"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Integer, "1234567890"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Integer, "-1234567890"), lexer.Read());

            Assert.AreEqual(new Lexeme(Token.EndOfInput), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.EndOfInput), lexer.Read());
        }

        [Test]
        public void Real()
        {
            var lexer = new Lexer(" 0.0  .12345 42.0+.4567 -.4567 +23.4567 -23.4567 4. ", basicKeywords);

            Assert.AreEqual(new Lexeme(Token.Real, "0.0"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Real, ".12345"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Real, "42.0"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Real, "+.4567"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Real, "-.4567"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Real, "+23.4567"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Real, "-23.4567"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Real, "4."), lexer.Read());

            Assert.AreEqual(new Lexeme(Token.EndOfInput), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.EndOfInput), lexer.Read());
        }

        [Test]
        public void LiteralString()
        {
            var lexer = new Lexer(
                "(abc) " +
                "(string()with parentheses)" +
                "()" +
                "(\"#¤%&/[]<>)" +
                @"(esc\\\n\r\t\)\(\W)" +
                "(no\\\n line \\\r\nbreaks)" +
                "(normalized\r\nline\rbreaks\n)" +
                @"(octal \53\053 \53a \0053)", basicKeywords);

            Assert.AreEqual(new Lexeme(Token.LiteralString, "abc"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.LiteralString, "string()with parentheses"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.LiteralString, ""), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.LiteralString, "\"#¤%&/[]<>"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.LiteralString, "esc\\\n\r\t)(W"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.LiteralString, "no line breaks"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.LiteralString, "normalized\nline\nbreaks\n"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.LiteralString, "octal ++ +a \x00053"), lexer.Read());

            Assert.AreEqual(new Lexeme(Token.EndOfInput), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.EndOfInput), lexer.Read());

            var lexerInvalid = new Lexer("(abc");
            Assert.Throws<PdfParserException>(() => lexerInvalid.Read());
        }

        [Test]
        public void HexString()
        {
            var lexer = new Lexer(
                "<>" +
                "<414a4A50>" +
                "< 4 14  \na  \t \f 4A 5  >", basicKeywords);

            Assert.AreEqual(new Lexeme(Token.HexString, 0, ""), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.HexString, 2, "AJJP"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.HexString, 12, "AJJP"), lexer.Read());

            Assert.AreEqual(new Lexeme(Token.EndOfInput, 35), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.EndOfInput, 35), lexer.Read());

            var lexerInvalidChars = new Lexer("< 4XXX 14 ### \0 \na  \t \f 4A 5  >");
            Assert.Throws<PdfParserException>(() => lexerInvalidChars.Read());

            // TODO check how other handles unclosed strings
            var lexerUnclosed = new Lexer("<414a4A50");
            Assert.Throws<PdfParserException>(() => lexerUnclosed.Read());
        }

        [Test]
        public void Name()
        {
            // Examples from Table 4 in PDF spec 1.7
            var lexer = new Lexer(
                "/Name1" +
                "/ASomewhatLongerName " +
                "/A;Name_With-Various***Characters?\n" +
                "/1.2" +
                "/$$ " +
                "/@pattern  " +
                "/.notdef " +
                "/lime#20green " +
                "/paired#28#29parentheses " +
                "/The_Key_of_F#23_Minor " +
                "/A#42", basicKeywords);

            Assert.AreEqual(new Lexeme(Token.Name, "Name1"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Name, "ASomewhatLongerName"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Name, "A;Name_With-Various***Characters?"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Name, "1.2"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Name, "$$"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Name, "@pattern"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Name, ".notdef"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Name, "lime green"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Name, "paired()parentheses"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Name, "The_Key_of_F#_Minor"), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.Name, "AB"), lexer.Read());

            Assert.AreEqual(new Lexeme(Token.EndOfInput), lexer.Read());
            Assert.AreEqual(new Lexeme(Token.EndOfInput), lexer.Read());
        }
    }
}
