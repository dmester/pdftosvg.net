// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System.Collections.Generic;

namespace PdfToSvg.Functions.PostScript.SyntaxTree
{
    internal sealed class PostScriptSyntaxBuilderContext
    {
        public int RemainingInstructions = PostScriptLimits.MaxCompiledInstructions;

        private readonly Dictionary<double, PostScriptConstantNode> internedConstants = new();
        private readonly PostScriptConstantNode falseConstant = new(false);
        private readonly PostScriptConstantNode trueConstant = new(true);

        public PostScriptConstantNode Intern(double value)
        {
            if (!internedConstants.TryGetValue(value, out var internedConstant))
            {
                internedConstant = new PostScriptConstantNode(value);
                internedConstants[value] = internedConstant;
            }

            return internedConstant;
        }

        public PostScriptConstantNode Intern(bool value) => value ? trueConstant : falseConstant;
    }
}
