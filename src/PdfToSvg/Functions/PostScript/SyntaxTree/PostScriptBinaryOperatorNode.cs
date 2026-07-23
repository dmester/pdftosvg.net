// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;

namespace PdfToSvg.Functions.PostScript.SyntaxTree
{
    internal class PostScriptBinaryOperatorNode(
        PostScriptNode left,
        PostScriptNode right,
        PostScriptOpCode op) : PostScriptNode
    {
        public override string DebugView => "(" + left.DebugView + ") (" + right.DebugView + ") " + op;

        public override PostScriptValueType Type =>
            op == PostScriptOpCode.Eq ||
            op == PostScriptOpCode.Ne ||
            op == PostScriptOpCode.Gt ||
            op == PostScriptOpCode.Ge ||
            op == PostScriptOpCode.Lt ||
            op == PostScriptOpCode.Le ||
            op == PostScriptOpCode.LogicalAnd ||
            op == PostScriptOpCode.LogicalOr ||
            op == PostScriptOpCode.LogicalXor
                ? PostScriptValueType.Boolean
                : PostScriptValueType.Number;

        public override bool PotentiallyInfinite { get; } =
            left.PotentiallyInfinite ||
            right.PotentiallyInfinite ||
            PostScriptOperators.PotentiallyInfinite(op);

        public PostScriptOpCode Op => op;

        public PostScriptNode Left => left;
        public PostScriptNode Right => right;

        public override void MarkReferenced()
        {
            References++;

            // Since this node caches the result from children, we only need to walk them on the first reference
            if (References == 1)
            {
                left.MarkReferenced();
                right.MarkReferenced();
            }
        }

        public override PostScriptNode Simplify(PostScriptSyntaxBuilderContext context)
        {
            var constLeft = left as PostScriptConstantNode;
            var constRight = right as PostScriptConstantNode;

            if (constLeft != null &&
                constRight != null &&
                constLeft.Type == constRight.Type)
            {
                var result = PostScriptOperators.Execute(op, constLeft.Value, constRight.Value);
                return Type == PostScriptValueType.Boolean
                    ? context.Intern(result != 0d)
                    : context.Intern(result);
            }

            if (op == PostScriptOpCode.Add || op == PostScriptOpCode.Sub)
            {
                return SimplifyAddSub(context);
            }

            if (op == PostScriptOpCode.Mul)
            {
                return SimplifyMul(context);
            }

            if (op == PostScriptOpCode.Div)
            {
                if (constRight != null && MathUtils.IsNormalPowerOfTwo(constRight.Value))
                {
                    return new PostScriptBinaryOperatorNode(
                        left, context.Intern(1f / constRight.Value),
                        PostScriptOpCode.Mul
                    ).SimplifyMul(context);
                }
                else
                {
                    return this;
                }
            }

            return this;
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
                var sourceRegister1 = (short)-1;
                var sourceRegister2 = (short)-1;
                var immediate = 0d;

                if (Right is PostScriptConstantNode constRight)
                {
                    if (!Left.TryCompile(context, out sourceRegister1))
                    {
                        register = -1;
                        return false;
                    }

                    immediate = constRight.Value;
                }
                else if (Left is PostScriptConstantNode constLeft)
                {
                    if (!Right.TryCompile(context, out sourceRegister2))
                    {
                        register = -1;
                        return false;
                    }

                    immediate = constLeft.Value;
                }
                else
                {
                    if (!Left.TryCompile(context, out sourceRegister1) ||
                        !Right.TryCompile(context, out sourceRegister2))
                    {
                        register = -1;
                        return false;
                    }
                }

                Left.Consume();
                Right.Consume();

                Register = context.AddToRegister(this);

                context.Instructions.Add(
                    new PostScriptCompiledInstruction(op, Register, sourceRegister1, sourceRegister2, immediate));
            }

            register = Register;
            return true;
        }

        private static bool CanBeCompiled(PostScriptOpCode opCode)
        {
            switch (opCode)
            {
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
                case PostScriptOpCode.LogicalAnd:
                case PostScriptOpCode.BitwiseAnd:
                case PostScriptOpCode.LogicalOr:
                case PostScriptOpCode.BitwiseOr:
                case PostScriptOpCode.LogicalXor:
                case PostScriptOpCode.BitwiseXor:
                case PostScriptOpCode.BitShift:
                    return true;

                default:
                    return false;
            }
        }

