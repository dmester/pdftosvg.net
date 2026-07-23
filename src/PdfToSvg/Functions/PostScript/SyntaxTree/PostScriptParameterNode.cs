// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Functions.PostScript.SyntaxTree
{
    internal class PostScriptParameterNode(int index) : PostScriptNode
    {
        public override string DebugView => "$" + index;

        public override PostScriptValueType Type => PostScriptValueType.Number;
        public override bool PotentiallyInfinite => false;

        public override PostScriptNode Simplify(PostScriptSyntaxBuilderContext context)
        {
            return this;
        }

        public override void MarkReferenced()
        {
            References++;
        }

        public override bool TryCompile(PostScriptCompilationContext context, out short register)
        {
            Register = register = (short)index; // Injected at first registers at startup
            return true;
        }
    }
}
