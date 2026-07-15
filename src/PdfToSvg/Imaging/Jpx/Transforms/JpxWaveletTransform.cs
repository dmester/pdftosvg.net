// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.ImageModel;
using System;
using System.Runtime.CompilerServices;

#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
#endif

namespace PdfToSvg.Imaging.Jpx.Transforms
{
    /// <summary>
    /// Discrete wavelet transformation of tile-components
    /// </summary>
    /// <remarks>
    /// See ITU-T T.800 (06/2019) Annex F
    /// </remarks>
    internal static class JpxWaveletTransform
    {
        // Table F.4 lifting parameters for the irreversible 9-7 filter
        internal const float Alpha = -1.586134342059924f;
        internal const float Beta = -0.052980118572961f;
        internal const float Gamma = 0.882911075530934f;
        internal const float Delta = 0.443506852043971f;
        internal const float K = 1.230174104914001f;

        // The vertical pass transforms this many columns at a time. Processing a block of columns instead of a
        // single column at a time ensures that each cache line fetched from the tile component is fully used.
        // 16 floats = 64 bytes = one cache line on most machines.
        private const int ColumnBlockWidth = 16;

        // The sample buffer stores each resolution level with the low-pass coefficients first, followed by the
        // high-pass coefficients, both horizontally within each row, and vertically within each column. Lifting is
        // performed directly on this split layout, and the coefficients are interleaved into natural order at the
        // end of each pass.
        //
        // In the interleaved signal x used by ITU-T T.800 Section F.3.8, low-pass samples sit at positions with the
        // same parity as the resolution level origin, and high-pass samples at the opposite parity. A lifting step
        // updates every target sample from its two nearest neighbors, which always belong to the opposite
        // (source) group:
        //
        //     x[2k + q] += f(x[2k + q - 1], x[2k + q + 1])
        //
        // where q is the parity of the target group. In the split layout this corresponds to:
        //
        //     target[k] += f(source[k + q - 1], source[k + q])
        //
        // At the signal boundaries, the missing neighbor is mirrored (x[-1] => x[1] and x[n] => x[n - 2],
        // Section F.3.7), which makes both neighbors the same source sample.

        /// <summary>
        /// Transforms the samples of one tile-component in place, reconstructing resolution level
        /// <paramref name="decompositionLevels"/>. <paramref name="samples"/> is a flat row-major sample buffer with
        /// the dimensions of that resolution level, holding the sub-band coefficients in the split layout described
        /// above. Passing fewer levels than the tile-component's full decomposition count produces a
        /// reduced-resolution reconstruction (ITU-T T.800 (06/2019) Section B.5).
        /// </summary>
        public static void Inverse(JpxTileComponent component, float[] samples, int decompositionLevels, bool reversible)
        {
            if (decompositionLevels < 1)
            {
                return;
            }

            var targetResolution = component.ResolutionLevels[decompositionLevels];
            var width = targetResolution.Width;

            // Scratch buffer for interleaving a single row, reused for every row across all decomposition levels.
            var rowBuffer = new float[width];

            // Scratch buffer holding a block of columns as contiguous rows during the vertical pass.
            var columnBuffer = new float[targetResolution.Height * ColumnBlockWidth];

            for (var levelIndex = 1; levelIndex <= decompositionLevels; levelIndex++)
            {
                var resolution = component.ResolutionLevels[levelIndex];
                var previousResolution = component.ResolutionLevels[levelIndex - 1];

                var columnCount = resolution.TrX1 - resolution.TrX0;
                var rowCount = resolution.TrY1 - resolution.TrY0;
                var lowColumnCount = previousResolution.TrX1 - previousResolution.TrX0;
                var lowRowCount = previousResolution.TrY1 - previousResolution.TrY0;
                var columnParity = resolution.TrX0 & 1;
                var rowParity = resolution.TrY0 & 1;

                // Horizontal pass (Section F.3.4 HOR_SR)
                if (columnCount > 1)
                {
                    for (var y = 0; y < rowCount; y++)
                    {
                        InverseRow(samples, y * width, columnCount, lowColumnCount, columnParity, reversible,
                            rowBuffer);
                    }
                }
                else if (columnCount == 1 && columnParity == 1)
                {
                    // Section F.3.6: a one-sample 1D_SR signal sets X(i0) to Y(i0) when the origin index i0 is even,
                    // and to Y(i0)/2 when it is odd (the lone coefficient is then a high-pass sample). This rule is
                    // filter independent and carries no K normalization. An even origin needs no operation.
                    for (var y = 0; y < rowCount; y++)
                    {
                        samples[y * width] *= 0.5f;
                    }
                }

                // Vertical pass (Section F.3.5 VER_SR)
                if (rowCount > 1)
                {
                    for (var x = 0; x < columnCount; x += ColumnBlockWidth)
                    {
                        var blockWidth = Math.Min(ColumnBlockWidth, columnCount - x);
                        InverseColumns(samples, width, x, blockWidth, rowCount, lowRowCount, rowParity, reversible, columnBuffer);
                    }
                }
                else if (rowCount == 1 && rowParity == 1)
                {
                    // Section F.3.6: the one-sample rule of the horizontal pass above, applied per column of the
                    // single-row resolution level.
                    for (var x = 0; x < columnCount; x++)
                    {
                        samples[x] *= 0.5f;
                    }
                }
            }
        }

