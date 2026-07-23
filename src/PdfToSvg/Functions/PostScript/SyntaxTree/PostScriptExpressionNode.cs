// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Functions.PostScript.SyntaxTree
{
    internal sealed class PostScriptExpressionNode(PostScriptExpression expression) : PostScriptNode
    {
        public override string DebugView => "{ ... }";

        public override PostScriptValueType Type => PostScriptValueType.Expression;
        public override bool PotentiallyInfinite => false;

        public PostScriptExpression Expression { get; } = expression;

        public override PostScriptNode Simplify(PostScriptSyntaxBuilderContext context) => this;

        public override void MarkReferenced()
        {
            References++;
        }

        public override bool TryCompile(PostScriptCompilationContext context, out short register)
        {
            register = -1;
            return false;
        }
    }
}
