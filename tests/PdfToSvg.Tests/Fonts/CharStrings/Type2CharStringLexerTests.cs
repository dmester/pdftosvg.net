// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Fonts.CharStrings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Fonts.CharStrings
{
    internal class Type2CharStringLexerTests
    {
        private static byte[] ParseSpec(string spec)
        {
            return spec
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => byte.Parse(x, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
                .ToArray();
        }

        private static Type2CharStringLexer CreateLexer(string spec)
        {
            var parsed = spec
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => byte.Parse(x, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
                .ToArray();

            return new Type2CharStringLexer(parsed, 0, parsed.Length);
        }

        [TestCase("1C 80 00", -32768d)]
        [TestCase("1C 7F FF", 32767d)]
        [TestCase("20", -107d)]
        [TestCase("F6", 107d)]
        [TestCase("F7 00", 108d)]
        [TestCase("FA FF", 1131d)]
        [TestCase("FB 00", -108d)]
        [TestCase("FE FF", -1131d)]
        [TestCase("FF 00 00 00 00", 0d)]
        [TestCase("FF 80 00 00 00", -32768d)]
        [TestCase("FF 7F FF 00 00", 32767d)]
        [TestCase("FF 00 00 80 00", 0.5d)]
        public void ReadOperand(string data, double expectedValue)
        {
            var bytes = ParseSpec(data);
            var lexer = new Type2CharStringLexer(bytes, 0, bytes.Length);

            var lexeme = lexer.Read();

            Assert.AreEqual(CharStringToken.Operand, lexeme.Token);
            Assert.AreEqual(expectedValue, lexeme.Value);

            Assert.AreEqual(bytes.Length, lexer.Position);
            Assert.AreEqual(CharStringToken.EndOfInput, lexer.Read().Token);
        }

        [TestCase("00", 0)]
        [TestCase("0B", 11)]
        [TestCase("0C 00", 12 << 8)]
        [TestCase("0C ff", (12 << 8) | 0xff)]
        [TestCase("15", 21)]
        [TestCase("1B", 27)]
        [TestCase("1D", 29)]
        [TestCase("1F", 31)]
        public void ReadOperator(string data, int expectedOperator)
        {
            var bytes = ParseSpec(data);
            var lexer = new Type2CharStringLexer(bytes, 0, bytes.Length);

            var lexeme = lexer.Read();

            Assert.AreEqual(CharStringToken.Operator, lexeme.Token);
            Assert.AreEqual(expectedOperator, (int)lexeme.Value);

            Assert.AreEqual(bytes.Length, lexer.Position);
            Assert.AreEqual(CharStringToken.EndOfInput, lexer.Read().Token);
        }
    }
}
