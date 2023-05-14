// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Parser = PdfToSvg.Fonts.CharStrings.CharStringParser;

namespace PdfToSvg.Fonts.CharStrings
{
    internal static class CharStringOperators
    {
        [AttributeUsage(AttributeTargets.Method)]
        private class OperatorAttribute : Attribute
        {
            public OperatorAttribute(CharStringOpCode code, bool clearStack = false)
            {
                Code = code;
                ClearStack = clearStack;
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
                        method.Operator.ClearStack));
        }

        public static bool TryGetOperator(CharStringOpCode code, out CharStringOperator result)
        {
            return operators.TryGetValue(code, out result);
        }

        #region Path construction operators

        [Operator(CharStringOpCode.RMoveTo, clearStack: true)]
        private static void RMoveTo(Parser parser)
        {
            if (parser.FlexPoints == null)
            {
                parser.AppendContent(CharStringOpCode.RMoveTo, last: 2);
                parser.Stack.Pop(out double dx1, out double dy1);
                parser.Path.RMoveTo(dx1, dy1);
            }
            else
            {
                parser.Stack.Pop(out double dx1, out double dy1);
                parser.FlexPoints.Add(new Point(dx1, dy1));
            }
        }

        [Operator(CharStringOpCode.HMoveTo, clearStack: true)]
        private static void HMoveTo(Parser parser)
        {
            if (parser.FlexPoints == null)
            {
                parser.AppendContent(CharStringOpCode.HMoveTo, last: 1);
                parser.Stack.Pop(out double dx1);
                parser.Path.RMoveTo(dx1, 0);
            }
            else
            {
                parser.Stack.Pop(out double dx1);
                parser.FlexPoints.Add(new Point(dx1, 0));
            }
        }

        [Operator(CharStringOpCode.VMoveTo, clearStack: true)]
        private static void VMoveTo(Parser parser)
        {
            if (parser.FlexPoints == null)
            {
                parser.AppendContent(CharStringOpCode.VMoveTo, last: 1);
                parser.Stack.Pop(out double dy1);
                parser.Path.RMoveTo(0, dy1);
            }
            else
            {
                parser.Stack.Pop(out double dy1);
                parser.FlexPoints.Add(new Point(0, dy1));
            }
        }

        [Operator(CharStringOpCode.RLineTo, clearStack: true)]
        private static void RLineTo(Parser parser)
        {
            var startAt = parser.Stack.Count % 2;

            for (var i = startAt; i < parser.Stack.Count; i += 2)
            {
                parser.Path.RLineTo(parser.Stack[i], parser.Stack[i + 1]);
            }

            parser.AppendContent(CharStringOpCode.RLineTo, from: startAt);
            parser.Stack.RemoveFrom(startAt);
        }

        private static void AlternatingLineTo(Parser parser, bool startHorizontally)
        {
            var horizontal = startHorizontally;

            parser.AppendContent(
                startHorizontally ? CharStringOpCode.HLineTo : CharStringOpCode.VLineTo,
                from: 0);

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

            parser.AppendContent(CharStringOpCode.RRCurveTo, from: startAt);
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

            parser.AppendContent(CharStringOpCode.HHCurveTo, from: removeFrom);
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

            parser.AppendContent(
                startHorizontal ? CharStringOpCode.HVCurveTo : CharStringOpCode.VHCurveTo,
                from: startAt);

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

            parser.AppendContent(CharStringOpCode.RCurveLine, from: startAt);
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

            parser.AppendContent(CharStringOpCode.RLineCurve, from: startAt);
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

            parser.AppendContent(CharStringOpCode.VVCurveTo, from: removeFrom);
            parser.Stack.RemoveFrom(removeFrom);
        }

        [Operator(CharStringOpCode.Flex, clearStack: true)]
        private static void Flex(Parser parser)
        {
            var startAt = parser.Stack.Count - 13;
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

            parser.AppendContent(CharStringOpCode.Flex, from: startAt);
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

            parser.AppendContent(CharStringOpCode.HFlex, from: startAt);
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

            parser.AppendContent(CharStringOpCode.HFlex1, from: startAt);
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

            parser.AppendContent(CharStringOpCode.Flex1, from: startAt);
            parser.Stack.RemoveFrom(startAt);
        }

