// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Parser = PdfToSvg.Fonts.CharStrings.Type2CharStringParser;

namespace PdfToSvg.Fonts.CharStrings
{
    internal static class CharStringOperators
    {
        [AttributeUsage(AttributeTargets.Method)]
        private class OperatorAttribute : Attribute
        {
            public OperatorAttribute(CharStringOpCode code, bool clearStack = false, bool subrOperator = false)
            {
                Code = code;
                ClearStack = clearStack;
                SubrOperator = subrOperator;
            }

            /// <summary>
            /// Operator code.
            /// </summary>
            public CharStringOpCode Code { get; }

            /// <summary>
            /// Specifies whether the operator is clearing the stack. Used for detecting the leading advance width in char
            /// strings.
            /// </summary>
            public bool ClearStack { get; }

            /// <summary>
            /// Specifies whether this operator invokes or returning from a subroutine. Such operators don't affect
            /// <see cref="Parser.LastOperator"/>.
            /// </summary>
            public bool SubrOperator { get; }
        }

        private static readonly Dictionary<CharStringOpCode, CharStringOperator> operators;

        static CharStringOperators()
        {
            operators = typeof(CharStringOperators)
                .GetTypeInfo()
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod)
                .Select(method => new
                {
                    Method = method,
                    Parameters = method.GetParameters(),
                    Operator = method.GetCustomAttributes(typeof(OperatorAttribute), false).Cast<OperatorAttribute>().FirstOrDefault(),
                })
                .Where(method =>
                    method.Operator != null &&
                    method.Parameters.Length == 1 &&
                    method.Parameters[0].ParameterType == typeof(Parser))
                .ToDictionary(
                    method => method.Operator.Code,
                    method => new CharStringOperator(
                        method.Method.CreateDelegate<Action<Parser>>(),
                        method.Operator.ClearStack,
                        method.Operator.SubrOperator));
        }

        public static bool TryGetOperator(CharStringOpCode code, out CharStringOperator result)
        {
            return operators.TryGetValue(code, out result);
        }

        #region Path construction operators

        [Operator(CharStringOpCode.RMoveTo, clearStack: true)]
        private static void RMoveTo(Parser parser)
        {
            parser.Stack.Pop(out double dx1, out double dy1);

            parser.Path.RMoveTo(dx1, dy1);
        }

        [Operator(CharStringOpCode.HMoveTo, clearStack: true)]
        private static void HMoveTo(Parser parser)
        {
            parser.Stack.Pop(out double dx1);

            parser.Path.RMoveTo(dx1, 0);
        }

        [Operator(CharStringOpCode.VMoveTo, clearStack: true)]
        private static void VMoveTo(Parser parser)
        {
            parser.Stack.Pop(out double dy1);

            parser.Path.RMoveTo(0, dy1);
        }

        [Operator(CharStringOpCode.RLineTo, clearStack: true)]
        private static void RLineTo(Parser parser)
        {
            var startAt = parser.Stack.Count % 2;

            for (var i = startAt; i < parser.Stack.Count; i += 2)
            {
                parser.Path.RLineTo(parser.Stack[i], parser.Stack[i + 1]);
            }

            parser.Stack.RemoveFrom(startAt);
        }

        private static void AlternatingLineTo(Parser parser, bool startHorizontally)
        {
            var horizontal = startHorizontally;

            for (var i = 0; i < parser.Stack.Count; i++)
            {
                if (horizontal)
                {
                    parser.Path.RLineTo(parser.Stack[i], 0);
                }
                else
                {
                    parser.Path.RLineTo(0, parser.Stack[i]);
                }

                horizontal = !horizontal;
            }

            parser.Stack.Clear();
        }

        [Operator(CharStringOpCode.HLineTo, clearStack: true)]
        private static void HLineTo(Parser parser)
        {
            AlternatingLineTo(parser, startHorizontally: true);
        }

        [Operator(CharStringOpCode.VLineTo, clearStack: true)]
        private static void VLineTo(Parser parser)
        {
            AlternatingLineTo(parser, startHorizontally: false);
        }

