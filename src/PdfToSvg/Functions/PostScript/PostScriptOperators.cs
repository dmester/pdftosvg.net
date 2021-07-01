// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable IDE0051 // Unused methods

namespace PdfToSvg.Functions.PostScript
{
    internal static class PostScriptOperators
    {
        private const double DegreesToRadians = (2d * Math.PI) / 360d;
        private const double RadiansToDegrees = 360d / (2d * Math.PI);

        private static readonly Dictionary<string, PostScriptInstruction> operators;

        static PostScriptOperators()
        {

            operators = typeof(PostScriptOperators)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod)
                .Select(method => new
                {
                    Method = method,
                    Parameters = method.GetParameters(),
                })
                .Where(method => method.Parameters.Length == 1 && method.Parameters[0].ParameterType == typeof(PostScriptStack))
                .ToDictionary(
                    method => method.Method.Name.ToLowerInvariant(),
                    method => (PostScriptInstruction)Delegate.CreateDelegate(typeof(PostScriptInstruction), method.Method)
                    );
        }

        public static bool TryGetOperator(string operatorName, out PostScriptInstruction result)
        {
            return operators.TryGetValue(operatorName, out result);
        }

        private static void Add(PostScriptStack stack)
        {
            stack.Pop(out double num1, out double num2);
            stack.Push(num1 + num2);
        }

        private static void Sub(PostScriptStack stack)
        {
            stack.Pop(out double num1, out double num2);
            stack.Push(num1 - num2);
        }

        private static void Mul(PostScriptStack stack)
        {
            stack.Pop(out double num1, out double num2);
            stack.Push(num1 * num2);
        }

        private static void Div(PostScriptStack stack)
        {
            stack.Pop(out double num1, out double num2);
            stack.Push(num1 / num2);
        }

        private static void Idiv(PostScriptStack stack)
        {
            stack.Pop(out int num1, out int num2);
            stack.Push(num1 / num2);
        }

        private static void Mod(PostScriptStack stack)
        {
            stack.Pop(out int num1, out int num2);
            stack.Push(num1 % num2);
        }

        private static void Neg(PostScriptStack stack)
        {
            stack.Pop(out double num1);
            stack.Push(-num1);
        }

        private static void Abs(PostScriptStack stack)
        {
            stack.Pop(out double num1);
            stack.Push(Math.Abs(num1));
        }

        private static void Ceiling(PostScriptStack stack)
        {
            stack.Pop(out double num1);
            stack.Push(Math.Ceiling(num1));
        }

        private static void Floor(PostScriptStack stack)
        {
            stack.Pop(out double num1);
            stack.Push(Math.Floor(num1));
        }

        private static void Round(PostScriptStack stack)
        {
            stack.Pop(out double num1);
            stack.Push(Math.Round(num1));
        }

        private static void Truncate(PostScriptStack stack)
        {
            stack.Pop(out double num1);
            stack.Push(Math.Truncate(num1));
        }

        private static void Sqrt(PostScriptStack stack)
        {
            stack.Pop(out double num1);
            stack.Push(Math.Sqrt(num1));
        }

        private static void Sin(PostScriptStack stack)
        {
            stack.Pop(out double num1);
            stack.Push(Math.Sin(num1 * DegreesToRadians));
        }

        private static void Cos(PostScriptStack stack)
        {
            stack.Pop(out double num1);
            stack.Push(Math.Cos(num1 * DegreesToRadians));
        }

        private static void Atan(PostScriptStack stack)
        {
            stack.Pop(out double num, out double den);

            // Expected range: 0 - 360
            var atanDegrees = Math.Atan(num / den) * RadiansToDegrees;
            if (atanDegrees < 0) atanDegrees += 360d;

            stack.Push(atanDegrees);
        }

        private static void Exp(PostScriptStack stack)
        {
            stack.Pop(out double @base, out double exponent);
            stack.Push(Math.Pow(@base, exponent));
        }

        private static void Ln(PostScriptStack stack)
        {
            stack.Pop(out double num);
            stack.Push(Math.Log(num));
        }

