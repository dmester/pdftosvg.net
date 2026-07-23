// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Functions.PostScript;
using PdfToSvg.Functions.PostScript.SyntaxTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Functions.PostScript.SyntaxTree
{
    public class PostScriptSyntaxBuilderTests
    {
        [Test]

        // Add
        [TestCase("{ 1 2 add 3 4 add add }", "10", TestName = "Add ([1 + 2] + [3 + 4])")]
        [TestCase("{ 1 2 add 3 2 index add add }", "($3) (6) Add", TestName = "Add ([1 + 2] + [3 + $3])")]
        [TestCase("{ 1 2 add 1 index 3 add add }", "($3) (6) Add", TestName = "Add ([1 + 2] + [$3 + 3])")]
        [TestCase("{ 1 1 index add 3 4 add add }", "($3) (8) Add", TestName = "Add ([1 + $3] + [3 + 4])")]
        [TestCase("{ 1 add 3 4 add add }", "($3) (8) Add", TestName = "Add ([$3 + 1] + [3 + 4])")]
        [TestCase("{ 1 index add 3 4 add add }", "(($3) ($2) Add) (7) Add", TestName = "Add ([$3 + $2] + [3 + 4])")]
        [TestCase("{ 1 add 1 index 3 add add }", "(($3) ($2) Add) (4) Add", TestName = "Add ([$3 + 1] + [$2 + 3])")]
        [TestCase("{ 1 2 add 1 index 3 index add add }", "(($3) ($2) Add) (3) Add",
            TestName = "Add ([1 + 2] + [$3 + $2])")]
        [TestCase("{ 1 1 index add 2 index 4 index add add }", "(($3) (1) Add) (($2) ($1) Add) Add",
            TestName = "Add ([1 + $3] + [$2 + $1])")] // No simplification

        // Sub
        [TestCase("{ 5 2 sub }", "3", TestName = "Sub (5 - 2)")]
        [TestCase("{ 1 index sub }", "($3) ($2) Sub", TestName = "Sub ($3 - $2)")]
        [TestCase("{ 0 sub }", "$3", TestName = "Sub ($3 - 0)")]
        [TestCase("{ 0 exch sub }", "($3) Neg", TestName = "Sub (0 - $3)")]
        [TestCase("{ 1 sub 2 sub }", "($3) (-3) Add", TestName = "Sub ([$3 - 1] - 2)")]
        [TestCase("{ 1 add 2 sub }", "($3) (-1) Add", TestName = "Sub ([$3 + 1] - 2)")]
        [TestCase("{ 1 sub 2 add }", "($3) (1) Add", TestName = "Sub ([$3 - 1] + 2)")]
        [TestCase("{ 1 index 1 add sub }", "($3) (($2) (1) Add) Sub",
            TestName = "Sub ($3 - [$2 + 1])")] // No simplification

        // Div
        [TestCase("{ 5 2 div }", "2.5", TestName = "Div (5 / 2)")]
        [TestCase("{ 2 div }", "($3) (0.5) Mul", TestName = "Div ($3 / 2)")]
        [TestCase("{ 3 div }", "($3) (3) Div", TestName = "Div ($3 / 3)")]
        [TestCase("{ 4 div }", "($3) (0.25) Mul", TestName = "Div ($3 / 4)")]

        // Mul
        [TestCase("{ 2 3 mul }", "6", TestName = "Mul (2 * 3)")]
        [TestCase("{ 3 mul 4 5 mul mul }", "($3) (60) Mul", TestName = "Mul ([$3 * 3] * [4 * 5])")]
        [TestCase("{ 3 mul 4 0 mul mul }", "0", TestName = "Mul ([$3 * 3] * [4 * 0])")]
        [TestCase("{ 3 mul 1 index 5 mul mul }", "(($3) ($2) Mul) (15) Mul", TestName = "Mul ([$3 * 3] * [$2 * 5])")]

        // Const operations
        [TestCase("{ 5 2 idiv }", "2", TestName = "Idiv (5 idiv 2)")]
        [TestCase("{ 5 2 mod }", "1", TestName = "Mod (5 % 2)")]
        [TestCase("{ 0 1 atan }", "0", TestName = "Atan (0 atan 1)")]
        [TestCase("{ 2 5 exp }", "32", TestName = "Exp (2 ^ 5)")]
        [TestCase("{ 2 2 eq }", "true", TestName = "Eq (2 == 2)")]
        [TestCase("{ 2 2 ne }", "false", TestName = "Ne (2 != 2)")]
        [TestCase("{ 5 2 gt }", "true", TestName = "Gt (5 > 2)")]
        [TestCase("{ 5 2 ge }", "true", TestName = "Ge (5 >= 2)")]
        [TestCase("{ 5 2 lt }", "false", TestName = "Lt (5 < 2)")]
        [TestCase("{ 5 2 le }", "false", TestName = "Le (5 <= 2)")]
        [TestCase("{ 5.78 cvi }", "5", TestName = "Cvi (5.78)")]
        [TestCase("{ 5.78 cvr }", "5.78", TestName = "Cvr (5.78)")]
        [TestCase("{ 42 neg }", "-42", TestName = "Neg (42)")]
        [TestCase("{ 5.78 round }", "6", TestName = "Round (5.78)")]
        [TestCase("{ 5.78 truncate }", "5", TestName = "Truncate (5.78)")]
        [TestCase("{ 16 sqrt }", "4", TestName = "Sqrt (16)")]
        [TestCase("{ 30 sin }", "0.5", TestName = "Sin (30)")]
        [TestCase("{ 60 cos }", "0.5", TestName = "Cos (60)")]
        [TestCase("{ 5.78 ceiling }", "6", TestName = "Ceiling (5.78)")]
        [TestCase("{ 5.78 floor }", "5", TestName = "Floor (5.78)")]
        [TestCase("{ 100 ln }", "4.61", TestName = "Ln (100)")]
        [TestCase("{ 10000 log }", "4", TestName = "Log (10000)")]
        [TestCase("{ 168 15 not and 1 or }", "161", TestName = "Bitwise ([168 & ~15] | 1)")]

        public void Simplify(string source, string expected)
        {
            var expression = PostScriptParser.Parse(source);

            Assert.IsTrue(PostScriptSyntaxBuilder.TryBuild(expression, 4, 4, out var tree));

            Assert.AreEqual(expected, tree.Outputs.LastOrDefault()?.DebugView);
        }

        [Test]
        public void TryBuild_ParameterStackBoundary()
        {
            var expression = PostScriptParser.Parse("{ }");
            var stackSize = PostScriptLimits.StackSize;

            Assert.IsTrue(PostScriptSyntaxBuilder.TryBuild(expression, stackSize, 1, out _));
            Assert.IsFalse(PostScriptSyntaxBuilder.TryBuild(expression, stackSize + 1, 1, out _));
        }

        [TestCase("{ 1 }")]
        [TestCase("{ true }")]
        [TestCase("{ false }")]
        [TestCase("{ {} }")]
        [TestCase("{ dup }")]
        public void TryBuild_StackBoundary(string source)
        {
            var expression = PostScriptParser.Parse(source);
            var parameterCount = PostScriptLimits.StackSize - 1;

            Assert.IsTrue(PostScriptSyntaxBuilder.TryBuild(expression, parameterCount, 1, out _));
            Assert.IsFalse(PostScriptSyntaxBuilder.TryBuild(expression, parameterCount + 1, 1, out _));
        }

        [Test]
        public void TryBuild_CopyStackBoundary()
        {
            var copyCount = PostScriptLimits.StackSize / 2;
            var parameterCount = PostScriptLimits.StackSize - copyCount;
            var expression = PostScriptParser.Parse("{ " + copyCount + " copy }");

            Assert.IsTrue(PostScriptSyntaxBuilder.TryBuild(expression, parameterCount, 1, out _));
            Assert.IsFalse(PostScriptSyntaxBuilder.TryBuild(expression, parameterCount + 1, 1, out _));
        }

        [Test]
        public void TryBuild_RepeatedCopy()
        {
            var expression = PostScriptParser.Parse("{ 1 copy 2 copy 4 copy 8 copy 16 copy 32 copy 64 copy }");

            Assert.IsFalse(PostScriptSyntaxBuilder.TryBuild(expression, 1, 1, out _));
        }

        [Test]
        public void TryCompile_RegistersReused()
        {
            var expression = PostScriptParser.Parse("{ add 2 mul }");
            Assert.IsTrue(PostScriptSyntaxBuilder.TryBuild(expression, 2, 1, out var ast));

            var context = new PostScriptCompilationContext();
            context.Registers.Add(new PostScriptParameterNode(0));
            context.Registers.Add(new PostScriptParameterNode(1));

            Assert.IsTrue(ast.Outputs[0].TryCompile(context, out var register));

            var instructions = context.Instructions;

            Assert.AreEqual(2, context.Registers.Count);
            Assert.AreEqual(new[]
            {
                new PostScriptCompiledInstruction(PostScriptOpCode.Add, 0, 0, 1, 0),
                new PostScriptCompiledInstruction(PostScriptOpCode.Mul, 0, 0, -1, 2f),
            }, instructions);
        }

        [Test]
        public void TryCompile_RegistersNotOverwrittenTooEarly_Binary()
        {
            var expression = PostScriptParser.Parse("{ 2 copy sub 3 1 roll add mul }");
            Assert.IsTrue(PostScriptSyntaxBuilder.TryBuild(expression, parameterCount: 2, outputCount: 1, out var ast));

            var context = new PostScriptCompilationContext();
            context.Registers.AddRange(ast.Parameters);

            Assert.IsTrue(ast.Outputs[0].TryCompile(context, out var register));

            var instructions = context.Instructions;

            Assert.AreEqual(3, context.Registers.Count);
            Assert.AreEqual(new[]
            {
                new PostScriptCompiledInstruction(PostScriptOpCode.Sub, 2, 0, 1, 0),
                new PostScriptCompiledInstruction(PostScriptOpCode.Add, 0, 0, 1, 0),
                new PostScriptCompiledInstruction(PostScriptOpCode.Mul, 0, 2, 0, 0),
            }, instructions);
        }

        [Test]
        public void TryCompile_RegistersNotOverwrittenTooEarly_Unary()
        {
            var expression = PostScriptParser.Parse("{ dup abs exch round add }");
            Assert.IsTrue(PostScriptSyntaxBuilder.TryBuild(expression, parameterCount: 1, outputCount: 1, out var ast));

            var context = new PostScriptCompilationContext();
            context.Registers.AddRange(ast.Parameters);

            Assert.IsTrue(ast.Outputs[0].TryCompile(context, out var register));

            var instructions = context.Instructions;

            Assert.AreEqual(2, context.Registers.Count);
            Assert.AreEqual(new[]
            {
                new PostScriptCompiledInstruction(PostScriptOpCode.Abs, 1, 0, -1, 0),
                new PostScriptCompiledInstruction(PostScriptOpCode.Round, 0, 0, -1, 0),
                new PostScriptCompiledInstruction(PostScriptOpCode.Add, 0, 1, 0, 0),
            }, instructions);
        }
    }
}
