// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Functions.PostScript.SyntaxTree;

namespace PdfToSvg.Functions.PostScript
{
    internal static class PostScriptCompiler
    {
        public static PostScriptCompiledExpression? Compile(
            PostScriptExpression expression, double[] domain, double[] range)
        {
            var inputCount = domain.Length / 2;
            var outputCount = range.Length / 2;

            if (!PostScriptSyntaxBuilder.TryBuild(expression, inputCount, outputCount, out var syntaxTree))
            {
                return null;
            }

            var context = new PostScriptCompilationContext();
            context.Registers.AddRange(syntaxTree.Parameters);

            var outputRegisters = new int[outputCount];

            for (var i = 0; i < outputCount; i++)
            {
                var stackFrame = syntaxTree.Outputs[i];
                if (!stackFrame.TryCompile(context, out var outputRegister))
                {
                    return null;
                }

                outputRegisters[i] = outputRegister;
            }

            return new PostScriptCompiledExpression(
                context.Instructions.ToArray(), context.Registers.Count, outputRegisters, domain, range);
        }
    }
}
