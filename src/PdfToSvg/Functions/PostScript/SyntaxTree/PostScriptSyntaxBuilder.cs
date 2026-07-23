// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System.Collections.Generic;

namespace PdfToSvg.Functions.PostScript.SyntaxTree
{
    internal static class PostScriptSyntaxBuilder
    {
        public static bool TryBuild(
            PostScriptExpression expression,
            int parameterCount,
            int outputCount,
            out PostScriptSyntaxTree syntaxTree)
        {
            syntaxTree = null!;

            if (parameterCount < 0 || parameterCount > PostScriptLimits.StackSize || outputCount < 0)
            {
                return false;
            }

            var context = new PostScriptSyntaxBuilderContext();
            var parameters = new PostScriptParameterNode[parameterCount];
            var stack = new List<PostScriptNode>();

            for (var i = 0; i < parameterCount; i++)
            {
                var parameter = new PostScriptParameterNode(i);
                parameters[i] = parameter;
                stack.Add(parameter);
            }

            if (!TryApply(expression, context, stack) || stack.Count < outputCount)
            {
                return false;
            }

            var outputs = stack.GetRange(stack.Count - outputCount, outputCount).ToArray();

            for (var i = 0; i < outputs.Length; i++)
            {
                outputs[i].MarkReferenced();
            }

            syntaxTree = new PostScriptSyntaxTree(parameters, outputs);
            return true;
        }

        private static bool TryApply(
            PostScriptExpression expression,
            PostScriptSyntaxBuilderContext context,
            List<PostScriptNode> stack)
        {
            var instructions = expression.Instructions;

            for (var i = 0; i < instructions.Length; i++)
            {
                if (--context.RemainingInstructions < 0)
                {
                    return false;
                }

                ref readonly var instruction = ref instructions[i];

                switch (instruction.OpCode)
                {
                    case PostScriptOpCode.PushValue:
                        if (stack.Count >= PostScriptLimits.StackSize ||
                            !TryPushValue(expression, instruction.Operand, context, stack))
                        {
                            return false;
                        }
                        break;

                    case PostScriptOpCode.Add:
                    case PostScriptOpCode.Sub:
                    case PostScriptOpCode.Mul:
                    case PostScriptOpCode.Div:
                    case PostScriptOpCode.Idiv:
                    case PostScriptOpCode.Mod:
                    case PostScriptOpCode.Atan:
                    case PostScriptOpCode.Exp:
                    case PostScriptOpCode.Eq:
                    case PostScriptOpCode.Ne:
                    case PostScriptOpCode.Gt:
                    case PostScriptOpCode.Ge:
                    case PostScriptOpCode.Lt:
                    case PostScriptOpCode.Le:
                    case PostScriptOpCode.And:
                    case PostScriptOpCode.Or:
                    case PostScriptOpCode.Xor:
                    case PostScriptOpCode.BitShift:
                        if (!TryApplyBinaryOperator(instruction.OpCode, context, stack))
                        {
                            return false;
                        }
                        break;

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
                    case PostScriptOpCode.Not:
                        if (!TryApplyUnaryOperator(instruction.OpCode, context, stack))
                        {
                            return false;
                        }
                        break;

                    case PostScriptOpCode.True:
                    case PostScriptOpCode.False:
                        if (stack.Count >= PostScriptLimits.StackSize)
                        {
                            return false;
                        }

                        stack.Add(context.Intern(instruction.OpCode == PostScriptOpCode.True));
                        break;

                    case PostScriptOpCode.If:
                        if (!TryApplyIf(context, stack))
                        {
                            return false;
                        }
                        break;

                    case PostScriptOpCode.IfElse:
                        if (!TryApplyIfElse(context, stack))
                        {
                            return false;
                        }
                        break;

                    case PostScriptOpCode.Pop:
                        if (stack.Count < 1)
                        {
                            return false;
                        }

                        stack.RemoveAt(stack.Count - 1);
                        break;

                    case PostScriptOpCode.Dup:
                        if (stack.Count < 1 || stack.Count >= PostScriptLimits.StackSize)
                        {
                            return false;
                        }

                        var duplicatedNode = stack[stack.Count - 1];
                        stack.Add(duplicatedNode);
                        break;

                    case PostScriptOpCode.Exch:
                        if (stack.Count < 2)
                        {
                            return false;
                        }

                        var exchangedNode = stack[stack.Count - 2];
                        stack[stack.Count - 2] = stack[stack.Count - 1];
                        stack[stack.Count - 1] = exchangedNode;
                        break;

                    case PostScriptOpCode.Roll:
                        if (!TryApplyRoll(stack))
                        {
                            return false;
                        }
                        break;

                    case PostScriptOpCode.Index:
                        if (!TryApplyIndex(stack))
                        {
                            return false;
                        }
                        break;

                    case PostScriptOpCode.Copy:
                        if (!TryApplyCopy(stack))
                        {
                            return false;
                        }
                        break;

                    default:
                        return false;
                }
            }

            return true;
        }

