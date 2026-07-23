// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

namespace PdfToSvg.Functions.PostScript
{
    /// <threadsafety instance="false" />
    internal class PostScriptStack
    {
        private readonly PostScriptValue[] values = new PostScriptValue[PostScriptLimits.StackSize];
        private int count;

        public int Count => count;

        public void Reset()
        {
            count = 0;
        }

        public void CopyTo(float[] output, int offset, int count)
        {
            var startIndex = this.count - count;
            if (startIndex < 0)
            {
                throw new PostScriptStackUnderflowException();
            }

            for (var i = 0; i < count; i++)
            {
                ref var value = ref values[startIndex + i];
                var type = value.Type;

                output[offset + i] =
                    type == PostScriptValueType.Number ? (float)value.Number :
                    0f;
            }
        }

        public void Roll(int windowSize, int shiftAmount)
        {
            if (windowSize <= 0)
            {
                return;
            }

            if (count < windowSize)
            {
                throw new PostScriptStackUnderflowException();
            }

            // Normalize to a left rotation by [0, windowSize) elements
            var leftShift = shiftAmount % windowSize;
            leftShift = leftShift > 0 ? windowSize - leftShift : -leftShift;

            if (leftShift != 0)
            {
                var startIndex = count - windowSize;

                Reverse(startIndex, leftShift);
                Reverse(startIndex + leftShift, windowSize - leftShift);
                Reverse(startIndex, windowSize);
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private void Reverse(int index, int length)
        {
            var i = index;
            var j = index + length - 1;

            while (i < j)
            {
                var tmp = values[i];
                values[i] = values[j];
                values[j] = tmp;
                i++;
                j--;
            }
        }

        public void Exchange()
        {
            if (count < 2)
            {
                throw new PostScriptStackUnderflowException();
            }

            var tmp = values[count - 1];
            values[count - 1] = values[count - 2];
            values[count - 2] = tmp;
        }

        public void Copy(int copyCount)
        {
            if (copyCount <= 0)
            {
                return;
            }

            var startIndex = count - copyCount;
            if (startIndex < 0)
            {
                throw new PostScriptStackUnderflowException();
            }

            if (count + copyCount > values.Length)
            {
                throw new PostScriptStackOverflowException();
            }

            Array.Copy(values, startIndex, values, count, copyCount);
            count += copyCount;
        }

        public PostScriptValue Get(int index)
        {
            index = count - index - 1;

            if (index < 0 || index >= count)
            {
                throw new PostScriptStackUnderflowException();
            }

            return values[index];
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private void Push(in PostScriptValue value)
        {
            if (count == values.Length)
            {
                throw new PostScriptStackOverflowException();
            }

            values[count++] = value;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Push(double value)
        {
            Push(new PostScriptValue(value));
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Push(int value)
        {
            Push(new PostScriptValue(value));
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Push(bool value)
        {
            Push(new PostScriptValue(value));
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void PushBlock(int blockIndex)
        {
            Push(new PostScriptValue(PostScriptValueType.Expression, blockIndex));
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Push(PostScriptValue value)
        {
            Push(in value);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Pop(out int int1, out int int2)
        {
            if (count < 2)
            {
                throw new PostScriptStackUnderflowException();
            }

            int2 = (int)values[--count].Number;
            int1 = (int)values[--count].Number;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Pop(out double num1, out double num2)
        {
            if (count < 2)
            {
                throw new PostScriptStackUnderflowException();
            }

            num2 = values[--count].Number;
            num1 = values[--count].Number;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Pop(out PostScriptValue any1, out PostScriptValue any2)
        {
            if (count < 2)
            {
                throw new PostScriptStackUnderflowException();
            }

            any2 = values[--count];
            any1 = values[--count];
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Pop(out PostScriptValue any)
        {
            if (count == 0)
            {
                throw new PostScriptStackUnderflowException();
            }

            any = values[--count];
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public PostScriptValue Peek()
        {
            if (count == 0)
            {
                throw new PostScriptStackUnderflowException();
            }

            return values[count - 1];
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Pop()
        {
            Pop(out PostScriptValue _);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void PopBlock2(out bool bool1, out int block1, out int block2)
        {
            PopBlock(out block2);
            PopBlock(out block1);
            Pop(out bool1);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void PopBlock1(out bool bool1, out int block1)
        {
            PopBlock(out block1);
            Pop(out bool1);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Pop(out bool bool1, out bool bool2)
        {
            Pop(out bool2);
            Pop(out bool1);
        }

        public void PopBlock(out int result)
        {
            Pop(out PostScriptValue value);

            result =
                value.Type == PostScriptValueType.Expression
                ? (int)value.Number
                : throw new InvalidCastException($"Expected block operand, but got value of type {value.TypeName}");
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Pop(out bool result)
        {
            Pop(out PostScriptValue value);
            result = value.Number != 0;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Pop(out double result)
        {
            Pop(out PostScriptValue value);
            result = value.Number;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void Pop(out int result)
        {
            Pop(out PostScriptValue value);
            result = (int)value.Number;
        }
    }

}
