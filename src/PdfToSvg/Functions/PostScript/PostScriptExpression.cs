// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System.Collections.Generic;

namespace PdfToSvg.Functions.PostScript
{
    internal class PostScriptExpression
    {
        private readonly PostScriptInstruction[] instructions;

        public PostScriptExpression(IList<PostScriptInstruction> instructions, IList<PostScriptExpression> blocks)
        {
            this.instructions = new PostScriptInstruction[instructions.Count];
            instructions.CopyTo(this.instructions, 0);
            Blocks = blocks;
        }

        public IList<PostScriptExpression> Blocks { get; }

        public PostScriptInstruction[] Instructions => instructions;

        public void Execute(ref PostScriptStack stack)
        {
            var remainingInstructions = PostScriptLimits.MaxInterpretedInstructions;
            Execute(ref stack, 0, ref remainingInstructions);
        }

        private void Execute(ref PostScriptStack stack, int depth, ref int remainingInstructions)
        {
            if (depth >= PostScriptLimits.MaxExecutionDepth)
            {
                throw new PostScriptFunctionException("PostScript execution depth limit exceeded");
            }

            var instructions = this.instructions;

            double num1, num2;
            int int1, int2;
            bool bool1, bool2;
            PostScriptValue any1, any2;

            for (var i = 0; i < instructions.Length; i++)
            {
                if (--remainingInstructions < 0)
                {
                    throw new PostScriptFunctionException("PostScript instruction limit exceeded");
                }

                ref readonly var instruction = ref instructions[i];

                switch (instruction.OpCode)
                {
                    case PostScriptOpCode.PushValue:
                        stack.Push(instruction.Operand);
                        break;

                    case PostScriptOpCode.Add:
                        stack.Pop(out num1, out num2);
                        stack.Push(PostScriptOperators.Add(num1, num2));
                        break;

                    case PostScriptOpCode.Sub:
                        stack.Pop(out num1, out num2);
                        stack.Push(PostScriptOperators.Sub(num1, num2));
                        break;

                    case PostScriptOpCode.Mul:
                        stack.Pop(out num1, out num2);
                        stack.Push(PostScriptOperators.Mul(num1, num2));
                        break;

                    case PostScriptOpCode.Div:
                        stack.Pop(out num1, out num2);
                        stack.Push(PostScriptOperators.Div(num1, num2));
                        break;

                    case PostScriptOpCode.Idiv:
                        stack.Pop(out int1, out int2);
                        stack.Push(PostScriptOperators.Idiv(int1, int2));
                        break;

                    case PostScriptOpCode.Mod:
                        stack.Pop(out int1, out int2);
                        stack.Push(PostScriptOperators.Mod(int1, int2));
                        break;

                    case PostScriptOpCode.Neg:
                        stack.Pop(out num1);
                        stack.Push(PostScriptOperators.Neg(num1));
                        break;

                    case PostScriptOpCode.Abs:
                        stack.Pop(out num1);
                        stack.Push(PostScriptOperators.Abs(num1));
                        break;

                    case PostScriptOpCode.Ceiling:
                        stack.Pop(out num1);
                        stack.Push(PostScriptOperators.Ceiling(num1));
                        break;

                    case PostScriptOpCode.Floor:
                        stack.Pop(out num1);
                        stack.Push(PostScriptOperators.Floor(num1));
                        break;

                    case PostScriptOpCode.Round:
                        stack.Pop(out num1);
                        stack.Push(PostScriptOperators.Round(num1));
                        break;

                    case PostScriptOpCode.Truncate:
                        stack.Pop(out num1);
                        stack.Push(PostScriptOperators.Truncate(num1));
                        break;

                    case PostScriptOpCode.Sqrt:
                        stack.Pop(out num1);
                        stack.Push(PostScriptOperators.Sqrt(num1));
                        break;

                    case PostScriptOpCode.Sin:
                        stack.Pop(out num1);
                        stack.Push(PostScriptOperators.Sin(num1));
                        break;

                    case PostScriptOpCode.Cos:
                        stack.Pop(out num1);
                        stack.Push(PostScriptOperators.Cos(num1));
                        break;

                    case PostScriptOpCode.Atan:
                        stack.Pop(out num1, out num2);
                        stack.Push(PostScriptOperators.Atan(num1, num2));
                        break;

                    case PostScriptOpCode.Exp:
                        stack.Pop(out num1, out num2);
                        stack.Push(PostScriptOperators.Exp(num1, num2));
                        break;

                    case PostScriptOpCode.Ln:
                        stack.Pop(out num1);
                        stack.Push(PostScriptOperators.Ln(num1));
                        break;

                    case PostScriptOpCode.Log:
                        stack.Pop(out num1);
                        stack.Push(PostScriptOperators.Log(num1));
                        break;

                    case PostScriptOpCode.Cvi:
                        stack.Pop(out num1);
                        stack.Push(PostScriptOperators.Cvi(num1));
                        break;

                    case PostScriptOpCode.Cvr:
                        stack.Pop(out num1);
                        stack.Push(PostScriptOperators.Cvr(num1));
                        break;

                    case PostScriptOpCode.Eq:
                        stack.Pop(out any1, out any2);
                        stack.Push(PostScriptOperators.Eq(in any1, in any2));
                        break;

                    case PostScriptOpCode.Ne:
                        stack.Pop(out any1, out any2);
                        stack.Push(PostScriptOperators.Ne(in any1, in any2));
                        break;

                    case PostScriptOpCode.Gt:
                        stack.Pop(out num1, out num2);
                        stack.Push(PostScriptOperators.Gt(num1, num2));
                        break;

                    case PostScriptOpCode.Ge:
                        stack.Pop(out num1, out num2);
                        stack.Push(PostScriptOperators.Ge(num1, num2));
                        break;

                    case PostScriptOpCode.Lt:
                        stack.Pop(out num1, out num2);
                        stack.Push(PostScriptOperators.Lt(num1, num2));
                        break;

                    case PostScriptOpCode.Le:
                        stack.Pop(out num1, out num2);
                        stack.Push(PostScriptOperators.Le(num1, num2));
                        break;

                    case PostScriptOpCode.And:
                        if (stack.Peek().Type == PostScriptValueType.Boolean)
                        {
                            stack.Pop(out bool1, out bool2);
                            stack.Push(PostScriptOperators.LogicalAnd(bool1 ? 1d : 0d, bool2 ? 1d : 0d) != 0d);
                        }
                        else
                        {
                            stack.Pop(out int1, out int2);
                            stack.Push(PostScriptOperators.BitwiseAnd(int1, int2));
                        }
                        break;

                    case PostScriptOpCode.Or:
                        if (stack.Peek().Type == PostScriptValueType.Boolean)
                        {
                            stack.Pop(out bool1, out bool2);
                            stack.Push(PostScriptOperators.LogicalOr(bool1 ? 1d : 0d, bool2 ? 1d : 0d) != 0d);
                        }
                        else
                        {
                            stack.Pop(out int1, out int2);
                            stack.Push(PostScriptOperators.BitwiseOr(int1, int2));
                        }
                        break;

                    case PostScriptOpCode.Xor:
                        if (stack.Peek().Type == PostScriptValueType.Boolean)
                        {
                            stack.Pop(out bool1, out bool2);
                            stack.Push(PostScriptOperators.LogicalXor(bool1 ? 1d : 0d, bool2 ? 1d : 0d) != 0d);
                        }
                        else
                        {
                            stack.Pop(out int1, out int2);
                            stack.Push(PostScriptOperators.BitwiseXor(int1, int2));
                        }
                        break;

                    case PostScriptOpCode.Not:
                        if (stack.Peek().Type == PostScriptValueType.Boolean)
                        {
                            stack.Pop(out bool1);
                            stack.Push(PostScriptOperators.LogicalNot(bool1 ? 1d : 0d) != 0d);
                        }
                        else
                        {
                            stack.Pop(out int1);
                            stack.Push(PostScriptOperators.BitwiseNot(int1));
                        }
                        break;

                    case PostScriptOpCode.BitShift:
                        stack.Pop(out int1, out int2);
                        stack.Push(PostScriptOperators.BitShift(int1, int2));
                        break;

                    case PostScriptOpCode.True:
                        stack.Push(PostScriptOperators.True());
                        break;

                    case PostScriptOpCode.False:
                        stack.Push(PostScriptOperators.False());
                        break;

                    case PostScriptOpCode.If:
                        stack.PopBlock1(out bool1, out int1);
                        if (bool1)
                        {
                            Blocks[int1].Execute(ref stack, depth + 1, ref remainingInstructions);
                        }
                        break;

                    case PostScriptOpCode.IfElse:
                        stack.PopBlock2(out bool1, out int1, out int2);
                        Blocks[bool1 ? int1 : int2].Execute(ref stack, depth + 1, ref remainingInstructions);
                        break;

                    case PostScriptOpCode.Pop:
                        stack.Pop();
                        break;

                    case PostScriptOpCode.Exch:
                        stack.Exchange();
                        break;

                    case PostScriptOpCode.Dup:
                        stack.Push(stack.Peek());
                        break;

                    case PostScriptOpCode.Copy:
                        stack.Pop(out int1);
                        stack.Copy(int1);
                        break;

                    case PostScriptOpCode.Index:
                        stack.Pop(out int1);
                        stack.Push(stack.Get(int1));
                        break;

                    case PostScriptOpCode.Roll:
                        stack.Pop(out int1, out int2);
                        stack.Roll(int1, int2);
                        break;
                }
            }
        }
    }
}
