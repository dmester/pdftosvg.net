// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.DocumentModel;
using PdfToSvg.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Functions
{
    public class PostScriptFunctionTests
    {
        [TestCase(1d, "{ }", 1d, TestName = "Noop")]
        [TestCase("{ 1 42.5 add }", 43.5d, TestName = "Add")]
        [TestCase("{ 0.5 1 sub }", -0.5d, TestName = "Sub")]
        [TestCase("{ 20 0.5 mul }", 10d, TestName = "Mul")]
        [TestCase("{ 2 4.0 div }", 0.5d, TestName = "Div")]
        [TestCase("{ 4.5 2 idiv }", 2d, TestName = "Idiv")]
        [TestCase("{ 8 3 mod }", 2d, TestName = "Mod")]
        [TestCase("{ -45 neg }", 45d, TestName = "Neg")]
        [TestCase("{ -43.5 abs }", 43.5d, TestName = "Abs (negative)")]
        [TestCase("{ 43.5 abs }", 43.5d, TestName = "Abs (positive)")]
        [TestCase("{ 7.2 ceiling }", 8d, TestName = "Ceiling (positive)")]
        [TestCase("{ -7.2 ceiling }", -7d, TestName = "Ceiling (negative)")]
        [TestCase("{ 7.2 floor }", 7d, TestName = "Floor (positive)")]
        [TestCase("{ -7.2 floor }", -8d, TestName = "Floor (negative)")]
        [TestCase("{ 7.4 round }", 7d, TestName = "Round (up)")]
        [TestCase("{ 7.6 round }", 8d, TestName = "Round (down)")]
        [TestCase("{ 7.2 truncate }", 7d, TestName = "Truncate (positive)")]
        [TestCase("{ -7.2 truncate }", -7d, TestName = "Truncate (negative)")]
        [TestCase("{ 25 sqrt }", 5d, TestName = "Sqrt")]
        [TestCase("{ 30 sin }", 0.5d, TestName = "Sin")]
        [TestCase("{ 60 cos }", 0.5d, TestName = "Cos")]
        [TestCase("{ 0 1 atan }", 0d, TestName = "Atan (0 1)")]
        [TestCase("{ 1 0 atan }", 90d, TestName = "Atan (1 0)")]
        [TestCase("{ -100 0 atan }", 270d, TestName = "Atan (-100 0)")]
        [TestCase("{ 4 4 atan }", 45d, TestName = "Atan (4 4)")]
        [TestCase("{ 2 4 exp }", 16d, TestName = "Exp")]
        [TestCase("{ 100 ln }", 4.6051701859880913680359829093687d, TestName = "Ln")]
        [TestCase("{ 100000 log }", 5d, TestName = "Log")]
        [TestCase("{ 9.123456 cvi }", 9d, TestName = "Cvi")]
        [TestCase("{ 9 cvr }", 9d, TestName = "Cvr")]
        [TestCase("{ 9.123 9.123 eq { 1 } { 0 } ifelse }", 1d, TestName = "Eq (true)")]
        [TestCase("{ 9 5 eq { 1 } { 0 } ifelse }", 0d, TestName = "Eq (false)")]
        [TestCase("{ 9.123 9.123 ne { 1 } { 0 } ifelse }", 0d, TestName = "Ne (false)")]
        [TestCase("{ 9 5 ne { 1 } { 0 } ifelse }", 1d, TestName = "Ne (true)")]

        [TestCase("{ 1 2 ge { 1 } { 0 } ifelse }", 0d, TestName = "Ge (less)")]
        [TestCase("{ 2 2 ge { 1 } { 0 } ifelse }", 1d, TestName = "Ge (equal)")]
        [TestCase("{ 2 1 ge { 1 } { 0 } ifelse }", 1d, TestName = "Ge (greater)")]

        [TestCase("{ 1 2 gt { 1 } { 0 } ifelse }", 0d, TestName = "Gt (less)")]
        [TestCase("{ 2 2 gt { 1 } { 0 } ifelse }", 0d, TestName = "Gt (equal)")]
        [TestCase("{ 2 1 gt { 1 } { 0 } ifelse }", 1d, TestName = "Gt (greater)")]

        [TestCase("{ 1 2 le { 1 } { 0 } ifelse }", 1d, TestName = "Le (less)")]
        [TestCase("{ 2 2 le { 1 } { 0 } ifelse }", 1d, TestName = "Le (equal)")]
        [TestCase("{ 2 1 le { 1 } { 0 } ifelse }", 0d, TestName = "Le (greater)")]

        [TestCase("{ 1 2 lt { 1 } { 0 } ifelse }", 1d, TestName = "Lt (less)")]
        [TestCase("{ 2 2 lt { 1 } { 0 } ifelse }", 0d, TestName = "Lt (equal)")]
        [TestCase("{ 2 1 lt { 1 } { 0 } ifelse }", 0d, TestName = "Lt (greater)")]

        [TestCase("{ 1370 4032 and }", 1344d, TestName = "And (bitwise)")]
        [TestCase("{ true true and { 1 } { 0 } ifelse }", 1d, TestName = "And (logical, 11)")]
        [TestCase("{ true false and { 1 } { 0 } ifelse }", 0d, TestName = "And (logical, 10)")]
        [TestCase("{ false true and { 1 } { 0 } ifelse }", 0d, TestName = "And (logical, 01)")]
        [TestCase("{ false false and { 1 } { 0 } ifelse }", 0d, TestName = "And (logical, 00)")]

        [TestCase("{ 1370 4032 or }", 4058d, TestName = "Or (bitwise)")]
        [TestCase("{ true true or { 1 } { 0 } ifelse }", 1d, TestName = "Or (logical, 11)")]
        [TestCase("{ true false or { 1 } { 0 } ifelse }", 1d, TestName = "Or (logical, 10)")]
        [TestCase("{ false true or { 1 } { 0 } ifelse }", 1d, TestName = "Or (logical, 01)")]
        [TestCase("{ false false or { 1 } { 0 } ifelse }", 0d, TestName = "Or (logical, 00)")]

        [TestCase("{ 1370 4032 xor }", 2714d, TestName = "Xor (bitwise)")]
        [TestCase("{ true true xor { 1 } { 0 } ifelse }", 0d, TestName = "Xor (logical, 11)")]
        [TestCase("{ true false xor { 1 } { 0 } ifelse }", 1d, TestName = "Xor (logical, 10)")]
        [TestCase("{ false true xor { 1 } { 0 } ifelse }", 1d, TestName = "Xor (logical, 01)")]
        [TestCase("{ false false xor { 1 } { 0 } ifelse }", 0d, TestName = "Xor (logical, 00)")]

        [TestCase("{ 1370 not }", (double)~1370, TestName = "Not (bitwise)")]
        [TestCase("{ true not { 1 } { 0 } ifelse }", 0d, TestName = "Not (logical, 1)")]
        [TestCase("{ false not { 1 } { 0 } ifelse }", 1d, TestName = "Not (logical, 0)")]

        [TestCase("{ 8 2 bitshift }", 32d, TestName = "Bitshift (left)")]
        [TestCase("{ 8 -2 bitshift }", 2d, TestName = "Bitshift (right)")]

        [TestCase("{ 1 2 3 4 pop pop }", 1d, 2d, TestName = "Pop")]
        [TestCase("{ 1 2 3 4 exch }", 1d, 2d, 4d, 3d, TestName = "Exch")]
        [TestCase("{ 1 2 dup dup }", 1d, 2d, 2d, 2d, TestName = "Dup")]
        [TestCase("{ 1 2 3 4 0 copy }", 1d, 2d, 3d, 4d, TestName = "Copy (0)")]
        [TestCase("{ 1 2 3 4 3 copy }", 1d, 2d, 3d, 4d, 2d, 3d, 4d, TestName = "Copy (3)")]
        [TestCase("{ 1 2 3 4 5 1 index }", 1d, 2d, 3d, 4d, 5d, 4d, TestName = "Index")]

        [TestCase("{ 11 12 13 3 -1 roll }", 12d, 13d, 11d, TestName = "Roll (3 -1)")]
        [TestCase("{ 11 12 13 3 1 roll }", 13d, 11d, 12d, TestName = "Roll (3 1)")]
        [TestCase("{ 11 12 13 3 0 roll }", 11d, 12d, 13d, TestName = "Roll (3 0)")]
        public void ValidDefinition(params object[] testData)
        {
            var i = 0;

            var input = ReadNumbers(testData, ref i);
            var definition = (string)testData[i++];
            var expectedOutput = ReadNumbers(testData, ref i);

            var domain = input
                .SelectMany(x => new[] { -10000d, 10000d })
                .Cast<object>()
                .ToArray();

            var range = expectedOutput
                .SelectMany(x => new[] { -10000d, 10000d })
                .Cast<object>()
                .ToArray();

            var dict = new PdfDictionary
            {
                { Names.Domain, domain },
                { Names.Range, range },
            };

            var binaryDefinition = Encoding.ASCII.GetBytes(definition);
            dict.MakeIndirectObject(new PdfObjectId(), new PdfMemoryStream(dict, binaryDefinition, binaryDefinition.Length));

            var function = new PostScriptFunction(dict, default);
            Assert.That(function.Evaluate(input), Is.EqualTo(expectedOutput).Within(0.00001d));
        }

        private static double[] ReadNumbers(object[] readFrom, ref int index)
        {
            var result = new List<double>();

            for (; index < readFrom.Length; index++)
            {
                var value = readFrom[index];

                if (value is double dblValue)
                {
                    result.Add(dblValue);
                }
                else
                {
                    break;
                }
            }

            return result.ToArray();
        }
    }
}