        private static bool TryPushValue(
            PostScriptExpression expression,
            in PostScriptValue value,
            PostScriptSyntaxBuilderContext context,
            List<PostScriptNode> stack)
        {
            switch (value.Type)
            {
                case PostScriptValueType.Number:
                    stack.Add(context.Intern(value.Number));
                    return true;

                case PostScriptValueType.Boolean:
                    stack.Add(context.Intern(value.Number != 0d));
                    return true;

                case PostScriptValueType.Expression:
                    var blockIndex = (int)value.Number;
                    if (blockIndex < 0 || blockIndex >= expression.Blocks.Count)
                    {
                        return false;
                    }

                    stack.Add(new PostScriptExpressionNode(expression.Blocks[blockIndex]));
                    return true;

                default:
                    return false;
            }
        }

        private static bool TryApplyBinaryOperator(
            PostScriptOpCode op,
            PostScriptSyntaxBuilderContext context,
            List<PostScriptNode> stack)
        {
            if (stack.Count < 2)
            {
                return false;
            }

            var left = stack[stack.Count - 2];
            var right = stack[stack.Count - 1];

            if (!TryResolveBinaryOperator(op, left.Type, right.Type, out var resolvedOp))
            {
                return false;
            }

            var binary = new PostScriptBinaryOperatorNode(left, right, resolvedOp);
            stack[stack.Count - 2] = binary.Simplify(context);
            stack.RemoveAt(stack.Count - 1);
            return true;
        }

        private static bool TryResolveBinaryOperator(
            PostScriptOpCode op,
            PostScriptValueType leftType,
            PostScriptValueType rightType,
            out PostScriptOpCode resolvedOp)
        {
            resolvedOp = op;

            if (op == PostScriptOpCode.And || op == PostScriptOpCode.Or || op == PostScriptOpCode.Xor)
            {
                if (leftType == PostScriptValueType.Boolean && rightType == PostScriptValueType.Boolean)
                {
                    resolvedOp = op == PostScriptOpCode.And ? PostScriptOpCode.LogicalAnd :
                        op == PostScriptOpCode.Or ? PostScriptOpCode.LogicalOr :
                        PostScriptOpCode.LogicalXor;
                    return true;
                }

                if (leftType == PostScriptValueType.Number && rightType == PostScriptValueType.Number)
                {
                    resolvedOp = op == PostScriptOpCode.And ? PostScriptOpCode.BitwiseAnd :
                        op == PostScriptOpCode.Or ? PostScriptOpCode.BitwiseOr :
                        PostScriptOpCode.BitwiseXor;
                    return true;
                }

                return false;
            }

            if (op == PostScriptOpCode.Eq || op == PostScriptOpCode.Ne)
            {
                return leftType == rightType && leftType != PostScriptValueType.Expression;
            }

            return leftType == PostScriptValueType.Number && rightType == PostScriptValueType.Number;
        }