        private static void InverseRow(
            float[] samples, int offset, int count, int lowCount, int parity, bool reversible, float[] rowBuffer)
        {
            // Section F.3.6: 1D_SR, applied by HOR_SR (F.3.4)

            var highCount = count - lowCount;
            var highOffset = offset + lowCount;

            var highParity = 1 - parity;

            // Section F.3.8: 1D_FILTR
            if (reversible)
            {
                // 1D_FILTR 5-3R procedure:
                LiftRow(new Lift53LowOp(), samples, offset, lowCount, highOffset, highCount, parity);
                LiftRow(new Lift53HighOp(), samples, highOffset, highCount, offset, lowCount, highParity);
            }
            else
            {
                // 1D_FILTR 9-7I procedure: 
                /* STEP1 */
                Multiply(samples, offset, lowCount, K);
                /* STEP2 */
                Multiply(samples, highOffset, highCount, 1f / K);

                /* STEP3 */
                LiftRow(new Lift97Op(-Delta), samples, offset, lowCount, highOffset, highCount, parity);
                /* STEP4 */
                LiftRow(new Lift97Op(-Gamma), samples, highOffset, highCount, offset, lowCount, highParity);
                /* STEP5 */
                LiftRow(new Lift97Op(-Beta), samples, offset, lowCount, highOffset, highCount, parity);
                /* STEP6 */
                LiftRow(new Lift97Op(-Alpha), samples, highOffset, highCount, offset, lowCount, highParity);
            }

            InterleaveRow(samples, offset, count, lowCount, parity, rowBuffer);
        }

        private static void InverseColumns(
            float[] samples, int width, int x, int blockWidth, int count, int lowCount, int parity, bool reversible,
            float[] columnBuffer)
        {
            // Section F.3.6: 1D_SR, applied to a block of columns by VER_SR (F.3.5)

            if (blockWidth < ColumnBlockWidth)
            {
                // The lifting steps below always process full block rows. Zero-fill the unused columns so that they
                // hold well-defined values. The results in those columns are never written back to the tile.
                Array.Clear(columnBuffer, 0, count * ColumnBlockWidth);
            }

            for (var i = 0; i < count; i++)
            {
                Array.Copy(samples, x + i * width, columnBuffer, i * ColumnBlockWidth, blockWidth);
            }

            var highCount = count - lowCount;
            var highOffset = lowCount * ColumnBlockWidth;
            var highParity = 1 - parity;

            // Section F.3.8: 1D_FILTR
            if (reversible)
            {
                // 1D_FILTR 5-3R procedure:
                LiftColumns(new Lift53LowOp(), columnBuffer, 0, lowCount, highOffset, highCount, parity);
                LiftColumns(new Lift53HighOp(), columnBuffer, highOffset, highCount, 0, lowCount, highParity);
            }
            else
            {
                // 1D_FILTR 9-7I procedure: 
                /* STEP1 */
                Multiply(columnBuffer, 0, lowCount * ColumnBlockWidth, K);
                /* STEP2 */
                Multiply(columnBuffer, highOffset, highCount * ColumnBlockWidth, 1f / K);

                /* STEP3 */
                LiftColumns(new Lift97Op(-Delta), columnBuffer, 0, lowCount, highOffset, highCount, parity);
                /* STEP4 */
                LiftColumns(new Lift97Op(-Gamma), columnBuffer, highOffset, highCount, 0, lowCount, highParity);
                /* STEP5 */
                LiftColumns(new Lift97Op(-Beta), columnBuffer, 0, lowCount, highOffset, highCount, parity);
                /* STEP6 */
                LiftColumns(new Lift97Op(-Alpha), columnBuffer, highOffset, highCount, 0, lowCount, highParity);
            }

            for (var i = 0; i < lowCount; i++)
            {
                Array.Copy(columnBuffer, i * ColumnBlockWidth, samples, x + ((i << 1) + parity) * width, blockWidth);
            }

            for (var i = 0; i < highCount; i++)
            {
                Array.Copy(
                    columnBuffer, highOffset + i * ColumnBlockWidth,
                    samples, x + ((i << 1) + highParity) * width, blockWidth);
            }
        }

