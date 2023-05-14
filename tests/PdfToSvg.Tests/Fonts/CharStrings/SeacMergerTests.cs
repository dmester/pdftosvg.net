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

            acharInfo.Hints.Add(CharStringLexeme.Operand(1));
            acharInfo.Hints.Add(CharStringLexeme.Operand(2));
            acharInfo.Hints.Add(CharStringLexeme.Operator(CharStringOpCode.HStemHm));
            acharInfo.Hints.Add(CharStringLexeme.Operator(CharStringOpCode.HintMask));
            acharInfo.Hints.Add(CharStringLexeme.Mask(0));

            acharInfo.Content.Add(CharStringLexeme.Operand(20));
            acharInfo.Content.Add(CharStringLexeme.Operand(40));
            acharInfo.Content.Add(CharStringLexeme.Operator(CharStringOpCode.RMoveTo));

            acharInfo.Content.Add(CharStringLexeme.Operand(10));
            acharInfo.Content.Add(CharStringLexeme.Operand(15));
            acharInfo.Content.Add(CharStringLexeme.Operator(CharStringOpCode.RLineTo));

            var bcharInfo = new CharStringInfo();

            bcharInfo.Path.RMoveTo(74, 75);

            bcharInfo.Hints.Add(CharStringLexeme.Operand(1));
            bcharInfo.Hints.Add(CharStringLexeme.Operand(2));
            bcharInfo.Hints.Add(CharStringLexeme.Operand(3));
            bcharInfo.Hints.Add(CharStringLexeme.Operand(4));
            bcharInfo.Hints.Add(CharStringLexeme.Operator(CharStringOpCode.HStemHm));
            bcharInfo.Hints.Add(CharStringLexeme.Operator(CharStringOpCode.HintMask));
            bcharInfo.Hints.Add(CharStringLexeme.Mask(0));

            bcharInfo.Content.Add(CharStringLexeme.Operand(30));
            bcharInfo.Content.Add(CharStringLexeme.Operand(50));
            bcharInfo.Content.Add(CharStringLexeme.Operator(CharStringOpCode.RMoveTo));

            bcharInfo.Content.Add(CharStringLexeme.Operand(20));
            bcharInfo.Content.Add(CharStringLexeme.Operand(25));
            bcharInfo.Content.Add(CharStringLexeme.Operator(CharStringOpCode.RLineTo));

            var merged = SeacMerger.Merge(new CharString(acharInfo), new CharString(bcharInfo), 10, 20);

            var expectedContent = new List<CharStringLexeme>
            {
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

            Assert.AreEqual(expectedContent, merged);
        }
    }
}