        private PostScriptNode SimplifyAddSub(PostScriptSyntaxBuilderContext context)
        {
            var canBeSimplified = true;
            var constValues = 0d;
            PostScriptNode? variablePositiveTerm1 = null;
            PostScriptNode? variablePositiveTerm2 = null;
            PostScriptNode? variableNegativeTerm = null;

            void RegisterVariableNode(PostScriptNode node, bool negative)
            {
                if (negative)
                {
                    if (variableNegativeTerm == null)
                    {
                        variableNegativeTerm = node;
                    }
                    else
                    {
                        canBeSimplified = false;
                    }
                }
                else
                {
                    if (variablePositiveTerm1 == null)
                    {
                        variablePositiveTerm1 = node;
                    }
                    else if (variablePositiveTerm2 == null)
                    {
                        variablePositiveTerm2 = node;
                    }
                    else
                    {
                        canBeSimplified = false;
                    }
                }
            }

            void HandleNode(PostScriptNode node, bool negative, int level)
            {
                if (node is PostScriptConstantNode constNode)
                {
                    constValues += negative ? -constNode.Value : constNode.Value;
                }
                else if (node is PostScriptBinaryOperatorNode binaryNode && level < 2)
                {
                    if (binaryNode.Op == PostScriptOpCode.Add)
                    {
                        HandleNode(binaryNode.Left, negative, level + 1);
                        HandleNode(binaryNode.Right, negative, level + 1);
                    }
                    else if (binaryNode.Op == PostScriptOpCode.Sub)
                    {
                        HandleNode(binaryNode.Left, negative, level + 1);
                        HandleNode(binaryNode.Right, !negative, level + 1);
                    }
                    else
                    {
                        RegisterVariableNode(node, negative);
                    }
                }
                else
                {
                    RegisterVariableNode(node, negative);
                }
            }

            HandleNode(this, negative: false, level: 0);

            if (canBeSimplified)
            {
                if (variableNegativeTerm != null)
                {
                    // We have at least one negative term
                    if (variablePositiveTerm1 == null)
                    {
                        if (constValues == 0)
                        {
                            return new PostScriptUnaryOperatorNode(variableNegativeTerm, PostScriptOpCode.Neg);
                        }
                        else
                        {
                            return new PostScriptBinaryOperatorNode(
                                context.Intern(constValues),
                                variableNegativeTerm,
                                PostScriptOpCode.Sub);
                        }
                    }

                    if (variablePositiveTerm2 == null)
                    {
                        if (constValues == 0)
                        {
                            return new PostScriptBinaryOperatorNode(
                                variablePositiveTerm1,
                                variableNegativeTerm,
                                PostScriptOpCode.Sub);
                        }
                        else
                        {
                            return this;
                        }
                    }

                    return this;
                }
                else
                {
                    // We have no negative term

                    if (variablePositiveTerm1 == null)
                    {
                        return context.Intern(constValues);
                    }

                    if (variablePositiveTerm2 == null)
                    {
                        if (constValues == 0)
                        {
                            return variablePositiveTerm1;
                        }
                        else
                        {
                            return new PostScriptBinaryOperatorNode(
                                variablePositiveTerm1,
                                context.Intern(constValues),
                                PostScriptOpCode.Add);
                        }
                    }

                    if (constValues == 0)
                    {
                        return new PostScriptBinaryOperatorNode(
                            variablePositiveTerm1,
                            variablePositiveTerm2,
                            PostScriptOpCode.Add);
                    }
                    else
                    {
                        return new PostScriptBinaryOperatorNode(
                            new PostScriptBinaryOperatorNode(
                                variablePositiveTerm1,
                                variablePositiveTerm2,
                                PostScriptOpCode.Add),
                            context.Intern(constValues),
                            PostScriptOpCode.Add);
                    }
                }
            }

            return this;
        }

        private PostScriptNode SimplifyMul(PostScriptSyntaxBuilderContext context)
        {
            var canBeSimplified = true;
            var constFactor = 1d;
            PostScriptNode? variableFactor1 = null;
            PostScriptNode? variableFactor2 = null;

            void HandleNode(PostScriptNode node, int level)
            {
                if (node is PostScriptConstantNode constNode)
                {
                    constFactor *= constNode.Value;
                }
                else if (level < 2 &&
                    node is PostScriptBinaryOperatorNode binaryNode &&
                    binaryNode.Op == PostScriptOpCode.Mul)
                {
                    HandleNode(binaryNode.Left, level + 1);
                    HandleNode(binaryNode.Right, level + 1);
                }
                else
                {
                    if (variableFactor1 == null)
                    {
                        variableFactor1 = node;
                    }
                    else if (variableFactor2 == null)
                    {
                        variableFactor2 = node;
                    }
                    else
                    {
                        canBeSimplified = false;
                    }
                }
            }

            HandleNode(left, level: 1);
            HandleNode(right, level: 1);

            if (constFactor == 0 && !left.PotentiallyInfinite && !right.PotentiallyInfinite)
            {
                // A zero factor nullifies the entire operation, even if the rest of the expression cannot be
                // simplified
                return context.Intern(0d);
            }
            else if (canBeSimplified)
            {
                if (variableFactor1 == null)
                {
                    return context.Intern(constFactor);
                }
                else if (variableFactor2 == null)
                {
                    if (constFactor == 1d)
                    {
                        return variableFactor1;
                    }
                    else
                    {
                        return new PostScriptBinaryOperatorNode(
                            variableFactor1, context.Intern(constFactor),
                            PostScriptOpCode.Mul);
                    }
                }
                else
                {
                    if (constFactor == 1d)
                    {
                        return new PostScriptBinaryOperatorNode(
                            variableFactor1, variableFactor2,
                            PostScriptOpCode.Mul);
                    }
                    else
                    {
                        return new PostScriptBinaryOperatorNode(
                            new PostScriptBinaryOperatorNode(
                                variableFactor1, variableFactor2,
                                PostScriptOpCode.Mul),
                            context.Intern(constFactor),
                            PostScriptOpCode.Mul);
                    }
                }
            }

            return this;
        }
    }
}