        private static void Log(PostScriptStack stack)
        {
            stack.Pop(out double num);
            stack.Push(Math.Log10(num));
        }

        private static void Cvi(PostScriptStack stack)
        {
            stack.Pop(out int num);
            stack.Push(num);
        }

        private static void Cvr(PostScriptStack stack)
        {
            stack.Pop(out double num);
            stack.Push((double)num);
        }

        private static void Eq(PostScriptStack stack)
        {
            stack.Pop(out object any1, out object any2);
            stack.Push(Equals(any1, any2));
        }

        private static void Ne(PostScriptStack stack)
        {
            stack.Pop(out object any1, out object any2);
            stack.Push(!Equals(any1, any2));
        }

        private static void Gt(PostScriptStack stack)
        {
            stack.Pop(out double num1, out double num2);
            stack.Push(num1 > num2);
        }

        private static void Ge(PostScriptStack stack)
        {
            stack.Pop(out double num1, out double num2);
            stack.Push(num1 >= num2);
        }

        private static void Lt(PostScriptStack stack)
        {
            stack.Pop(out double num1, out double num2);
            stack.Push(num1 < num2);
        }

        private static void Le(PostScriptStack stack)
        {
            stack.Pop(out double num1, out double num2);
            stack.Push(num1 <= num2);
        }

        private static void And(PostScriptStack stack)
        {
            if (stack.Peek() is bool)
            {
                stack.Pop(out bool val1, out bool val2);
                stack.Push(val1 && val2);
            }
            else
            {
                stack.Pop(out int val1, out int val2);
                stack.Push(val1 & val2);
            }
        }

        private static void Or(PostScriptStack stack)
        {
            if (stack.Peek() is bool)
            {
                stack.Pop(out bool val1, out bool val2);
                stack.Push(val1 || val2);
            }
            else
            {
                stack.Pop(out int val1, out int val2);
                stack.Push(val1 | val2);
            }
        }

        private static void Xor(PostScriptStack stack)
        {
            if (stack.Peek() is bool)
            {
                stack.Pop(out bool val1, out bool val2);
                stack.Push(val1 != val2);
            }
            else
            {
                stack.Pop(out int val1, out int val2);
                stack.Push(val1 ^ val2);
            }
        }

        private static void Not(PostScriptStack stack)
        {
            if (stack.Peek() is bool)
            {
                stack.Pop(out bool val1);
                stack.Push(!val1);
            }
            else
            {
                stack.Pop(out int val1);
                stack.Push(~val1);
            }
        }

        private static void BitShift(PostScriptStack stack)
        {
            stack.Pop(out int int1, out int shift);
            stack.Push(shift > 0 ? (int1 << shift) : (int1 >> (-shift)));
        }

        private static void True(PostScriptStack stack)
        {
            stack.Push(true);
        }

        private static void False(PostScriptStack stack)
        {
            stack.Push(false);
        }

        private static void If(PostScriptStack stack)
        {
            stack.Pop(out bool condition, out PostScriptExpression block1);

            if (condition)
            {
                block1.Execute(stack);
            }
        }

        private static void IfElse(PostScriptStack stack)
        {
            stack.Pop(out bool condition, out PostScriptExpression block1, out PostScriptExpression block2);

            if (condition)
            {
                block1.Execute(stack);
            }
            else
            {
                block2.Execute(stack);
            }
        }

        private static void Pop(PostScriptStack stack)
        {
            stack.Pop();
        }

        private static void Exch(PostScriptStack stack)
        {
            stack.Exchange();
        }

        private static void Dup(PostScriptStack stack)
        {
            stack.Push(stack.Peek());
        }

        private static void Copy(PostScriptStack stack)
        {
            stack.Pop(out int n);
            stack.Copy(n);
        }

        private static void Index(PostScriptStack stack)
        {
            stack.Pop(out int n);

            stack.Push(stack.Get(n));
        }

        private static void Roll(PostScriptStack stack)
        {
            stack.Pop(out int n, out int j);
            stack.Roll(n, j);
        }
    }
}