        private static void InterleaveRow(
            float[] samples, int offset, int count, int lowCount, int parity, float[] rowBuffer)
        {
            var highCount = count - lowCount;
            var highOffset = offset + lowCount;
            var highParity = 1 - parity;
            var i = 0;

#if NET8_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated && Avx2.IsSupported)
            {
                var pairCount = Math.Min(lowCount, highCount);
                ref var samplesRef = ref MemoryMarshal.GetArrayDataReference(samples);
                ref var bufferRef = ref MemoryMarshal.GetArrayDataReference(rowBuffer);

                for (; i + Vector256<float>.Count <= pairCount; i += Vector256<float>.Count)
                {
                    var low = Vector256.LoadUnsafe(ref samplesRef, (nuint)(offset + i));
                    var high = Vector256.LoadUnsafe(ref samplesRef, (nuint)(highOffset + i));

                    var first = parity == 0 ? low : high;
                    var second = parity == 0 ? high : low;

                    // (f0, s0, f1, s1, f4, s4, f5, s5) and (f2, s2, f3, s3, f6, s6, f7, s7)
                    var unpackedLow = Avx.UnpackLow(first, second);
                    var unpackedHigh = Avx.UnpackHigh(first, second);

                    // Combine the 128-bit halves into (f0, s0 .. f3, s3) and (f4, s4 .. f7, s7)
                    var permutedLow = Avx.Permute2x128(unpackedLow, unpackedHigh, 0x20);
                    permutedLow.StoreUnsafe(ref bufferRef, (nuint)(i << 1));

                    var permutedHigh = Avx.Permute2x128(unpackedLow, unpackedHigh, 0x31);
                    permutedHigh.StoreUnsafe(ref bufferRef, (nuint)((i << 1) + Vector256<float>.Count));
                }
            }
            else if (Vector128.IsHardwareAccelerated && (Sse.IsSupported || AdvSimd.Arm64.IsSupported))
            {
                var pairCount = Math.Min(lowCount, highCount);
                ref var samplesRef = ref MemoryMarshal.GetArrayDataReference(samples);
                ref var bufferRef = ref MemoryMarshal.GetArrayDataReference(rowBuffer);

                for (; i + Vector128<float>.Count <= pairCount; i += Vector128<float>.Count)
                {
                    var low = Vector128.LoadUnsafe(ref samplesRef, (nuint)(offset + i));
                    var high = Vector128.LoadUnsafe(ref samplesRef, (nuint)(highOffset + i));

                    var first = parity == 0 ? low : high;
                    var second = parity == 0 ? high : low;

                    // (f0, s0, f1, s1) and (f2, s2, f3, s3)
                    var unpackedLow = VectorUtils.InterleaveLow(first, second);
                    var unpackedHigh = VectorUtils.InterleaveHigh(first, second);

                    unpackedLow.StoreUnsafe(ref bufferRef, (nuint)(i << 1));
                    unpackedHigh.StoreUnsafe(ref bufferRef, (nuint)((i << 1) + Vector128<float>.Count));
                }
            }
#endif

            for (var j = i; j < lowCount; j++)
            {
                rowBuffer[(j << 1) + parity] = samples[offset + j];
            }

            for (var j = i; j < highCount; j++)
            {
                rowBuffer[(j << 1) + highParity] = samples[highOffset + j];
            }

            Array.Copy(rowBuffer, 0, samples, offset, count);
        }

