// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;

namespace PdfToSvg.Functions.PostScript.SyntaxTree
{
    internal class PostScriptUnaryOperatorNode(PostScriptNode input, PostScriptOpCode op) : PostScriptNode
    {
        public PostScriptNode Input => input;

        public PostScriptOpCode Op => op;

        public override string DebugView => "(" + input.DebugView + ") " + op;

        public override PostScriptValueType Type => op == PostScriptOpCode.LogicalNot
            ? PostScriptValueType.Boolean
            : PostScriptValueType.Number;

        public override bool PotentiallyInfinite { get; } =
            input.PotentiallyInfinite ||
            PostScriptOperators.PotentiallyInfinite(op);

        public override void MarkReferenced()
        {
            References++;

            // Since this node caches the result from children, we only need to walk them on the first reference
            if (References == 1)
            {
                input.MarkReferenced();
            }
        }

        public override bool TryCompile(PostScriptCompilationContext context, out short register)
        {
            if (!CanBeCompiled(op))
            {
                register = -1;
                return false;
            }

            if (Register < 0)
            {
                var sourceRegister = (short)-1;
                var immediate = 0d;

                if (Input is PostScriptConstantNode constInput)
                {
                    immediate = constInput.Value;
                }
                else
                {
                    if (!Input.TryCompile(context, out sourceRegister))
                    {
                        register = -1;
                        return false;
                    }
                }

                input.Consume();

                Register = context.AddToRegister(this);

                context.Instructions.Add(
                    new PostScriptCompiledInstruction(op, Register, sourceRegister, -1, immediate));
            }

            register = Register;
            return true;
        }

        public override PostScriptNode Simplify(PostScriptSyntaxBuilderContext context)
        {
            if (input is PostScriptConstantNode constantNode)
            {
                var result = PostScriptOperators.Execute(op, constantNode.Value, 0d);
                return Type == PostScriptValueType.Boolean
                    ? context.Intern(result != 0d)
                    : context.Intern(result);
            }
            else if (op == PostScriptOpCode.Neg)
            {
                if (input is PostScriptUnaryOperatorNode unaryNode && unaryNode.Op == PostScriptOpCode.Neg)
                {
                    return unaryNode.Input;
                }
            }
            else if (op != PostScriptOpCode.Cvi && OpResultIsInteger(op)) 
            {
                // cvi saturates values, so we cannot optimize it away

                if (input is PostScriptUnaryOperatorNode unaryNode && OpResultIsInteger(unaryNode.Op))
                {
                    // The float already represents an integer
                    return input;
                }
            }

            return this;
        }

        private static bool OpResultIsInteger(PostScriptOpCode op)
        {
            return
                op == PostScriptOpCode.Cvi ||
                op == PostScriptOpCode.Truncate ||
                op == PostScriptOpCode.Round ||
                op == PostScriptOpCode.Ceiling ||
                op == PostScriptOpCode.Floor ||
                op == PostScriptOpCode.Round;
        }

        private static bool CanBeCompiled(PostScriptOpCode opCode)
        {
            switch (opCode)
            {
                case PostScriptOpCode.Cvi:
                case PostScriptOpCode.Cvr:
                case PostScriptOpCode.Neg:
                case PostScriptOpCode.Abs:
                case PostScriptOpCode.Round:
                case PostScriptOpCode.Truncate:
                case PostScriptOpCode.Sqrt:
                case PostScriptOpCode.Sin:
                case PostScriptOpCode.Cos:
                case PostScriptOpCode.Ceiling:
                case PostScriptOpCode.Floor:
                case PostScriptOpCode.Ln:
                case PostScriptOpCode.Log:
                case PostScriptOpCode.LogicalNot:
                case PostScriptOpCode.BitwiseNot:
                    return true;

                default:
                    return false;
            }
        }
    }
}
