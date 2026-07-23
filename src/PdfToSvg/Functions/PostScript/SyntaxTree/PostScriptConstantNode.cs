// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System.Globalization;

namespace PdfToSvg.Functions.PostScript.SyntaxTree
{
    internal class PostScriptConstantNode : PostScriptNode
    {
        public readonly double Value;

        public override PostScriptValueType Type { get; }

#if NET5_0_OR_GREATER
        public override bool PotentiallyInfinite => !double.IsFinite(Value);
#else
        public override bool PotentiallyInfinite => double.IsNaN(Value) || double.IsInfinity(Value);
#endif

        public override string DebugView => Type == PostScriptValueType.Boolean
            ? Value == 0d ? "false" : "true"
            : Value.ToString("0.##", CultureInfo.InvariantCulture);

        public PostScriptConstantNode(double value)
        {
            Value = value;
            Type = PostScriptValueType.Number;
        }

        public PostScriptConstantNode(bool value)
        {
            Value = value ? 1d : 0d;
            Type = PostScriptValueType.Boolean;
        }

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
            if (Register < 0)
            {
                Register = context.AddToRegister(this);

                context.Instructions.Add(
                    new PostScriptCompiledInstruction(PostScriptOpCode.PushValue, Register, -1, -1, Value));
            }

            register = Register;
            return true;
        }
    }
}
