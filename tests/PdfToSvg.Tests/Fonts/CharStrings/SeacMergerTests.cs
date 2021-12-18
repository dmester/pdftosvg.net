// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Fonts.CharStrings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Fonts.CharStrings
{
    internal class SeacMergerTests
    {
        [Test]
        public void Merge()
        {
            var acharInfo = new CharStringInfo();

            acharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operand(1));
            acharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operand(2));
            acharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operator(CharStringOpCode.HStemHm));
            acharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operator(CharStringOpCode.HintMask));
            acharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Mask(0));

            acharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operand(20));
            acharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operand(40));
            acharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operator(CharStringOpCode.RMoveTo));

            acharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operand(10));
            acharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operand(15));
            acharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operator(CharStringOpCode.RLineTo));

            var bcharInfo = new CharStringInfo();

            bcharInfo.Path.RMoveTo(74, 75);

            bcharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operand(1));
            bcharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operand(2));
            bcharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operand(3));
            bcharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operand(4));
            bcharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operator(CharStringOpCode.HStemHm));
            bcharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operator(CharStringOpCode.HintMask));
            bcharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Mask(0));

            bcharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operand(30));
            bcharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operand(50));
            bcharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operator(CharStringOpCode.RMoveTo));

            bcharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operand(20));
            bcharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operand(25));
            bcharInfo.ContentInlinedSubrs.Add(CharStringLexeme.Operator(CharStringOpCode.RLineTo));

            var merged = SeacMerger.Merge(new CharString(acharInfo), new CharString(bcharInfo), 10, 20);

            var expected = new List<CharStringLexeme>
            {
                CharStringLexeme.Operand(1),
                CharStringLexeme.Operand(2),
                CharStringLexeme.Operand(3),
                CharStringLexeme.Operand(4),
                CharStringLexeme.Operator(CharStringOpCode.HStemHm),
                CharStringLexeme.Operator(CharStringOpCode.HintMask),
                CharStringLexeme.Mask(0),

                CharStringLexeme.Operand(30),
                CharStringLexeme.Operand(50),
                CharStringLexeme.Operator(CharStringOpCode.RMoveTo),

                CharStringLexeme.Operand(20),
                CharStringLexeme.Operand(25),
                CharStringLexeme.Operator(CharStringOpCode.RLineTo),

                CharStringLexeme.Operand(-64),
                CharStringLexeme.Operand(-55),
                CharStringLexeme.Operator(CharStringOpCode.RMoveTo),

                CharStringLexeme.Operand(20),
                CharStringLexeme.Operand(40),
                CharStringLexeme.Operator(CharStringOpCode.RMoveTo),

                CharStringLexeme.Operand(10),
                CharStringLexeme.Operand(15),
                CharStringLexeme.Operator(CharStringOpCode.RLineTo),
            };

            Assert.AreEqual(expected, merged);
        }
    }
}
