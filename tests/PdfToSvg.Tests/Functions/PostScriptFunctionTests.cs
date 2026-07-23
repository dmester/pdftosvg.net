// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.DocumentModel;
using PdfToSvg.Functions;
using PdfToSvg.Functions.PostScript;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Functions
{
    public class PostScriptFunctionTests
    {
        private const double DomainMin = -100000d;
        private const double DomainMax = 100000d;

        private const double RangeMin = -10000d;
        private const double RangeMax = 10000d;

        [TestCase(1, "{ }", 1f, TestName = "Noop")]
        [TestCase(1, 42.5, "{ add }", 43.5, TestName = "Add")]
        [TestCase(0.5, 1, "{ sub }", -0.5, TestName = "Sub")]
        [TestCase(20, .5, "{ mul }", 10, TestName = "Mul")]
        [TestCase(2, 4.0, "{ div }", 0.5, TestName = "Div")]
        [TestCase(4.5, 2, "{ idiv }", 2, TestName = "Idiv")]
        [TestCase(8, 3, "{ mod }", 2, TestName = "Mod")]
        [TestCase(-45, "{ neg }", 45, TestName = "Neg")]
        [TestCase(-43.5, "{ abs }", 43.5, TestName = "Abs (negative)")]
        [TestCase(43.5, "{ abs }", 43.5, TestName = "Abs (positive)")]
        [TestCase(7.2, "{ ceiling }", 8, TestName = "Ceiling (positive)")]
        [TestCase(-7.2, "{ ceiling }", -7, TestName = "Ceiling (negative)")]
        [TestCase(7.2, "{ floor }", 7, TestName = "Floor (positive)")]
        [TestCase(-7.2, "{ floor }", -8, TestName = "Floor (negative)")]
        [TestCase(7.4, "{ round }", 7, TestName = "Round (up)")]
        [TestCase(7.6, "{ round }", 8, TestName = "Round (down)")]
        [TestCase(7.2, "{ truncate }", 7, TestName = "Truncate (positive)")]
        [TestCase(-7.2, "{ truncate }", -7, TestName = "Truncate (negative)")]
        [TestCase(25, "{ sqrt }", 5, TestName = "Sqrt")]
        [TestCase(30, "{ sin }", .5, TestName = "Sin")]
        [TestCase(60, "{ cos }", .5, TestName = "Cos")]
        [TestCase(0, 1, "{ atan }", 0, TestName = "Atan (0 1)")]
        [TestCase(1, 0, "{ atan }", 90, TestName = "Atan (1 0)")]
        [TestCase(-100, 0, "{ atan }", 270, TestName = "Atan (-100 0)")]
        [TestCase(4, 4, "{ atan }", 45, TestName = "Atan (4 4)")]
        [TestCase(2, 4, "{ exp }", 16, TestName = "Exp")]
        [TestCase(100, "{ ln }", 4.6051701859880913680359829093687, TestName = "Ln")]
        [TestCase(100000, "{ log }", 5, TestName = "Log")]
        [TestCase(9.123456, "{ cvi }", 9, TestName = "Cvi")]
        [TestCase(9, "{ cvr }", 9, TestName = "Cvr")]
        [TestCase(9.123, 9.123, "{ eq { 1 } { 0 } ifelse }", 1, TestName = "Eq (true)")]
        [TestCase(9, 5, "{ eq { 1 } { 0 } ifelse }", 0, TestName = "Eq (false)")]
        [TestCase(1, "{ 1 eq dup eq { 1 } { 0 } ifelse }", 1, TestName = "Eq (same boolean)")]
        [TestCase(1, "{ true eq { 1 } { 0 } ifelse }", 0, TestName = "Eq (different types)")]
        [TestCase("{ { 1 } dup eq { 1 } { 0 } ifelse }", 1, TestName = "Eq (same expression)")]
        [TestCase(9.123, 9.123, "{ ne { 1 } { 0 } ifelse }", 0, TestName = "Ne (false)")]
        [TestCase(9, 5, "{ ne { 1 } { 0 } ifelse }", 1, TestName = "Ne (true)")]

        [TestCase(1, 2, "{ ge { 1 } { 0 } ifelse }", 0, TestName = "Ge (less)")]
        [TestCase(2, 2, "{ ge { 1 } { 0 } ifelse }", 1, TestName = "Ge (equal)")]
        [TestCase(2, 1, "{ ge { 1 } { 0 } ifelse }", 1, TestName = "Ge (greater)")]

        [TestCase(1, 2, "{ gt { 1 } { 0 } ifelse }", 0, TestName = "Gt (less)")]
        [TestCase(2, 2, "{ gt { 1 } { 0 } ifelse }", 0, TestName = "Gt (equal)")]
        [TestCase(2, 1, "{ gt { 1 } { 0 } ifelse }", 1, TestName = "Gt (greater)")]

        [TestCase(1, 2, "{ le { 1 } { 0 } ifelse }", 1, TestName = "Le (less)")]
        [TestCase(2, 2, "{ le { 1 } { 0 } ifelse }", 1, TestName = "Le (equal)")]
        [TestCase(2, 1, "{ le { 1 } { 0 } ifelse }", 0, TestName = "Le (greater)")]

        [TestCase(1, 2, "{ lt { 1 } { 0 } ifelse }", 1, TestName = "Lt (less)")]
        [TestCase(2, 2, "{ lt { 1 } { 0 } ifelse }", 0, TestName = "Lt (equal)")]
        [TestCase(2, 1, "{ lt { 1 } { 0 } ifelse }", 0, TestName = "Lt (greater)")]

        [TestCase(1370, 4032, "{ and }", 1344, TestName = "And (bitwise)")]
        [TestCase(1, "{ 1 eq dup and { 1 } { 0 } ifelse }", 1, TestName = "And (logical, 11)")]
        [TestCase(1, "{ dup 1 eq exch 0 eq and { 1 } { 0 } ifelse }", 0, TestName = "And (logical, 10)")]
        [TestCase(0, "{ dup 1 eq exch 0 eq and { 1 } { 0 } ifelse }", 0, TestName = "And (logical, 01)")]
        [TestCase(0, "{ 1 eq dup and { 1 } { 0 } ifelse }", 0, TestName = "And (logical, 00)")]

        [TestCase(1370, 4032, "{ or }", 4058, TestName = "Or (bitwise)")]
        [TestCase(1, "{ 1 eq dup or { 1 } { 0 } ifelse }", 1, TestName = "Or (logical, 11)")]
        [TestCase(1, "{ dup 1 eq exch 0 eq or { 1 } { 0 } ifelse }", 1, TestName = "Or (logical, 10)")]
        [TestCase(0, "{ dup 1 eq exch 0 eq or { 1 } { 0 } ifelse }", 1, TestName = "Or (logical, 01)")]
        [TestCase(0, "{ 1 eq dup or { 1 } { 0 } ifelse }", 0, TestName = "Or (logical, 00)")]

        [TestCase(1370, 4032, "{ xor }", 2714, TestName = "Xor (bitwise)")]
        [TestCase(1, "{ 1 eq dup xor { 1 } { 0 } ifelse }", 0, TestName = "Xor (logical, 11)")]
        [TestCase(1, "{ dup 1 eq exch 0 eq xor { 1 } { 0 } ifelse }", 1, TestName = "Xor (logical, 10)")]
        [TestCase(0, "{ dup 1 eq exch 0 eq xor { 1 } { 0 } ifelse }", 1, TestName = "Xor (logical, 01)")]
        [TestCase(0, "{ 1 eq dup xor { 1 } { 0 } ifelse }", 0, TestName = "Xor (logical, 00)")]

        [TestCase(1370, "{ not }", ~1370, TestName = "Not (bitwise)")]
        [TestCase(1, "{ 1 eq not { 1 } { 0 } ifelse }", 0, TestName = "Not (logical, 1)")]
        [TestCase(0, "{ 1 eq not { 1 } { 0 } ifelse }", 1, TestName = "Not (logical, 0)")]

        [TestCase(8, 2, "{ bitshift }", 32, TestName = "Bitshift (left)")]
        [TestCase(8, -2, "{ bitshift }", 2, TestName = "Bitshift (right)")]

        [TestCase(1, 2, 3, 4, "{ pop pop }", 1, 2, TestName = "Pop")]
        [TestCase(1, 2, 3, 4, "{ exch }", 1, 2, 4, 3, TestName = "Exch")]
        [TestCase(1, 2, "{ dup dup }", 1, 2, 2, 2, TestName = "Dup")]
        [TestCase(1, 2, 3, 4, "{ 0 copy }", 1, 2, 3, 4, TestName = "Copy (0)")]
        [TestCase(1, 2, 3, 4, "{ 3 copy }", 1, 2, 3, 4, 2, 3, 4, TestName = "Copy (3)")]
        [TestCase(1, 2, 3, 4, 5, "{ 1 index }", 1, 2, 3, 4, 5, 4, TestName = "Index")]

        [TestCase(11, 12, 13, "{ 3 -1 roll }", 12, 13, 11, TestName = "Roll (3 -1)")]
        [TestCase(11, 12, 13, "{ 3 1 roll }", 13, 11, 12, TestName = "Roll (3 1)")]
        [TestCase(11, 12, 13, "{ 3 0 roll }", 11, 12, 13, TestName = "Roll (3 0)")]

        [TestCase("{ 0 { 42 } { 13 } ifelse }", 13, TestName = "Ifelse (numeric condition 0)")]
        [TestCase("{ 1 { 42 } { 13 } ifelse }", 42, TestName = "Ifelse (numeric condition 1)")]
        [TestCase("{ -1 { 42 } { 13 } ifelse }", 42, TestName = "Ifelse (numeric condition -1)")]
        [TestCase("{ 0 0 { 13 } if }", 0, TestName = "If (numeric condition 0)")]
        [TestCase("{ 2 { 42 } if }", 42, TestName = "If (numeric condition 2)")]

        [TestCase(1, 2, 3, 4, "{ }", 4, TestName = "Stack with too many elements")]

        [TestCase(-3.2, "{ dup abs exch round add }", 0.2, TestName = "Reuse registers (unary ops)")]
        [TestCase(3, 2, "{ 2 copy sub 3 1 roll add mul }", 5, TestName = "Reuse registers (binary ops)")]
        [TestCase(0.25, "{ dup sqrt exch dup neg exch dup mul add sub }", 0.6875,
            TestName = "Reuse registers (√x - [-x + x * x])")]

        // NaN -> 0, -∞ -> Range[min], ∞ -> Range[max]
        [TestCase(-1, "{ sqrt }", 0, TestName = "NaN (sqrt -1)")]
        [TestCase(0, "{ ln }", RangeMin, TestName = "-Infinity (ln 0)")]
        [TestCase(-1, "{ ln }", 0, TestName = "NaN (ln -1)")]
        [TestCase(0, "{ log }", RangeMin, TestName = "-Infinity (log 0)")]
        [TestCase(1, 0, "{ div }", RangeMax, TestName = "Infinity (div 0)")]
        [TestCase(1, 0, "{ idiv }", RangeMax, TestName = "Infinity (idiv 0)")]
        [TestCase(-1, 0, "{ div }", RangeMin, TestName = "-Infinity (div 0)")]
        [TestCase(-1, 0, "{ idiv }", RangeMin, TestName = "-Infinity (idiv 0)")]
        [TestCase(1, 0, "{ mod }", 0, TestName = "NaN (mod 0)")]
        [TestCase(-1, "{ log }", 0, TestName = "NaN (log -1)")]
        [TestCase(10000, 10000, "{ exp }", RangeMax, TestName = "Infinity (exp)")]
        [TestCase(-1, 0, "{ div cvi }", RangeMin, TestName = "-Infinity (saturated in cvi)")]
        [TestCase(1, 0, "{ div cvi }", RangeMax, TestName = "Infinity (saturated in cvi)")]
        [TestCase(-1, "{ ln cvi 1 add }", 1, TestName = "NaN (propagated NaN via cvi)")]
        [TestCase(-1, "{ ln round cvi 1 add }", 1, TestName = "NaN (propagated NaN via rounding, cvi)")]
        // [TestCase(10, 0, "{ atan }", 0, TestName = "NaN (atan 0)")]

        [TestCase(10000, 10000, "{ exp 1 add }", RangeMax, TestName = "Infinity (propagated)")]
        [TestCase(10000, 10000, "{ exp 0 mul 1 add }", 0, TestName = "NaN (propagated Infinity * 0)")]
        [TestCase(-1, "{ sqrt 1 add }", 0, TestName = "NaN (propagated)")]
        [TestCase(-1, "{ sqrt 0 mul 1 add }", 0, TestName = "NaN (propagated NaN * 0)")]

        [TestCase(49, "{ 49 div cvi }", 1, TestName = "Div (rounding before cvi)")]
        [TestCase(49, "{ 49 div floor }", 1, TestName = "Div (rounding before floor)")]
        [TestCase(100000, "{ 100000 mul floor cvi 2147483647 sub }", 0,
            TestName = "Cvi (saturation after floor)")]
        [TestCase(-100000, "{ 100000 mul ceiling cvi -2147483648 sub }", 0,
            TestName = "Cvi (saturation after ceiling)")]
        [TestCase(0, "{ 0 atan 0 mul 1 add }", 0, TestName = "NaN (propagated atan * 0)")]

        public void ValidDefinition(params object[] testData)
        {
            var i = 0;

            var input = ReadNumbers(testData, ref i);
            var definition = (string)testData[i++];
            var expectedOutput = ReadNumbers(testData, ref i);

            Assert.Multiple(() =>
            {
                Evaluate(input, definition, expectedOutput, enableCompilation: true, usingVariableInput: true);
                Evaluate(input, definition, expectedOutput, enableCompilation: false, usingVariableInput: true);

                if (input.Length > 0)
                {
                    var constInputs = string.Join(" ", input.Select(x => x.ToString(CultureInfo.InvariantCulture)));
                    var constDefinition = "{ " + constInputs + " " + definition.TrimStart('{', ' ');

                    Evaluate([], constDefinition, expectedOutput, enableCompilation: true, usingVariableInput: false);
                    Evaluate([], constDefinition, expectedOutput, enableCompilation: false, usingVariableInput: false);
                }
            });
        }

        [Test]
        public void Parse_DeepBlocks()
        {
            var source = new string('{', 30000) + new string('}', 30000);
            Assert.Throws<PostScriptFunctionException>(() => PostScriptParser.Parse(source));
        }

        [Test]
        public void Parse_BlockDepthBoundary()
        {
            var source = new string('{', PostScriptLimits.MaxExecutionDepth) +
                new string('}', PostScriptLimits.MaxExecutionDepth);
            Assert.DoesNotThrow(() => PostScriptParser.Parse(source));
            Assert.Throws<PostScriptFunctionException>(() => PostScriptParser.Parse("{" + source + "}"));
        }

        [TestCase("sin ")]
        [TestCase("dup mul ")]
        public void Compile_LongExpression(string instructions)
        {
            var source = "{ " + string.Concat(Enumerable.Repeat(instructions, 30000)) + "}";
            var expression = PostScriptParser.Parse(source);
            Assert.IsNull(PostScriptCompiler.Compile(expression, new[] { 0d, 1d }, new[] { 0d, 1d }));
            Assert.Throws<PostScriptFunctionException>(() =>
                Evaluate(new[] { 0f }, source, new[] { 0f }, enableCompilation: true, usingVariableInput: true));
        }

        [TestCase("sin ")]
        [TestCase("dup mul ")]
        public void Compile_InterpretedFallback(string instructions)
        {
            var source = "{ " + string.Concat(Enumerable.Repeat(
                instructions, PostScriptLimits.MaxCompiledInstructions + 1)) + "}";
            var expression = PostScriptParser.Parse(source);
            Assert.IsNull(PostScriptCompiler.Compile(expression, new[] { 0d, 1d }, new[] { 0d, 1d }));
            Evaluate(new[] { 0f }, source, new[] { 0f }, enableCompilation: true, usingVariableInput: true);
        }

        [Test]
        public void Compile_SharedExpression()
        {
            var source = "{ " + string.Concat(Enumerable.Repeat("dup mul ", 60)) + "0 mul }";
            Evaluate(new[] { 0f }, source, new[] { 0f }, enableCompilation: true, usingVariableInput: true);
        }

        [TestCase(true, "{ { dup true exch if } dup true exch if }")]
        [TestCase(false, "{ { dup true exch if } dup true exch if }")]
        [TestCase(true, "{ { dup true exch {} ifelse } dup true exch {} ifelse }")]
        [TestCase(false, "{ { dup true exch {} ifelse } dup true exch {} ifelse }")]
        public void Evaluate_RecursiveBlock(bool enableCompilation, string source)
        {
            Assert.Throws<PostScriptFunctionException>(() =>
                Evaluate(new[] { 0f }, source, new[] { 0f }, enableCompilation, usingVariableInput: true));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Evaluate_SharedInstructionBudget(bool enableCompilation)
        {
            // Each block fits within the budget, but executing it twice exceeds the shared budget.
            var source = "{ { " + string.Concat(Enumerable.Repeat(
                "0 pop ", PostScriptLimits.MaxInterpretedInstructions / 4)) +
                "} dup true exch if true exch if }";
            Assert.Throws<PostScriptFunctionException>(() =>
                Evaluate(new[] { 0f }, source, new[] { 0f }, enableCompilation, usingVariableInput: true));
        }

        [Test]
        public void Execute_BudgetResets()
        {
            var expression = PostScriptParser.Parse(
                "{ " + string.Concat(Enumerable.Repeat(
                    "0 pop ", PostScriptLimits.MaxInterpretedInstructions / 2)) + "}");
            var stack = new PostScriptStack();
            expression.Execute(ref stack);
            expression.Execute(ref stack);
            Assert.AreEqual(0, stack.Count);
        }

        private void Evaluate(float[] input, string definition, float[] expectedOutput, bool enableCompilation, bool usingVariableInput)
        {
            var domain = input
                .SelectMany(x => new[] { DomainMin, DomainMax })
                .Cast<object>()
                .ToArray();

            var range = expectedOutput
                .SelectMany(x => new[] { RangeMin, RangeMax })
                .Cast<object>()
                .ToArray();

            var dict = new PdfDictionary
            {
                { Names.Domain, domain },
                { Names.Range, range },
            };

            var binaryDefinition = Encoding.ASCII.GetBytes(definition);
            dict.MakeIndirectObject(new PdfObjectId(), new PdfMemoryStream(dict, binaryDefinition, binaryDefinition.Length));

            var function = new PostScriptFunction(dict, enableCompilation, default);
            var actualOutput = new float[function.OutputCount];
            function.Evaluate(input, 0, actualOutput, 0);

            Assert.That(
                actualOutput,
                Is.EqualTo(expectedOutput).Within(0.0001),
                $"EnableCompilation: {enableCompilation}, UseVariableInput: {usingVariableInput}");
        }

        private static float[] ReadNumbers(object[] readFrom, ref int index)
        {
            var result = new List<float>();

            for (; index < readFrom.Length; index++)
            {
                var value = readFrom[index];

                if (value is int iValue)
                {
                    result.Add(iValue);
                }
                else if (value is double dValue)
                {
                    result.Add((float)dValue);
                }
                else if (value is float fValue)
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