        [Operator(CharStringOpCode.RRCurveTo, clearStack: true)]
        private static void RRCurveTo(Parser parser)
        {
            var startAt = parser.Stack.Count % 6;

            for (var i = startAt; i < parser.Stack.Count; i += 6)
            {
                parser.Path.RRCurveTo(
                    parser.Stack[i + 0],
                    parser.Stack[i + 1],
                    parser.Stack[i + 2],
                    parser.Stack[i + 3],
                    parser.Stack[i + 4],
                    parser.Stack[i + 5]
                    );
            }

            parser.Stack.RemoveFrom(startAt);
        }

        [Operator(CharStringOpCode.HHCurveTo, clearStack: true)]
        private static void HHCurveTo(Parser parser)
        {
            var startAt = parser.Stack.Count % 4;
            int removeFrom;

            if (startAt > 0 && startAt + 3 < parser.Stack.Count)
            {
                removeFrom = startAt - 1;

                parser.Path.RRCurveTo(
                    parser.Stack[startAt + 0],
                    parser.Stack[startAt - 1],
                    parser.Stack[startAt + 1],
                    parser.Stack[startAt + 2],
                    parser.Stack[startAt + 3],
                    0);

                startAt += 4;
            }
            else
            {
                removeFrom = startAt;
            }

            for (var i = startAt; i < parser.Stack.Count; i += 4)
            {
                parser.Path.RRCurveTo(
                    parser.Stack[i + 0],
                    0,
                    parser.Stack[i + 1],
                    parser.Stack[i + 2],
                    parser.Stack[i + 3],
                    0);
            }

            parser.Stack.RemoveFrom(removeFrom);
        }

        private static void AlternatingHVCurveTo(Parser parser, bool startHorizontal)
        {
            if (parser.Stack.Count < 4)
            {
                throw new CharStringStackUnderflowException();
            }

            var startAt = parser.Stack.Count % 4;
            var endAt = parser.Stack.Count;
            var endOrthogonal = true;

            if (startAt > 0)
            {
                startAt--;
                endAt--;
                endOrthogonal = false;
            }

            for (var i = startAt; i < endAt; i += 4)
            {
                var lastd = 0d;

                if (!endOrthogonal && i + 4 == endAt)
                {
                    lastd = parser.Stack[i + 4];
                }

                if (startHorizontal)
                {
                    parser.Path.RRCurveTo(
                        parser.Stack[i + 0],
                        0,
                        parser.Stack[i + 1],
                        parser.Stack[i + 2],
                        lastd,
                        parser.Stack[i + 3]);

                    startHorizontal = false;
                }
                else
                {
                    parser.Path.RRCurveTo(
                        0,
                        parser.Stack[i + 0],
                        parser.Stack[i + 1],
                        parser.Stack[i + 2],
                        parser.Stack[i + 3],
                        lastd);

                    startHorizontal = true;
                }
            }

            parser.Stack.RemoveFrom(startAt);
        }

        [Operator(CharStringOpCode.HVCurveTo, clearStack: true)]
        private static void HVCurveTo(Parser parser)
        {
            AlternatingHVCurveTo(parser, startHorizontal: true);
        }

        [Operator(CharStringOpCode.RCurveLine, clearStack: true)]
        private static void RCurveLine(Parser parser)
        {
            if (parser.Stack.Count < 8)
            {
                throw new CharStringStackUnderflowException();
            }

            var startAt = (parser.Stack.Count - 2) % 6;

            for (var i = startAt; i + 2 < parser.Stack.Count; i += 6)
            {
                parser.Path.RRCurveTo(
                   parser.Stack[i + 0],
                   parser.Stack[i + 1],
                   parser.Stack[i + 2],
                   parser.Stack[i + 3],
                   parser.Stack[i + 4],
                   parser.Stack[i + 5]);
            }

            parser.Path.RLineTo(
                parser.Stack[parser.Stack.Count - 2],
                parser.Stack[parser.Stack.Count - 1]
                );

            parser.Stack.RemoveFrom(startAt);
        }

        [Operator(CharStringOpCode.RLineCurve, clearStack: true)]
        private static void RLineCurve(Parser parser)
        {
            if (parser.Stack.Count < 8)
            {
                throw new CharStringStackUnderflowException();
            }

            var startAt = parser.Stack.Count % 2;

            for (var i = startAt; i + 6 < parser.Stack.Count; i += 2)
            {
                parser.Path.RLineTo(
                   parser.Stack[i + 0],
                   parser.Stack[i + 1]);
            }

            parser.Path.RRCurveTo(
                parser.Stack[parser.Stack.Count - 6],
                parser.Stack[parser.Stack.Count - 5],
                parser.Stack[parser.Stack.Count - 4],
                parser.Stack[parser.Stack.Count - 3],
                parser.Stack[parser.Stack.Count - 2],
                parser.Stack[parser.Stack.Count - 1]);

            parser.Stack.RemoveFrom(startAt);
        }

