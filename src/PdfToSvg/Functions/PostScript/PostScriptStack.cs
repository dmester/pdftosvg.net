// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Functions.PostScript
{
    internal class PostScriptStack
    {
        private readonly List<object> stack = new List<object>();

        public int Count => stack.Count;

        public double[] ToDoubleArray()
        {
            var result = new double[stack.Count];

            for (var i = 0; i < result.Length; i++)
            {
                var value = stack[i];

                result[i] =
                    value is int iValue ? iValue :
                    value is double dblValue ? dblValue :
                    0d;
            }

            return result;
        }

        public void Roll(int windowSize, int shiftAmount)
        {
            if (windowSize <= 0)
            {
                return;
            }

            if (stack.Count < windowSize)
            {
                throw new PostScriptStackUnderflowException();
            }

            // Normalize shift amount to (1-n, 0]
            shiftAmount = shiftAmount % windowSize;
            if (shiftAmount > 0) shiftAmount -= windowSize;

            if (shiftAmount != 0)
            {
                var startIndex = stack.Count - windowSize;

                for (var i = 0; i > shiftAmount; i--)
                {
                    stack.Add(stack[startIndex - i]);
                }

                stack.RemoveRange(startIndex, -shiftAmount);
            }
        }

        public void Exchange()
        {
            if (stack.Count < 2)
            {
                throw new PostScriptStackUnderflowException();
            }

            var tmp = stack[stack.Count - 1];
            stack[stack.Count - 1] = stack[stack.Count - 2];
            stack[stack.Count - 2] = tmp;
        }

        public void Copy(int count)
        {
            var startIndex = stack.Count - count;
            if (startIndex < 0)
            {
                throw new PostScriptStackUnderflowException();
            }

            for (var i = 0; i < count; i++)
            {
                Push(stack[startIndex + i]);
            }
        }

        public object Get(int index)
        {
            index = stack.Count - index - 1;

            if (index < 0)
            {
                throw new PostScriptStackUnderflowException();
            }

            return stack[index];
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Push(object value)
        {
            stack.Add(value);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Pop(out int int1, out int int2)
        {
            Pop(out int2);
            Pop(out int1);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Pop(out double num1, out double num2)
        {
            Pop(out num2);
            Pop(out num1);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Pop(out object any1, out object any2)
        {
            Pop(out any2);
            Pop(out any1);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Pop(out object any)
        {
            var index = stack.Count - 1;
            if (index >= 0)
            {
                any = stack[index];
                stack.RemoveAt(index);
            }
            else
            {
                throw new PostScriptStackUnderflowException();
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public object Peek()
        {
            var index = stack.Count - 1;
            if (index >= 0)
            {
                return stack[index];
            }
            else
            {
                throw new PostScriptStackUnderflowException();
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Pop()
        {
            Pop(out object _);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Pop(out bool bool1, out PostScriptExpression block1, out PostScriptExpression block2)
        {
            Pop(out block2);
            Pop(out block1);
            Pop(out bool1);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Pop(out bool bool1, out PostScriptExpression block1)
        {
            Pop(out block1);
            Pop(out bool1);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Pop(out bool bool1, out bool bool2)
        {
            Pop(out bool2);
            Pop(out bool1);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Pop(out PostScriptExpression result)
        {
            Pop(out object value);

            result =
                value is PostScriptExpression block ? block :
                throw new InvalidCastException($"Expected block operand, but got value of type {Log.TypeOf(value)}");
        }

        public void Pop(out bool result)
        {
            Pop(out object value);

            result =
                value is bool bValue ? bValue :
                value is int intValue ? intValue != 0 :
                value is double dblValue ? dblValue != 0 :
                throw new InvalidCastException($"Expected boolean operand, but got value of type {Log.TypeOf(value)}");
        }

        public void Pop(out double result)
        {
            Pop(out object value);

            result =
                value is int intValue ? (double)intValue :
                value is double dblValue ? dblValue :
                value is bool bValue ? (bValue ? 1 : 0) :
                throw new InvalidCastException($"Expected numeric operand, but got value of type {Log.TypeOf(value)}");
        }

        public void Pop(out int result)
        {
            Pop(out object value);

            result =
                value is int intValue ? intValue :
                value is double dblValue ? (int)dblValue :
                throw new InvalidCastException($"Expected operand of type int, but got value of type {Log.TypeOf(value)}");
        }

    }
}
