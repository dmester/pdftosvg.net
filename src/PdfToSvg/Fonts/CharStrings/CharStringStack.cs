// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace PdfToSvg.Fonts.CharStrings
{
    internal class CharStringStack
    {
        private readonly List<double> stack = new List<double>();

        public int Count => stack.Count;

        public double this[int index]
        {
            get => stack[index];
            set => stack[index] = value;
        }

        public void Clear()
        {
            stack.Clear();
        }

        public void RemoveFrom(int index)
        {
            stack.RemoveRange(index, stack.Count - index);
        }

        public void Roll(int windowSize, int shiftAmount)
        {
            if (windowSize <= 0)
            {
                return;
            }

            if (stack.Count < windowSize)
            {
                throw new CharStringStackUnderflowException();
            }

            stack.RollEnd(windowSize, shiftAmount);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Push(double value)
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
        public double Peek()
        {
            var index = stack.Count - 1;
            if (index >= 0)
            {
                return stack[index];
            }
            else
            {
                throw new CharStringStackUnderflowException();
            }
        }

        public void Pop(out double result)
        {
            var index = stack.Count - 1;
            if (index >= 0)
            {
                result = stack[index];
                stack.RemoveAt(index);
            }
            else
            {
                throw new CharStringStackUnderflowException();
            }
        }

        public void Pop(out int result)
        {
            Pop(out double value);
            result = (int)value;
        }
    }
}
