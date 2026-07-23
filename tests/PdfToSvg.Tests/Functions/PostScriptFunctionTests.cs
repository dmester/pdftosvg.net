// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.DocumentModel;
using PdfToSvg.Functions;
using PdfToSvg.Functions.PostScript;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Functions
{
    public class PostScriptFunctionTests
    {
        [TestCase(1f, "{ }", 1f, TestName = "Noop")]
        [TestCase("{ 1 42.5 add }", 43.5f, TestName = "Add")]
        [TestCase("{ 0.5 1 sub }", -0.5f, TestName = "Sub")]
        [TestCase("{ 20 0.5 mul }", 10f, TestName = "Mul")]
        [TestCase("{ 2 4.0 div }", 0.5f, TestName = "Div")]
        [TestCase("{ 4.5 2 idiv }", 2f, TestName = "Idiv")]
        [TestCase("{ 8 3 mod }", 2f, TestName = "Mod")]
        [TestCase("{ -45 neg }", 45f, TestName = "Neg")]
        [TestCase("{ -43.5 abs }", 43.5f, TestName = "Abs (negative)")]
        [TestCase("{ 43.5 abs }", 43.5f, TestName = "Abs (positive)")]
        [TestCase("{ 7.2 ceiling }", 8f, TestName = "Ceiling (positive)")]
        [TestCase("{ -7.2 ceiling }", -7f, TestName = "Ceiling (negative)")]
        [TestCase("{ 7.2 floor }", 7f, TestName = "Floor (positive)")]
        [TestCase("{ -7.2 floor }", -8f, TestName = "Floor (negative)")]
        [TestCase("{ 7.4 round }", 7f, TestName = "Round (up)")]
        [TestCase("{ 7.6 round }", 8f, TestName = "Round (down)")]
        [TestCase("{ 7.2 truncate }", 7f, TestName = "Truncate (positive)")]
        [TestCase("{ -7.2 truncate }", -7f, TestName = "Truncate (negative)")]
        [TestCase("{ 25 sqrt }", 5f, TestName = "Sqrt")]
        [TestCase("{ 30 sin }", 0.5f, TestName = "Sin")]
        [TestCase("{ 60 cos }", 0.5f, TestName = "Cos")]
        [TestCase("{ 0 1 atan }", 0f, TestName = "Atan (0 1)")]
        [TestCase("{ 1 0 atan }", 90f, TestName = "Atan (1 0)")]
        [TestCase("{ -100 0 atan }", 270f, TestName = "Atan (-100 0)")]
        [TestCase("{ 4 4 atan }", 45f, TestName = "Atan (4 4)")]
        [TestCase("{ 2 4 exp }", 16f, TestName = "Exp")]
        [TestCase("{ 100 ln }", 4.6051701859880913680359829093687f, TestName = "Ln")]
        [TestCase("{ 100000 log }", 5f, TestName = "Log")]
        [TestCase("{ 9.123456 cvi }", 9f, TestName = "Cvi")]
        [TestCase("{ 9 cvr }", 9f, TestName = "Cvr")]
        [TestCase("{ 9.123 9.123 eq { 1 } { 0 } ifelse }", 1f, TestName = "Eq (true)")]
        [TestCase("{ 9 5 eq { 1 } { 0 } ifelse }", 0f, TestName = "Eq (false)")]
        [TestCase("{ true true eq { 1 } { 0 } ifelse }", 1f, TestName = "Eq (same boolean)")]
        [TestCase("{ 1 true eq { 1 } { 0 } ifelse }", 0f, TestName = "Eq (different types)")]
        [TestCase("{ { 1 } dup eq { 1 } { 0 } ifelse }", 1f, TestName = "Eq (same expression)")]
        [TestCase("{ 9.123 9.123 ne { 1 } { 0 } ifelse }", 0f, TestName = "Ne (false)")]
        [TestCase("{ 9 5 ne { 1 } { 0 } ifelse }", 1f, TestName = "Ne (true)")]

        [TestCase("{ 1 2 ge { 1 } { 0 } ifelse }", 0f, TestName = "Ge (less)")]
        [TestCase("{ 2 2 ge { 1 } { 0 } ifelse }", 1f, TestName = "Ge (equal)")]
        [TestCase("{ 2 1 ge { 1 } { 0 } ifelse }", 1f, TestName = "Ge (greater)")]

        [TestCase("{ 1 2 gt { 1 } { 0 } ifelse }", 0f, TestName = "Gt (less)")]
        [TestCase("{ 2 2 gt { 1 } { 0 } ifelse }", 0f, TestName = "Gt (equal)")]
        [TestCase("{ 2 1 gt { 1 } { 0 } ifelse }", 1f, TestName = "Gt (greater)")]

        [TestCase("{ 1 2 le { 1 } { 0 } ifelse }", 1f, TestName = "Le (less)")]
        [TestCase("{ 2 2 le { 1 } { 0 } ifelse }", 1f, TestName = "Le (equal)")]
        [TestCase("{ 2 1 le { 1 } { 0 } ifelse }", 0f, TestName = "Le (greater)")]

        [TestCase("{ 1 2 lt { 1 } { 0 } ifelse }", 1f, TestName = "Lt (less)")]
        [TestCase("{ 2 2 lt { 1 } { 0 } ifelse }", 0f, TestName = "Lt (equal)")]
        [TestCase("{ 2 1 lt { 1 } { 0 } ifelse }", 0f, TestName = "Lt (greater)")]

        [TestCase("{ 1370 4032 and }", 1344f, TestName = "And (bitwise)")]
        [TestCase("{ true true and { 1 } { 0 } ifelse }", 1f, TestName = "And (logical, 11)")]
        [TestCase("{ true false and { 1 } { 0 } ifelse }", 0f, TestName = "And (logical, 10)")]
        [TestCase("{ false true and { 1 } { 0 } ifelse }", 0f, TestName = "And (logical, 01)")]
        [TestCase("{ false false and { 1 } { 0 } ifelse }", 0f, TestName = "And (logical, 00)")]

        [TestCase("{ 1370 4032 or }", 4058f, TestName = "Or (bitwise)")]
        [TestCase("{ true true or { 1 } { 0 } ifelse }", 1f, TestName = "Or (logical, 11)")]
        [TestCase("{ true false or { 1 } { 0 } ifelse }", 1f, TestName = "Or (logical, 10)")]
        [TestCase("{ false true or { 1 } { 0 } ifelse }", 1f, TestName = "Or (logical, 01)")]
        [TestCase("{ false false or { 1 } { 0 } ifelse }", 0f, TestName = "Or (logical, 00)")]

        [TestCase("{ 1370 4032 xor }", 2714f, TestName = "Xor (bitwise)")]
        [TestCase("{ true true xor { 1 } { 0 } ifelse }", 0f, TestName = "Xor (logical, 11)")]
        [TestCase("{ true false xor { 1 } { 0 } ifelse }", 1f, TestName = "Xor (logical, 10)")]
        [TestCase("{ false true xor { 1 } { 0 } ifelse }", 1f, TestName = "Xor (logical, 01)")]
        [TestCase("{ false false xor { 1 } { 0 } ifelse }", 0f, TestName = "Xor (logical, 00)")]

        [TestCase("{ 1370 not }", (float)~1370, TestName = "Not (bitwise)")]
        [TestCase("{ true not { 1 } { 0 } ifelse }", 0f, TestName = "Not (logical, 1)")]
        [TestCase("{ false not { 1 } { 0 } ifelse }", 1f, TestName = "Not (logical, 0)")]

        [TestCase("{ 8 2 bitshift }", 32f, TestName = "Bitshift (left)")]
        [TestCase("{ 8 -2 bitshift }", 2f, TestName = "Bitshift (right)")]

        [TestCase("{ 1 2 3 4 pop pop }", 1f, 2f, TestName = "Pop")]
        [TestCase("{ 1 2 3 4 exch }", 1f, 2f, 4f, 3f, TestName = "Exch")]
        [TestCase("{ 1 2 dup dup }", 1f, 2f, 2f, 2f, TestName = "Dup")]
        [TestCase("{ 1 2 3 4 0 copy }", 1f, 2f, 3f, 4f, TestName = "Copy (0)")]
        [TestCase("{ 1 2 3 4 3 copy }", 1f, 2f, 3f, 4f, 2f, 3f, 4f, TestName = "Copy (3)")]
        [TestCase("{ 1 2 3 4 5 1 index }", 1f, 2f, 3f, 4f, 5f, 4f, TestName = "Index")]

        [TestCase("{ 11 12 13 3 -1 roll }", 12f, 13f, 11f, TestName = "Roll (3 -1)")]
        [TestCase("{ 11 12 13 3 1 roll }", 13f, 11f, 12f, TestName = "Roll (3 1)")]
        [TestCase("{ 11 12 13 3 0 roll }", 11f, 12f, 13f, TestName = "Roll (3 0)")]
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
            var actualOutput = new float[function.OutputCount];
            function.Evaluate(input, 0, actualOutput, 0);

            Assert.That(actualOutput, Is.EqualTo(expectedOutput).Within(0.00001d));
        }

        [Test]
        public void Parse_BlockDepthBoundary()
        {
            var source = new string('{', PostScriptLimits.MaxExecutionDepth) +
                new string('}', PostScriptLimits.MaxExecutionDepth);
            Assert.DoesNotThrow(() => Parse(source));
            Assert.Throws<PostScriptFunctionException>(() => Parse("{" + source + "}"));
        }

        [TestCase("{ { dup true exch if } dup true exch if }")]
        [TestCase("{ { dup true exch {} ifelse } dup true exch {} ifelse }")]
        public void Execute_RecursiveBlock(string source)
        {
            var expression = Parse(source);
            var exception = Assert.Throws<PostScriptFunctionException>(() => expression.Execute(new PostScriptStack()));
            StringAssert.Contains("execution depth limit", exception.Message);
        }

        [Test]
        public void Execute_SharedInstructionBudget()
        {
            var source = "{ { " + string.Concat(Enumerable.Repeat(
                "0 pop ", PostScriptLimits.MaxInterpretedInstructions / 4)) +
                "} dup true exch if true exch if }";
            var expression = Parse(source);

            Assert.Throws<PostScriptFunctionException>(() => expression.Execute(new PostScriptStack()));
        }

        [Test]
        public void Execute_BudgetResets()
        {
            var body = string.Concat(Enumerable.Repeat("0 pop ", PostScriptLimits.MaxInterpretedInstructions / 2));
            var stack = new PostScriptStack();
            Assert.Throws<PostScriptFunctionException>(() => Parse("{ " + body + "0 }").Execute(stack));

            var expression = Parse("{ " + body + "}");
            expression.Execute(stack);
            expression.Execute(stack);
            Assert.AreEqual(0, stack.Count);
        }

        [Test]
        public void Push_StackLimit()
        {
            var stack = new PostScriptStack();
            for (var i = 0; i < PostScriptLimits.StackSize; i++)
            {
                stack.Push(i);
            }

            Assert.Throws<PostScriptFunctionException>(() => stack.Push(0));
        }

        private static PostScriptExpression Parse(string source)
        {
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(source));
            return PostScriptParser.Parse(stream);
        }

        private static float[] ReadNumbers(object[] readFrom, ref int index)
        {
            var result = new List<float>();

            for (; index < readFrom.Length; index++)
            {
                var value = readFrom[index];

                if (value is float fValue)
                {
                    result.Add(fValue);
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