        #endregion

        #region Operator for finishing a path

        [Operator(CharStringOpCode.EndChar, clearStack: true)]
        private static void EndChar(Parser parser)
        {
            if (parser.Stack.Count >= 4)
            {
                parser.CharString.Seac = new CharStringSeacInfo(
                    adx: parser.Stack[parser.Stack.Count - 4],
                    ady: parser.Stack[parser.Stack.Count - 3],
                    bchar: (int)parser.Stack[parser.Stack.Count - 2],
                    achar: (int)parser.Stack[parser.Stack.Count - 1]);

                parser.AppendContent(CharStringOpCode.EndChar, last: 4);
                parser.Stack.RemoveFrom(parser.Stack.Count - 4);
            }
            else
            {
                parser.AppendContent(CharStringOpCode.EndChar);
            }

            parser.EndChar();
        }

        #endregion

        #region Hint operators

        private static void Hint(Parser parser, bool isHorizontal, CharStringOpCode? opCode = null)
        {
            // All hints use an even number of arguments
            var startAt = parser.Stack.Count % 2;

            // We could handle hints from type 1 char strings as well, but as long as we don't support turning on and
            // off hints by othersubr #3, they will not work optimally. After testing, it was decided to ignore hints
            // as it caused better and more consistent results than having all hints enabled all the time.
            if (parser.Type == CharStringType.Type2)
            {
                parser.CharString.HintCount += parser.Stack.Count / 2;

                if (startAt < parser.Stack.Count)
                {
                    var sideBearing = isHorizontal
                        ? parser.Path.LastY
                        : parser.Path.LastX;

                    parser.Stack[startAt] = parser.Stack[startAt] + sideBearing;

                    for (var i = startAt; i < parser.Stack.Count; i++)
                    {
                        parser.CharString.Hints.Add(CharStringLexeme.Operand(parser.Stack[i]));
                    }
                }

                if (opCode.HasValue)
                {
                    parser.CharString.Hints.Add(CharStringLexeme.Operator(opCode.Value));
                }
            }

            parser.Stack.RemoveFrom(startAt);
        }

        [Operator(CharStringOpCode.HStem, clearStack: true)]
        private static void HStem(Parser parser)
        {
            Hint(parser, isHorizontal: true, CharStringOpCode.HStem);
        }

        [Operator(CharStringOpCode.VStem, clearStack: true)]
        private static void VStem(Parser parser)
        {
            Hint(parser, isHorizontal: false, CharStringOpCode.VStem);
        }

        [Operator(CharStringOpCode.HStemHm, clearStack: true)]
        private static void HStemHm(Parser parser)
        {
            Hint(parser, isHorizontal: true, CharStringOpCode.HStemHm);
        }

        [Operator(CharStringOpCode.VStemHm, clearStack: true)]
        private static void VStemHm(Parser parser)
        {
            Hint(parser, isHorizontal: false, CharStringOpCode.VStemHm);
        }

        private static void Mask(Parser parser, CharStringOpCode opCode)
        {
            // vstem hint operator is optional if hstem and vstem direcly preceeds the hintmask operator.
            var lastOperator = parser.CharString.Hints.LastOrDefault(x => x.Token == CharStringToken.Operator);

            if (lastOperator.OpCode == CharStringOpCode.HStem ||
                lastOperator.OpCode == CharStringOpCode.HStemHm)
            {
                Hint(parser, isHorizontal: true);
            }

            parser.CharString.Content.Add(CharStringLexeme.Operator(opCode));

            // Mask
            var maskBytes = MathUtils.BitsToBytes(parser.CharString.HintCount);

            for (var i = 0; i < maskBytes; i++)
            {
                var lexeme = CharStringLexeme.Mask(parser.Lexer.ReadByte());
                parser.CharString.Content.Add(lexeme);
            }
        }