        [Operator(CharStringOpCode.VHCurveTo, clearStack: true)]
        private static void VHCurveTo(Parser parser)
        {
            AlternatingHVCurveTo(parser, startHorizontal: false);
        }

        [Operator(CharStringOpCode.VVCurveTo, clearStack: true)]
        private static void VVCurveTo(Parser parser)
        {
            if (parser.Stack.Count < 4)
            {
                throw new CharStringStackUnderflowException();
            }

            var startAt = parser.Stack.Count % 4;
            var removeFrom = startAt;
            var dx1 = 0d;

            if (startAt > 0)
            {
                dx1 = parser.Stack[startAt - 1];
                removeFrom--;
            }

            for (var i = startAt; i < parser.Stack.Count; i += 4)
            {
                parser.Path.RRCurveTo(
                    dx1,
                    parser.Stack[i + 0],
                    parser.Stack[i + 1],
                    parser.Stack[i + 2],
                    0,
                    parser.Stack[i + 3]);

                dx1 = 0;
            }

            parser.Stack.RemoveFrom(removeFrom);
        }

        [Operator(CharStringOpCode.Flex, clearStack: true)]
        private static void Flex(Parser parser)
        {
            var startAt = parser.Stack.Count - 12;
            if (startAt < 0)
            {
                throw new CharStringStackUnderflowException();
            }

            parser.Path.RRCurveTo(
                parser.Stack[startAt + 0],
                parser.Stack[startAt + 1],
                parser.Stack[startAt + 2],
                parser.Stack[startAt + 3],
                parser.Stack[startAt + 4],
                parser.Stack[startAt + 5]
                );

            parser.Path.RRCurveTo(
                parser.Stack[startAt + 6],
                parser.Stack[startAt + 7],
                parser.Stack[startAt + 8],
                parser.Stack[startAt + 9],
                parser.Stack[startAt + 10],
                parser.Stack[startAt + 11]
                );

            parser.Stack.RemoveFrom(startAt);
        }

        [Operator(CharStringOpCode.HFlex, clearStack: true)]
        private static void HFlex(Parser parser)
        {
            var startAt = parser.Stack.Count - 7;
            if (startAt < 0)
            {
                throw new CharStringStackUnderflowException();
            }

            parser.Path.RRCurveTo(
                parser.Stack[startAt + 0],
                0,
                parser.Stack[startAt + 1],
                parser.Stack[startAt + 2],
                parser.Stack[startAt + 3],
                0);

            parser.Path.RRCurveTo(
                parser.Stack[startAt + 4],
                0,
                parser.Stack[startAt + 5],
                0,
                parser.Stack[startAt + 6],
                0);

            parser.Stack.RemoveFrom(startAt);
        }

        [Operator(CharStringOpCode.HFlex1, clearStack: true)]
        private static void HFlex1(Parser parser)
        {
            var startAt = parser.Stack.Count - 9;
            if (startAt < 0)
            {
                throw new CharStringStackUnderflowException();
            }

            parser.Path.RRCurveTo(
                parser.Stack[startAt + 0],
                parser.Stack[startAt + 1],
                parser.Stack[startAt + 2],
                parser.Stack[startAt + 3],
                parser.Stack[startAt + 4],
                0);

            parser.Path.RRCurveTo(
                parser.Stack[startAt + 5],
                0,
                parser.Stack[startAt + 6],
                parser.Stack[startAt + 7],
                parser.Stack[startAt + 8],
                0);

            parser.Stack.RemoveFrom(startAt);
        }