        private static bool TryApplyUnaryOperator(
            PostScriptOpCode op,
            PostScriptSyntaxBuilderContext context,
            List<PostScriptNode> stack)
        {
            if (stack.Count < 1)
            {
                return false;
            }

            var input = stack[stack.Count - 1];
            var resolvedOp = op;

            if (op == PostScriptOpCode.Not)
            {
                resolvedOp = input.Type == PostScriptValueType.Boolean ? PostScriptOpCode.LogicalNot :
                    input.Type == PostScriptValueType.Number ? PostScriptOpCode.BitwiseNot :
                    PostScriptOpCode.Not;
            }

            if (resolvedOp == PostScriptOpCode.Not ||
                resolvedOp != PostScriptOpCode.LogicalNot && input.Type != PostScriptValueType.Number)
            {
                return false;
            }

            var unary = new PostScriptUnaryOperatorNode(input, resolvedOp);
            stack[stack.Count - 1] = unary.Simplify(context);
            return true;
        }

        private static bool TryApplyIf(
            PostScriptSyntaxBuilderContext context,
            List<PostScriptNode> stack)
        {
            if (stack.Count < 2 ||
                stack[stack.Count - 2] is not PostScriptConstantNode condition ||
                condition.Type == PostScriptValueType.Expression ||
                stack[stack.Count - 1] is not PostScriptExpressionNode expression)
            {
                return false;
            }

            stack.RemoveRange(stack.Count - 2, 2);
            return condition.Value == 0d || TryApply(expression.Expression, context, stack);
        }

        private static bool TryApplyIfElse(
            PostScriptSyntaxBuilderContext context,
            List<PostScriptNode> stack)
        {
            if (stack.Count < 3 ||
                stack[stack.Count - 3] is not PostScriptConstantNode condition ||
                condition.Type == PostScriptValueType.Expression ||
                stack[stack.Count - 2] is not PostScriptExpressionNode trueExpression ||
                stack[stack.Count - 1] is not PostScriptExpressionNode falseExpression)
            {
                return false;
            }

            stack.RemoveRange(stack.Count - 3, 3);
            var selectedExpression = condition.Value != 0d ? trueExpression.Expression : falseExpression.Expression;
            return TryApply(selectedExpression, context, stack);
        }

        private static bool TryApplyRoll(List<PostScriptNode> stack)
        {
            if (stack.Count < 2 ||
                stack[stack.Count - 2] is not PostScriptConstantNode windowSizeNode ||
                windowSizeNode.Type != PostScriptValueType.Number ||
                stack[stack.Count - 1] is not PostScriptConstantNode shiftAmountNode ||
                shiftAmountNode.Type != PostScriptValueType.Number)
            {
                return false;
            }

            var windowSize = (int)windowSizeNode.Value;
            var shiftAmount = (int)shiftAmountNode.Value;
            if (windowSize < 0 || windowSize > stack.Count - 2)
            {
                return false;
            }

            stack.RemoveRange(stack.Count - 2, 2);
            stack.RollEnd(windowSize, shiftAmount);
            return true;
        }

        private static bool TryApplyIndex(List<PostScriptNode> stack)
        {
            if (stack.Count < 1 ||
                stack[stack.Count - 1] is not PostScriptConstantNode indexNode ||
                indexNode.Type != PostScriptValueType.Number)
            {
                return false;
            }

            var index = (int)indexNode.Value + 1;
            if (index < 1 || index >= stack.Count)
            {
                return false;
            }

            var indexedNode = stack[stack.Count - index - 1];
            stack[stack.Count - 1] = indexedNode;
            return true;
        }

        private static bool TryApplyCopy(List<PostScriptNode> stack)
        {
            if (stack.Count < 1 ||
                stack[stack.Count - 1] is not PostScriptConstantNode copyCountNode ||
                copyCountNode.Type != PostScriptValueType.Number)
            {
                return false;
            }

            stack.RemoveAt(stack.Count - 1);

            var copyCount = (int)copyCountNode.Value;
            if (copyCount < 0 || copyCount > stack.Count || copyCount > PostScriptLimits.StackSize - stack.Count)
            {
                return false;
            }

            var endIndex = stack.Count;
            for (var copyCursor = stack.Count - copyCount; copyCursor < endIndex; copyCursor++)
            {
                var copiedNode = stack[copyCursor];
                stack.Add(copiedNode);
            }

            return true;
        }
    }
}