        [Operator(CharStringOpCode.HintMask, clearStack: true)]
        private static void HintMask(Parser parser)
        {
            Mask(parser, CharStringOpCode.HintMask);
        }

        [Operator(CharStringOpCode.CntrMask, clearStack: true)]
        private static void CntrMask(Parser parser)
        {
            Mask(parser, CharStringOpCode.CntrMask);
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

        #region Type 1 operators

        [Operator(CharStringOpCode.VStem3, clearStack: true)]
        private static void VStem3(Parser parser)
        {
            var startAt = parser.Stack.Count - 6;
            if (startAt < 0)
            {
                throw new CharStringStackUnderflowException();
            }

            // Make relative
            parser.Stack[startAt + 4] =
                parser.Stack[startAt + 4] - parser.Stack[startAt + 2] - parser.Stack[startAt + 3];

            parser.Stack[startAt + 2] =
                parser.Stack[startAt + 2] - parser.Stack[startAt + 0] - parser.Stack[startAt + 1];

            VStem(parser);
        }


        [Operator(CharStringOpCode.HStem3, clearStack: true)]
        private static void HStem3(Parser parser)
        {
            var startAt = parser.Stack.Count - 6;
            if (startAt < 0)
            {
                throw new CharStringStackUnderflowException();
            }

            // Make relative
            parser.Stack[startAt + 4] =
                parser.Stack[startAt + 4] - parser.Stack[startAt + 2] - parser.Stack[startAt + 3];

            parser.Stack[startAt + 2] =
                parser.Stack[startAt + 2] - parser.Stack[startAt + 0] - parser.Stack[startAt + 1];

            HStem(parser);
        }

        [Operator(CharStringOpCode.Pop, clearStack: false)]
        private static void Pop(Parser parser)
        {
            if (parser.PostScriptStack.Count > 0)
            {
                parser.Stack.Push(parser.PostScriptStack.Pop());
            }
        }

        [Operator(CharStringOpCode.Hsbw, clearStack: true)]
        private static void Hsbw(Parser parser)
        {
            parser.Stack.Pop(out double sbx, out double wx);

            parser.CharString.Width = wx;

            if (sbx != 0)
            {
                parser.CharString.Content.Add(CharStringLexeme.Operand(sbx));
                parser.CharString.Content.Add(CharStringLexeme.Operator(CharStringOpCode.HMoveTo));
                parser.Path.RMoveTo(sbx, 0);
            }
        }

        [Operator(CharStringOpCode.Seac, clearStack: true)]
        private static void Seac(Parser parser)
        {
            parser.Stack.Pop(out int bchar, out int achar);
            parser.Stack.Pop(out double adx, out double ady);
            parser.Stack.Pop(out double asb);

            parser.CharString.Seac = new CharStringSeacInfo(adx, ady, bchar, achar);
        }

        [Operator(CharStringOpCode.Sbw, clearStack: true)]
        private static void Sbw(Parser parser)
        {
            parser.Stack.Pop(out double wx, out double wy);
            parser.Stack.Pop(out double sbx, out double sby);

            parser.CharString.Width = wx;

            if (sbx != 0 || sby != 0)
            {
                parser.CharString.Content.Add(CharStringLexeme.Operand(sbx));
                parser.CharString.Content.Add(CharStringLexeme.Operand(sby));
                parser.CharString.Content.Add(CharStringLexeme.Operator(CharStringOpCode.RMoveTo));
                parser.Path.RMoveTo(sbx, sby);
            }
        }

        [Operator(CharStringOpCode.CallOtherSubr, clearStack: true)]
        private static void CallOtherSubr(Parser parser)
        {
            // OtherSubrs are PostScript subroutines included in Type 1 fonts. The routines are however highly
            // standardized, so they are hardcoded in C# below to prevent having to implement the entire PostScript
            // language.
            //
            // What the routines do is not well documented, and the spec only include their source code in PostScript.
            // The routines below were instead created by looking at other implementations and a lot of trial and error.
            //
            // Reference implementations:
            // https://gitlab.freedesktop.org/freetype/freetype/-/blob/872a759b468ef0d88b0636d6beb074fe6b87f9cd/src/psaux/psintrp.c#L1704
            // https://github.com/mozilla/pdf.js/blob/96e34fbb7d7bb556392646a7a6720182953ac275/src/core/type1_parser.js#L260

            parser.Stack.Pop(out int n, out int othersubr);

            var args = new double[n];
            for (var i = 0; i < args.Length; i++)
            {
                parser.Stack.Pop(out args[args.Length - i - 1]);
            }

            switch ((CharStringOtherSubr)othersubr)
            {
                case CharStringOtherSubr.StartFlex:
                    parser.FlexPoints = new List<Point>(7);
                    break;

                case CharStringOtherSubr.AddFlexVector:
                    break;

                case CharStringOtherSubr.EndFlex:
                    var flexPoints = parser.FlexPoints;
                    parser.FlexPoints = null;

                    if (flexPoints?.Count == 7)
                    {
                        parser.Stack.Push(flexPoints[0].X + flexPoints[1].X);
                        parser.Stack.Push(flexPoints[0].Y + flexPoints[1].Y);

                        parser.Stack.Push(flexPoints[2].X);
                        parser.Stack.Push(flexPoints[2].Y);

                        parser.Stack.Push(flexPoints[3].X);
                        parser.Stack.Push(flexPoints[3].Y);

                        parser.Stack.Push(flexPoints[4].X);
                        parser.Stack.Push(flexPoints[4].Y);

                        parser.Stack.Push(flexPoints[5].X);
                        parser.Stack.Push(flexPoints[5].Y);

                        parser.Stack.Push(flexPoints[6].X);
                        parser.Stack.Push(flexPoints[6].Y);

                        var x3 = flexPoints[0].X + flexPoints[1].X + flexPoints[2].X + flexPoints[3].X;
                        var y3 = flexPoints[0].Y + flexPoints[1].Y + flexPoints[2].Y + flexPoints[3].Y;

                        var x6 = x3 + flexPoints[4].X + flexPoints[5].X + flexPoints[6].X;
                        var y6 = y3 + flexPoints[4].Y + flexPoints[5].Y + flexPoints[6].Y;

                        var flex = Math.Abs(x6 * y3 - y6 * x3) / Math.Sqrt(x6 * x6 + y6 * y6);

                        parser.Stack.Push(flex);

                        Flex(parser);

                        parser.PostScriptStack.Push(parser.Path.LastY);
                        parser.PostScriptStack.Push(parser.Path.LastX);
                    }
                    break;

                case CharStringOtherSubr.ChangeHints:
                    if (args.Length < 1)
                    {
                        throw new ArgumentException("Too few arguments specified to othersubr 3.");
                    }

                    parser.PostScriptStack.Push(args[0]);

                    // Potential improvement:
                    // Right now all hints will be enabled at all time. A better approach would be to transform the Type 1
                    // stems to hstemhm/vstemh and turn on/off stems after calling othersubr 3. This would however require
                    // a complete rewrite of the hint handling. Type 1 fonts are pretty rare, so let's skip this for now.
                    break;
            }
        }


        [Operator(CharStringOpCode.ClosePath, clearStack: false)]
        private static void ClosePath(Parser parser)
        {
            // Noop in type 2
        }

        [Operator(CharStringOpCode.SetCurrentPoint, clearStack: false)]
        private static void SetCurrentPoint(Parser parser)
        {
            parser.Stack.Pop(out double x, out double y);

            var dx = x - parser.Path.LastX;
            var dy = y - parser.Path.LastY;

            if (dx != 0 || dy != 0)
            {
                // This is not entirely correct, but hopefully good enough.
                parser.CharString.Content.Add(CharStringLexeme.Operand(dx));
                parser.CharString.Content.Add(CharStringLexeme.Operand(dy));
                parser.CharString.Content.Add(CharStringLexeme.Operator(CharStringOpCode.RLineTo));
            }
        }

        #endregion
    }
}
