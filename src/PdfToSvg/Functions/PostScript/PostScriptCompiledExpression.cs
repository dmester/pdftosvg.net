// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PdfToSvg.Functions.PostScript
{
    /// <threadsafety instance="true" />
    internal class PostScriptCompiledExpression(
        PostScriptCompiledInstruction[] instructions,
        int registerCount,
        int[] outputRegisters,
        double[] domain,
        double[] range
        )
    {
        private volatile double[]? pooledRegisterArray = new double[registerCount];

        public readonly PostScriptCompiledInstruction[] Instructions = instructions;
        public readonly int RegisterCount = registerCount;
        public readonly int[] OutputRegisters = outputRegisters;

        public void Execute(
            float[] input, int inputOffset,
            float[] output, int outputOffset
            )
        {
            var instructions = Instructions;

            var registers = RentRegisterArray();

            var localDomain = domain;
            for (var i = 1; i < localDomain.Length; i += 2)
            {
                var inputIndex = i / 2;

                registers[inputIndex] = MathUtils.Clamp(
                    input[inputOffset + inputIndex],
                    min: localDomain[i - 1],
                    max: localDomain[i]
                    );
            }

            for (var i = 0; i < instructions.Length; i++)
            {
                var instruction = instructions[i];

                var left = instruction.SourceRegister1 < 0
                    ? instruction.Immediate
                    : registers[instruction.SourceRegister1];

                var right = instruction.SourceRegister2 < 0
                    ? instruction.Immediate
                    : registers[instruction.SourceRegister2];

                var result = PostScriptOperators.Execute(instruction.OpCode, left, right);
                registers[instruction.DestinationRegister] = result;
            }

            var localRange = range;
            for (var i = 1; i < localRange.Length; i += 2)
            {
                var outputIndex = i / 2;
                var result = registers[OutputRegisters[outputIndex]];
                
                if (double.IsNaN(result))
                {
                    result = 0;
                }

                output[outputOffset + outputIndex] = (float)MathUtils.Clamp(
                    result,
                    min: localRange[i - 1],
                    max: localRange[i]
                    );
            }

            ReturnRegisterArray(registers);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private double[] RentRegisterArray()
        {
            var registers = Interlocked.Exchange(ref pooledRegisterArray, null);
            return registers ?? new double[RegisterCount];
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private void ReturnRegisterArray(double[] registers)
        {
            pooledRegisterArray = registers;
        }

    }
}