        [Operator(CharStringOpCode.Flex1, clearStack: true)]
        private static void Flex1(Parser parser)
        {
            var startAt = parser.Stack.Count - 11;
            if (startAt < 0)
            {
                throw new CharStringStackUnderflowException();
            }

            var dx1 = parser.Stack[startAt + 0];
            var dy1 = parser.Stack[startAt + 1];
            var dx2 = parser.Stack[startAt + 2];
            var dy2 = parser.Stack[startAt + 3];
            var dx3 = parser.Stack[startAt + 4];
            var dy3 = parser.Stack[startAt + 5];
            var dx4 = parser.Stack[startAt + 6];
            var dy4 = parser.Stack[startAt + 7];
            var dx5 = parser.Stack[startAt + 8];
            var dy5 = parser.Stack[startAt + 9];
            var d6 = parser.Stack[startAt + 10];

            var dx = dx1 + dx2 + dx3 + dx4 + dx5;
            var dy = dy1 + dy2 + dy3 + dy4 + dy5;

            double dx6, dy6;

            if (Math.Abs(dx) > Math.Abs(dy))
            {
                dx6 = d6;
                dy6 = 0;
            }
            else
            {
                dx6 = 0;
                dy6 = d6;
            }

            parser.Path.RRCurveTo(dx1, dy1, dx2, dy2, dx3, dy3);
            parser.Path.RRCurveTo(dx4, dy4, dx5, dy5, dx6, dy6);

            parser.Stack.RemoveFrom(startAt);
        }

        #endregion

        #region Operator for finishing a path

        [Operator(CharStringOpCode.EndChar, clearStack: true)]
        private static void EndChar(Parser parser)
        {
            parser.EndChar();
        }

        #endregion

        #region Hint operators

        private static void Hint(Parser parser)
        {
            // All hints use an even number of arguments
            var startAt = parser.Stack.Count % 2;

            parser.HintCount += parser.Stack.Count / 2;

            parser.Stack.RemoveFrom(startAt);

        }

        [Operator(CharStringOpCode.HStem, clearStack: true)]
        private static void HStem(Parser parser)
        {
            Hint(parser);
        }

        [Operator(CharStringOpCode.VStem, clearStack: true)]
        private static void VStem(Parser parser)
        {
            Hint(parser);
        }

        [Operator(CharStringOpCode.HStemHm, clearStack: true)]
        private static void HStemHm(Parser parser)
        {
            Hint(parser);
        }

        [Operator(CharStringOpCode.VStemHm, clearStack: true)]
        private static void VStemHm(Parser parser)
        {
            Hint(parser);
        }

        [Operator(CharStringOpCode.HintMask, clearStack: true)]
        private static void HintMask(Parser parser)
        {
            // vstem hint operator is optional if hstem and vstem direcly preceeds the hintmask operator.
            if (parser.LastOperator == HStem ||
                parser.LastOperator == HStemHm)
            {
                VStem(parser);
            }

            var maskBytes = MathUtils.BitsToBytes(parser.HintCount);
            parser.Lexer.SkipBytes(maskBytes);
        }

        [Operator(CharStringOpCode.CntrMask, clearStack: true)]
        private static void CntrMask(Parser parser)
        {
            HintMask(parser);
        }

        #endregion

        #region Arithmetic operators

        [Operator(CharStringOpCode.Abs)]
        private static void Abs(Parser parser)
        {
            parser.Stack.Pop(out double num1);
            parser.Stack.Push(Math.Abs(num1));
        }

        [Operator(CharStringOpCode.Add)]
        private static void Add(Parser parser)
        {
            parser.Stack.Pop(out double num1, out double num2);
            parser.Stack.Push(num1 + num2);
        }

        [Operator(CharStringOpCode.Sub)]
        private static void Sub(Parser parser)
        {
            parser.Stack.Pop(out double num1, out double num2);
            parser.Stack.Push(num1 - num2);
        }

        [Operator(CharStringOpCode.Div)]
        private static void Div(Parser parser)
        {
            parser.Stack.Pop(out double num1, out double num2);
            parser.Stack.Push(num1 / num2);
        }

        [Operator(CharStringOpCode.Neg)]
        private static void Neg(Parser parser)
        {
            parser.Stack.Pop(out double num1);
            parser.Stack.Push(-num1);
        }

        [Operator(CharStringOpCode.Random)]
        private static void Random(Parser parser)
        {
            // PdfToSvg.NET does not support the random operator. It will always generate 0.42.
            parser.Stack.Push(0.42d);
        }

        [Operator(CharStringOpCode.Mul)]
        private static void Mul(Parser parser)
        {
            parser.Stack.Pop(out double num1, out double num2);
            parser.Stack.Push(num1 * num2);
        }