        private static void LiftRow<TOp>(
            TOp op, float[] samples, int targetOffset, int targetCount, int sourceOffset, int sourceCount, int parity)
            where TOp : struct, ILiftOp
        {
            // Lifting for 1D_FILTR 5-3R and 1D_FILTR 9-7I (step 3-6)

            var interiorStart = 1 - parity;
            var interiorEnd = Math.Min(targetCount, sourceCount - parity);

            if (interiorStart > 0 && targetCount > 0)
            {
                var mirrored = samples[sourceOffset];
                samples[targetOffset] = op.Apply(samples[targetOffset], mirrored, mirrored);
            }

            var k = interiorStart;

#if NET8_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated)
            {
                ref var samplesRef = ref MemoryMarshal.GetArrayDataReference(samples);

                for (; k + Vector256<float>.Count <= interiorEnd; k += Vector256<float>.Count)
                {
                    var value = Vector256.LoadUnsafe(ref samplesRef, (nuint)(targetOffset + k));
                    var neighbor1 = Vector256.LoadUnsafe(ref samplesRef, (nuint)(sourceOffset + k + parity - 1));
                    var neighbor2 = Vector256.LoadUnsafe(ref samplesRef, (nuint)(sourceOffset + k + parity));

                    var result = op.Apply(value, neighbor1, neighbor2);

                    result.StoreUnsafe(ref samplesRef, (nuint)(targetOffset + k));
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                ref var samplesRef = ref MemoryMarshal.GetArrayDataReference(samples);

                for (; k + Vector128<float>.Count <= interiorEnd; k += Vector128<float>.Count)
                {
                    var value = Vector128.LoadUnsafe(ref samplesRef, (nuint)(targetOffset + k));
                    var neighbor1 = Vector128.LoadUnsafe(ref samplesRef, (nuint)(sourceOffset + k + parity - 1));
                    var neighbor2 = Vector128.LoadUnsafe(ref samplesRef, (nuint)(sourceOffset + k + parity));

                    var result = op.Apply(value, neighbor1, neighbor2);

                    result.StoreUnsafe(ref samplesRef, (nuint)(targetOffset + k));
                }
            }
#endif

            for (; k < interiorEnd; k++)
            {
                samples[targetOffset + k] = op.Apply(
                    samples[targetOffset + k],
                    samples[sourceOffset + k + parity - 1],
                    samples[sourceOffset + k + parity]);
            }

            for (k = Math.Max(k, interiorEnd); k < targetCount; k++)
            {
                var mirrored = samples[sourceOffset + k + parity - 1];
                samples[targetOffset + k] = op.Apply(samples[targetOffset + k], mirrored, mirrored);
            }
        }

        private static void LiftColumns<TOp>(
            TOp op, float[] columnBuffer, int targetOffset, int targetCount, int sourceOffset, int sourceCount,
            int parity)
            where TOp : struct, ILiftOp
        {
            // Lifting for 1D_FILTR 5-3R and 1D_FILTR 9-7I (step 3-6)

            var interiorStart = 1 - parity;
            var interiorEnd = Math.Min(targetCount, sourceCount - parity);

            if (interiorStart > 0 && targetCount > 0)
            {
                LiftBlockRow(op, columnBuffer, targetOffset, sourceOffset, sourceOffset);
            }

            for (var k = interiorStart; k < interiorEnd; k++)
            {
                LiftBlockRow(op, columnBuffer,
                    targetOffset + k * ColumnBlockWidth,
                    sourceOffset + (k + parity - 1) * ColumnBlockWidth,
                    sourceOffset + (k + parity) * ColumnBlockWidth);
            }

            for (var k = Math.Max(interiorStart, interiorEnd); k < targetCount; k++)
            {
                var mirroredOffset = sourceOffset + (k + parity - 1) * ColumnBlockWidth;
                LiftBlockRow(op, columnBuffer, targetOffset + k * ColumnBlockWidth, mirroredOffset, mirroredOffset);
            }
        }

        private static void LiftBlockRow<TOp>(
            TOp op, float[] columnBuffer, int targetOffset, int sourceOffset1, int sourceOffset2)
            where TOp : struct, ILiftOp
        {
#if NET8_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated)
            {
                ref var bufferRef = ref MemoryMarshal.GetArrayDataReference(columnBuffer);

                for (var c = 0; c < ColumnBlockWidth; c += Vector256<float>.Count)
                {
                    var value = Vector256.LoadUnsafe(ref bufferRef, (nuint)(targetOffset + c));
                    var neighbor1 = Vector256.LoadUnsafe(ref bufferRef, (nuint)(sourceOffset1 + c));
                    var neighbor2 = Vector256.LoadUnsafe(ref bufferRef, (nuint)(sourceOffset2 + c));

                    var result = op.Apply(value, neighbor1, neighbor2);

                    result.StoreUnsafe(ref bufferRef, (nuint)(targetOffset + c));
                }

                return;
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                ref var bufferRef = ref MemoryMarshal.GetArrayDataReference(columnBuffer);

                for (var c = 0; c < ColumnBlockWidth; c += Vector128<float>.Count)
                {
                    var value = Vector128.LoadUnsafe(ref bufferRef, (nuint)(targetOffset + c));
                    var neighbor1 = Vector128.LoadUnsafe(ref bufferRef, (nuint)(sourceOffset1 + c));
                    var neighbor2 = Vector128.LoadUnsafe(ref bufferRef, (nuint)(sourceOffset2 + c));

                    var result = op.Apply(value, neighbor1, neighbor2);

                    result.StoreUnsafe(ref bufferRef, (nuint)(targetOffset + c));
                }

                return;
            }
#endif

            for (var c = 0; c < ColumnBlockWidth; c++)
            {
                columnBuffer[targetOffset + c] = op.Apply(
                    columnBuffer[targetOffset + c],
                    columnBuffer[sourceOffset1 + c],
                    columnBuffer[sourceOffset2 + c]);
            }
        }

        private static void Multiply(float[] data, int offset, int count, float factor)
        {
            var i = offset;
            var endIndex = offset + count;

#if NET8_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated)
            {
                ref var samplesRef = ref MemoryMarshal.GetArrayDataReference(data);
                var factorVector = Vector256.Create(factor);

                for (; i + Vector256<float>.Count <= endIndex; i += Vector256<float>.Count)
                {
                    var source = Vector256.LoadUnsafe(ref samplesRef, (nuint)i);
                    var result = source * factorVector;
                    result.StoreUnsafe(ref samplesRef, (nuint)i);
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                ref var samplesRef = ref MemoryMarshal.GetArrayDataReference(data);
                var factorVector = Vector128.Create(factor);

                for (; i + Vector128<float>.Count <= endIndex; i += Vector128<float>.Count)
                {
                    var source = Vector128.LoadUnsafe(ref samplesRef, (nuint)i);
                    var result = source * factorVector;
                    result.StoreUnsafe(ref samplesRef, (nuint)i);
                }
            }
#endif

            for (; i < endIndex; i++)
            {
                data[i] *= factor;
            }
        }

        private interface ILiftOp
        {
            float Apply(float value, float neighbor1, float neighbor2);

#if NET8_0_OR_GREATER
            Vector256<float> Apply(Vector256<float> value, Vector256<float> neighbor1, Vector256<float> neighbor2);
            Vector128<float> Apply(Vector128<float> value, Vector128<float> neighbor1, Vector128<float> neighbor2);
#endif
        }

        // Equation F-5
        private readonly struct Lift53LowOp : ILiftOp
        {
            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public float Apply(float value, float neighbor1, float neighbor2)
            {
                return value - MathF.Floor((neighbor1 + neighbor2 + 2) * 0.25f);
            }

#if NET8_0_OR_GREATER
            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public Vector256<float> Apply(
                Vector256<float> value, Vector256<float> neighbor1, Vector256<float> neighbor2)
            {
                return value - Vector256.Floor((neighbor1 + neighbor2 + Vector256.Create(2f)) * 0.25f);
            }

            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public Vector128<float> Apply(
                Vector128<float> value, Vector128<float> neighbor1, Vector128<float> neighbor2)
            {
                return value - Vector128.Floor((neighbor1 + neighbor2 + Vector128.Create(2f)) * 0.25f);
            }
#endif
        }

        // Equation F-6
        private readonly struct Lift53HighOp : ILiftOp
        {
            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public float Apply(float value, float neighbor1, float neighbor2)
            {
                return value + MathF.Floor((neighbor1 + neighbor2) * 0.5f);
            }

#if NET8_0_OR_GREATER
            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public Vector256<float> Apply(
                Vector256<float> value, Vector256<float> neighbor1, Vector256<float> neighbor2)
            {
                return value + Vector256.Floor((neighbor1 + neighbor2) * 0.5f);
            }

            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public Vector128<float> Apply(
                Vector128<float> value, Vector128<float> neighbor1, Vector128<float> neighbor2)
            {
                return value + Vector128.Floor((neighbor1 + neighbor2) * 0.5f);
            }
#endif
        }

        // Equation F-7 step 3-6
        private readonly struct Lift97Op(float factor) : ILiftOp
        {
            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public float Apply(float value, float neighbor1, float neighbor2)
            {
                return value + factor * (neighbor1 + neighbor2);
            }

#if NET8_0_OR_GREATER
            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public Vector256<float> Apply(
                Vector256<float> value, Vector256<float> neighbor1, Vector256<float> neighbor2)
            {
                return value + Vector256.Create(factor) * (neighbor1 + neighbor2);
            }

            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public Vector128<float> Apply(Vector128<float> value, Vector128<float> neighbor1, Vector128<float> neighbor2)
            {
                return value + Vector128.Create(factor) * (neighbor1 + neighbor2);
            }
#endif
        }
    }
}
