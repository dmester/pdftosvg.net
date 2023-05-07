// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Fonts.CharStrings;
using PdfToSvg.Fonts.CompactFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Fonts.CompactFonts
{
    internal class CompactFontBuilderTests
    {
        private CompactFontSet Charset(int[] sids)
        {
            var fontSet = new CompactFontSet();
            var font = new CompactFont(fontSet);

            for (var gid = 0; gid < sids.Length; gid++)
            {
                var sid = sids[gid];
                font.Glyphs.Add(new CompactFontGlyph(CharString.Empty, "x", gid, sid, null, 0));
            }

            fontSet.Fonts.Add(font);

            return fontSet;
        }

        private void CustomCharset(int format, int[] sids)
        {
            var fontSet = Charset(sids);

            var builtCff = CompactFontBuilder.Build(fontSet, inlineSubrs: false);
            var parsedCff = CompactFontParser.Parse(builtCff);

            var actualFormat = builtCff[parsedCff.Fonts[0].TopDict.Charset];

            Assert.AreEqual(sids, parsedCff.Fonts[0].CharSet.ToArray());
            Assert.AreEqual(format, actualFormat);
        }

        private void PredefinedCharset(int charsetId, int[] sids)
        {
            var fontSet = Charset(sids);

            var builtCff = CompactFontBuilder.Build(fontSet, inlineSubrs: false);
            var parsedCff = CompactFontParser.Parse(builtCff);

            Assert.AreEqual(sids, parsedCff.Fonts[0].CharSet.ToArray());
            Assert.AreEqual(charsetId, parsedCff.Fonts[0].TopDict.Charset);
        }

        [TestCase("", "")]
        [TestCase("abc", "abc")]
        [TestCase("a[](){}<>/%\0 \t\r\n\fa", "a________________a")]
        public void SanitizeName(string input, string expectedOutput)
        {
            var actualOutput = CompactFontBuilder.SanitizeName(input);
            Assert.AreEqual(expectedOutput, actualOutput);
        }

        [Test]
        public void Charset_Format0()
        {
            CustomCharset(0, new[] { 0, 43, 42, 36, 38, 51, 77, 59, 41, 99, 85 });
        }

        [Test]
        public void Charset_Format1()
        {
            CustomCharset(1, new int[] { 0, 33, 34, 35, 36, 37, 38, 39, 51, 52, 53, 54, 55, 56 });
        }

        [Test]
        public void Charset_Format2()
        {
            CustomCharset(2, Enumerable.Concat(new int[] { 0 }, Enumerable.Range(10, 400)).ToArray());
        }

        [Test]
        public void Charset_ISOAdobe()
        {
            PredefinedCharset(0, Enumerable.Range(0, 200).ToArray());
        }

        [Test]
        public void Charset_Expert()
        {
            PredefinedCharset(1, CompactFontPredefinedCharsets.Charsets[1]);
        }

        [Test]
        public void Charset_ExpertSubset()
        {
            PredefinedCharset(2, CompactFontPredefinedCharsets.Charsets[2]);
        }

        private CharStringSubRoutine Subr(int moveX, int moveY)
        {
            return new CharStringSubRoutine(new byte[]
            {
                (byte)(moveX + 139),
                (byte)(moveY + 139),
                21, // rmoveto
                11, // return
            })
            {
                Used = true,
            };
        }

        private CompactFontSet FontWithSubrs()
        {
            var fontSet = new CompactFontSet();
            var font = new CompactFont(fontSet);

            fontSet.Subrs.Add(Subr(1, 2));
            font.Subrs.Add(Subr(3, 4));

            var charStringInfo = new CharStringInfo();

            charStringInfo.Width = 400;

            charStringInfo.Content.Add(CharStringLexeme.Operand(-107));
            charStringInfo.Content.Add(CharStringLexeme.Operator(CharStringOpCode.CallGSubr));
            charStringInfo.Content.Add(CharStringLexeme.Operand(-107));
            charStringInfo.Content.Add(CharStringLexeme.Operator(CharStringOpCode.CallSubr));
            charStringInfo.Content.Add(CharStringLexeme.Operator(CharStringOpCode.EndChar));

            font.Glyphs.Add(new CompactFontGlyph(new CharString(charStringInfo), "x", 0, 0, null, 0));

            fontSet.Fonts.Add(font);

            var builtCff = CompactFontBuilder.Build(fontSet, inlineSubrs: false);
            var parsedCff = CompactFontParser.Parse(builtCff);

            return parsedCff;
        }

        [Test]
        public void CharString_KeepSubrs()
        {
            var fontSet = FontWithSubrs();

            var builtCff = CompactFontBuilder.Build(fontSet, inlineSubrs: false);
            var parsedCff = CompactFontParser.Parse(builtCff);

            var expected = new List<CharStringLexeme>
            {
                CharStringLexeme.Operand(-107),
                CharStringLexeme.Operator(CharStringOpCode.CallGSubr),
                CharStringLexeme.Operand(-107),
                CharStringLexeme.Operator(CharStringOpCode.CallSubr),
                CharStringLexeme.Operator(CharStringOpCode.EndChar),
            };

            Assert.AreEqual(expected, parsedCff.Fonts[0].Glyphs[0].CharString.Content);
            Assert.AreEqual(1, parsedCff.Subrs.Count);
            Assert.AreEqual(1, parsedCff.Fonts[0].Subrs.Count);
        }

        [Test]
        public void CharString_InlineSubrs()
        {
            var fontSet = FontWithSubrs();

            var builtCff = CompactFontBuilder.Build(fontSet, inlineSubrs: true);
            var parsedCff = CompactFontParser.Parse(builtCff);

            var expected = new List<CharStringLexeme>
            {
                CharStringLexeme.Operand(1),
                CharStringLexeme.Operand(2),
                CharStringLexeme.Operator(CharStringOpCode.RMoveTo),
                CharStringLexeme.Operand(3),
                CharStringLexeme.Operand(4),
                CharStringLexeme.Operator(CharStringOpCode.RMoveTo),
                CharStringLexeme.Operator(CharStringOpCode.EndChar),
            };

            Assert.AreEqual(expected, parsedCff.Fonts[0].Glyphs[0].CharString.Content);
            Assert.AreEqual(0, parsedCff.Subrs.Count);
            Assert.AreEqual(0, parsedCff.Fonts[0].Subrs.Count);
        }

        [Test]
        public void CidFont()
        {
            var fontSet = new CompactFontSet();
            var font = new CompactFont(fontSet);
            var subFont0 = new CompactSubFont();
            var subFont1 = new CompactSubFont();

            fontSet.Subrs.Add(Subr(1, 2));
            font.Subrs.Add(Subr(3, 4));

            font.TopDict.ROS = new double[] { 1 };

            subFont0.Subrs.Add(Subr(5, 6));
            subFont1.Subrs = font.Subrs;

            var charStringInfo = new CharStringInfo();
            charStringInfo.Width = 400;

            charStringInfo.Content.Add(CharStringLexeme.Operand(-107));
            charStringInfo.Content.Add(CharStringLexeme.Operator(CharStringOpCode.CallGSubr));
            charStringInfo.Content.Add(CharStringLexeme.Operand(-107));
            charStringInfo.Content.Add(CharStringLexeme.Operator(CharStringOpCode.CallSubr));
            charStringInfo.Content.Add(CharStringLexeme.Operator(CharStringOpCode.EndChar));

            font.Glyphs.Add(new CompactFontGlyph(new CharString(charStringInfo), "x", 0, 0, null, 0));
            font.Glyphs.Add(new CompactFontGlyph(new CharString(charStringInfo), "x", 1, 1, null, 0));
            font.Glyphs.Add(new CompactFontGlyph(new CharString(charStringInfo), "x", 2, 2, null, 0));

            font.FDArray.Add(subFont0);
            font.FDArray.Add(subFont1);
            fontSet.Fonts.Add(font);

            font.FDSelect.Add(0);
            font.FDSelect.Add(0);
            font.FDSelect.Add(1);

            var builtCff = CompactFontBuilder.Build(fontSet, inlineSubrs: false);
            var parsedCff = CompactFontParser.Parse(builtCff);
            builtCff = CompactFontBuilder.Build(CompactFontParser.Parse(builtCff), inlineSubrs: true);
            parsedCff = CompactFontParser.Parse(builtCff);

            var expected1 = new List<CharStringLexeme>
            {
                CharStringLexeme.Operand(1),
                CharStringLexeme.Operand(2),
                CharStringLexeme.Operator(CharStringOpCode.RMoveTo),
                CharStringLexeme.Operand(3),
                CharStringLexeme.Operand(4),
                CharStringLexeme.Operator(CharStringOpCode.RMoveTo),
                CharStringLexeme.Operator(CharStringOpCode.EndChar),
            };
            var expected2 = new List<CharStringLexeme>
            {
                CharStringLexeme.Operand(1),
                CharStringLexeme.Operand(2),
                CharStringLexeme.Operator(CharStringOpCode.RMoveTo),
                CharStringLexeme.Operand(5),
                CharStringLexeme.Operand(6),
                CharStringLexeme.Operator(CharStringOpCode.RMoveTo),
                CharStringLexeme.Operator(CharStringOpCode.EndChar),
            };

            Assert.AreEqual(0, parsedCff.Subrs.Count);
            Assert.AreEqual(0, parsedCff.Fonts[0].Subrs.Count);
            Assert.AreEqual(2, parsedCff.Fonts[0].FDArray.Count);
            Assert.AreEqual(0, parsedCff.Fonts[0].FDArray[0].Subrs.Count);
            Assert.AreEqual(0, parsedCff.Fonts[0].FDArray[1].Subrs.Count);

            Assert.AreEqual(expected2, parsedCff.Fonts[0].Glyphs[0].CharString.Content);
            Assert.AreEqual(expected2, parsedCff.Fonts[0].Glyphs[1].CharString.Content);
            Assert.AreEqual(expected1, parsedCff.Fonts[0].Glyphs[2].CharString.Content);
        }

    }
}