        [Operator(CharStringOpCode.Sqrt)]
        private static void Sqrt(Parser parser)
        {
            parser.Stack.Pop(out double num1);
            parser.Stack.Push(Math.Sqrt(num1));
        }

        [Operator(CharStringOpCode.Drop)]
        private static void Drop(Parser parser)
        {
            parser.Stack.Pop(out int n);
            parser.Stack.RemoveFrom(MathUtils.Clamp(parser.Stack.Count - n, 0, parser.Stack.Count));
        }

        [Operator(CharStringOpCode.Exch)]
        private static void Exch(Parser parser)
        {
            if (parser.Stack.Count < 2)
            {
                throw new CharStringStackUnderflowException();
            }

            var index1 = parser.Stack.Count - 1;
            var index2 = parser.Stack.Count - 2;

            var tmp = parser.Stack[index1];
            parser.Stack[index1] = parser.Stack[index2];
            parser.Stack[index2] = tmp;
        }

        [Operator(CharStringOpCode.Index)]
        private static void Index(Parser parser)
        {
            parser.Stack.Pop(out int n);

            if (n < 0)
            {
                n = 0;
            }

            var index = parser.Stack.Count - n - 1;
            if (index < 0)
            {
                throw new CharStringStackUnderflowException();
            }

            parser.Stack.Push(parser.Stack[index]);
        }

        [Operator(CharStringOpCode.Roll)]
        private static void Roll(Parser parser)
        {
            parser.Stack.Pop(out int n, out int j);
            parser.Stack.Roll(n, j);
        }

        [Operator(CharStringOpCode.Dup)]
        private static void Dup(Parser parser)
        {
            parser.Stack.Push(parser.Stack.Peek());
        }

        #endregion

        #region Storage operators

        [Operator(CharStringOpCode.Put)]
        private static void Put(Parser parser)
        {
            parser.Stack.Pop(out int i);
            parser.Stack.Pop(out double val);

            if (i < parser.Storage.Length)
            {
                parser.Storage[i] = val;
            }
        }

        [Operator(CharStringOpCode.Get)]
        private static void Get(Parser parser)
        {
            parser.Stack.Pop(out int i);
            parser.Stack.Push(i < parser.Storage.Length
                ? parser.Storage[i]
                : 0);
        }

        #endregion

        #region Conditional operators

        [Operator(CharStringOpCode.And)]
        private static void And(Parser parser)
        {
            parser.Stack.Pop(out double num1, out double num2);
            parser.Stack.Push(num1 != 0 && num2 != 0 ? 1 : 0);
        }

        [Operator(CharStringOpCode.Or)]
        private static void Or(Parser parser)
        {
            parser.Stack.Pop(out double num1, out double num2);
            parser.Stack.Push(num1 != 0 || num2 != 0 ? 1 : 0);
        }

        [Operator(CharStringOpCode.Not)]
        private static void Not(Parser parser)
        {
            parser.Stack.Pop(out double num1);
            parser.Stack.Push(num1 != 0 ? 0 : 1);
        }

        [Operator(CharStringOpCode.Eq)]
        private static void Eq(Parser parser)
        {
            parser.Stack.Pop(out double num1, out double num2);
            parser.Stack.Push(num1 == num2 ? 1 : 0);
        }

        [Operator(CharStringOpCode.IfElse)]
        private static void IfElse(Parser parser)
        {
            parser.Stack.Pop(out double v1, out double v2);
            parser.Stack.Pop(out double s1, out double s2);
            parser.Stack.Push(v1 <= v2 ? s1 : s2);
        }

        #endregion

        #region Subroutine operators

        [Operator(CharStringOpCode.CallSubr)]
        private static void CallSubr(Parser parser)
        {
            parser.Stack.Pop(out int number);

            parser.CallSubr(number, global: false);
        }

        [Operator(CharStringOpCode.CallGSubr)]
        private static void CallGSubr(Parser parser)
        {
            parser.Stack.Pop(out int number);

            parser.CallSubr(number, global: true);
        }

        [Operator(CharStringOpCode.Return)]
        private static void Return(Parser parser)
        {
            parser.Return();
        }

        #endregion

        #region Deprecated operators

        [Operator(CharStringOpCode.DotSection)]
        private static void DotSection(Parser parser)
        {
            // Treat as a noop according to spec
        }

        #endregion
    }
}
