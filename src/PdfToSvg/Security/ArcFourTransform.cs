// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace PdfToSvg.Security
{
    // Implementation based on pseudo-code from:
    // https://en.wikipedia.org/wiki/RC4#Description
    internal class ArcFourTransform : ICryptoTransform
    {
        private const int StateLength = 256;
        private readonly byte[] state;
        private int i;
        private int j;

        public ArcFourTransform(byte[] key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (key.Length == 0) throw new ArgumentException("The key cannot be empty.", nameof(key));

            state = new byte[StateLength];

            for (var i = 0; i < state.Length; i++)
            {
                state[i] = unchecked((byte)i);
            }

            var j = 0;

            for (var i = 0; i < state.Length; i++)
            {
                j = (j + state[i] + key[i % key.Length]) % StateLength;
                Swap(ref state[i], ref state[j]);
            }
        }

        public int InputBlockSize => 1;

        public int OutputBlockSize => 1;

        public bool CanTransformMultipleBlocks => true;

        public bool CanReuseTransform => true;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static void Swap<T>(ref T a, ref T b)
        {
            var temp = a;
            a = b;
            b = temp;
        }

        private void UncheckedTransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            while (inputCount-- > 0)
            {
                i = (i + 1) % StateLength;
                j = (j + state[i]) % StateLength;

                Swap(ref state[i], ref state[j]);

                var K = state[(state[i] + state[j]) % StateLength];

                outputBuffer[outputOffset++] = unchecked((byte)(K ^ inputBuffer[inputOffset++]));
            }
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            if (inputBuffer == null) throw new ArgumentNullException(nameof(inputBuffer));
            if (inputOffset < 0) throw new ArgumentOutOfRangeException(nameof(inputOffset));
            if (inputCount < 0 || inputOffset + inputCount > inputBuffer.Length) throw new ArgumentOutOfRangeException(nameof(inputCount));

            if (outputBuffer == null) throw new ArgumentNullException(nameof(outputBuffer));
            if (outputOffset < 0) throw new ArgumentOutOfRangeException(nameof(outputOffset));

            var count = Math.Min(inputCount, outputBuffer.Length - outputOffset);
            UncheckedTransformBlock(inputBuffer, inputOffset, count, outputBuffer, outputOffset);
            return count;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            if (inputBuffer == null) throw new ArgumentNullException(nameof(inputBuffer));
            if (inputOffset < 0) throw new ArgumentOutOfRangeException(nameof(inputOffset));
            if (inputCount < 0 || inputOffset + inputCount > inputBuffer.Length) throw new ArgumentOutOfRangeException(nameof(inputCount));

            var output = new byte[inputCount];
            UncheckedTransformBlock(inputBuffer, inputOffset, inputCount, output, 0);
            return output;
        }

        public void Dispose()
        {
        }
    }
}
