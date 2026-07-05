// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

#if NET8_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace PdfToSvg.Imaging.Jpeg
{
    internal static class JpegBlockUtils
    {
        private const int BlockSize = 64;
        private const int BlockSide = 8;

        public static void TransposeScalar<T>(T[] block)
        {
            if (block.Length != BlockSize)
            {
                throw new ArgumentOutOfRangeException(nameof(block), "Block must contain exactly 64 elements");
            }

            for (var x = 0; x < BlockSide; x++)
            {
                for (var y = 0; y <= x; y++)
                {
                    Swap(ref block[x * BlockSide + y], ref block[y * BlockSide + x]);
                }
            }
        }

#if NET8_0_OR_GREATER
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void TransposeAvx(float[] block)
        {
            if (block.Length != BlockSize)
            {
                throw new ArgumentOutOfRangeException(nameof(block), "Block must contain exactly 64 elements");
            }

            ref var start = ref Unsafe.As<float, Vector256<float>>(ref MemoryMarshal.GetArrayDataReference(block));

            TransposeAvx(
                ref start,
                ref Unsafe.Add(ref start, 1),
                ref Unsafe.Add(ref start, 2),
                ref Unsafe.Add(ref start, 3),
                ref Unsafe.Add(ref start, 4),
                ref Unsafe.Add(ref start, 5),
                ref Unsafe.Add(ref start, 6),
                ref Unsafe.Add(ref start, 7)
                );
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void TransposeAvx(
            ref Vector256<float> row0,
            ref Vector256<float> row1,
            ref Vector256<float> row2,
            ref Vector256<float> row3,
            ref Vector256<float> row4,
            ref Vector256<float> row5,
            ref Vector256<float> row6,
            ref Vector256<float> row7)
        {
            // Based on this answer by Z boson:
            // https://stackoverflow.com/a/25627536

            var t0 = Avx.UnpackLow(row0, row1);
            var t1 = Avx.UnpackHigh(row0, row1);
            var t2 = Avx.UnpackLow(row2, row3);
            var t3 = Avx.UnpackHigh(row2, row3);
            var t4 = Avx.UnpackLow(row4, row5);
            var t5 = Avx.UnpackHigh(row4, row5);
            var t6 = Avx.UnpackLow(row6, row7);
            var t7 = Avx.UnpackHigh(row6, row7);

            var tt0 = Avx.Shuffle(t0, t2, 0x44);
            var tt1 = Avx.Shuffle(t0, t2, 0xee);
            var tt2 = Avx.Shuffle(t1, t3, 0x44);
            var tt3 = Avx.Shuffle(t1, t3, 0xee);
            var tt4 = Avx.Shuffle(t4, t6, 0x44);
            var tt5 = Avx.Shuffle(t4, t6, 0xee);
            var tt6 = Avx.Shuffle(t5, t7, 0x44);
            var tt7 = Avx.Shuffle(t5, t7, 0xee);

            row0 = Avx.Permute2x128(tt0, tt4, 0x20);
            row1 = Avx.Permute2x128(tt1, tt5, 0x20);
            row2 = Avx.Permute2x128(tt2, tt6, 0x20);
            row3 = Avx.Permute2x128(tt3, tt7, 0x20);
            row4 = Avx.Permute2x128(tt0, tt4, 0x31);
            row5 = Avx.Permute2x128(tt1, tt5, 0x31);
            row6 = Avx.Permute2x128(tt2, tt6, 0x31);
            row7 = Avx.Permute2x128(tt3, tt7, 0x31);
        }

        public static void TransposeSse(float[] block)
        {
            if (block.Length != 64)
            {
                throw new ArgumentOutOfRangeException(nameof(block), "Block must contain exactly 64 elements");
            }

            ref var start = ref Unsafe.As<float, Vector128<float>>(ref MemoryMarshal.GetArrayDataReference(block));

            TransposeSse(
                ref Unsafe.Add(ref start, 0),
                ref Unsafe.Add(ref start, 1),
                ref Unsafe.Add(ref start, 2),
                ref Unsafe.Add(ref start, 3),
                ref Unsafe.Add(ref start, 4),
                ref Unsafe.Add(ref start, 5),
                ref Unsafe.Add(ref start, 6),
                ref Unsafe.Add(ref start, 7),
                ref Unsafe.Add(ref start, 8),
                ref Unsafe.Add(ref start, 9),
                ref Unsafe.Add(ref start, 10),
                ref Unsafe.Add(ref start, 11),
                ref Unsafe.Add(ref start, 12),
                ref Unsafe.Add(ref start, 13),
                ref Unsafe.Add(ref start, 14),
                ref Unsafe.Add(ref start, 15)
                );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TransposeSse(
            ref Vector128<float> row0_lo,
            ref Vector128<float> row0_hi,
            ref Vector128<float> row1_lo,
            ref Vector128<float> row1_hi,
            ref Vector128<float> row2_lo,
            ref Vector128<float> row2_hi,
            ref Vector128<float> row3_lo,
            ref Vector128<float> row3_hi,
            ref Vector128<float> row4_lo,
            ref Vector128<float> row4_hi,
            ref Vector128<float> row5_lo,
            ref Vector128<float> row5_hi,
            ref Vector128<float> row6_lo,
            ref Vector128<float> row6_hi,
            ref Vector128<float> row7_lo,
            ref Vector128<float> row7_hi
            )
        {
            // Blocks: 
            // | A  B |
            // | C  D |
            //
            // Transpose is:
            // | At Ct |
            // | Bt Dt |

            var row4_lo_copy = row4_lo;
            var row5_lo_copy = row5_lo;
            var row6_lo_copy = row6_lo;
            var row7_lo_copy = row7_lo;

            Transpose4x4Sse(
                row0_lo,
                row1_lo,
                row2_lo,
                row3_lo,
                out row0_lo,
                out row1_lo,
                out row2_lo,
                out row3_lo
                );

            Transpose4x4Sse(
                row4_hi,
                row5_hi,
                row6_hi,
                row7_hi,
                out row4_hi,
                out row5_hi,
                out row6_hi,
                out row7_hi
                );

            Transpose4x4Sse(
                row0_hi,
                row1_hi,
                row2_hi,
                row3_hi,
                out row4_lo,
                out row5_lo,
                out row6_lo,
                out row7_lo
                );

            Transpose4x4Sse(
                row4_lo_copy,
                row5_lo_copy,
                row6_lo_copy,
                row7_lo_copy,
                out row0_hi,
                out row1_hi,
                out row2_hi,
                out row3_hi
                );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Transpose4x4Sse(
            in Vector128<float> input0,
            in Vector128<float> input1,
            in Vector128<float> input2,
            in Vector128<float> input3,
            out Vector128<float> output0,
            out Vector128<float> output1,
            out Vector128<float> output2,
            out Vector128<float> output3
            )
        {
            // Based on example here:
            // https://www.intel.com/content/www/us/en/docs/intrinsics-guide/index.html#text=_MM_TRANSPOSE4_PS&ig_expand=6889

            var t0 = Sse.UnpackLow(input0, input1);
            var t2 = Sse.UnpackLow(input2, input3);
            var t1 = Sse.UnpackHigh(input0, input1);
            var t3 = Sse.UnpackHigh(input2, input3);

            output0 = Sse.MoveLowToHigh(t0, t2);
            output1 = Sse.MoveHighToLow(t2, t0);
            output2 = Sse.MoveLowToHigh(t1, t3);
            output3 = Sse.MoveHighToLow(t3, t1);
        }
#endif

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static bool IsSolidBlockScalar(float[] block, int offset)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (block.Length + offset < BlockSize)
            {
                throw new ArgumentOutOfRangeException(nameof(block));
            }

            var first = block[offset];

            for (var i = 1; i < BlockSize; i++)
            {
                if (block[offset + i] != first)
                {
                    return false;
                }
            }

            return true;
        }

#if NET8_0_OR_GREATER
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static bool IsSolidBlock256Unsafe(ref float start)
        {
            var comparison = Vector256.Create(start);

            return
                comparison == Vector256.LoadUnsafe(ref start, (nuint)(0 * Vector256<float>.Count)) &&
                comparison == Vector256.LoadUnsafe(ref start, (nuint)(1 * Vector256<float>.Count)) &&
                comparison == Vector256.LoadUnsafe(ref start, (nuint)(2 * Vector256<float>.Count)) &&
                comparison == Vector256.LoadUnsafe(ref start, (nuint)(3 * Vector256<float>.Count)) &&
                comparison == Vector256.LoadUnsafe(ref start, (nuint)(4 * Vector256<float>.Count)) &&
                comparison == Vector256.LoadUnsafe(ref start, (nuint)(5 * Vector256<float>.Count)) &&
                comparison == Vector256.LoadUnsafe(ref start, (nuint)(6 * Vector256<float>.Count)) &&
                comparison == Vector256.LoadUnsafe(ref start, (nuint)(7 * Vector256<float>.Count));
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static bool IsSolidBlock256Unsafe(ref Vector256<float> start)
        {
            return IsSolidBlock256Unsafe(ref Unsafe.As<Vector256<float>, float>(ref start));
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static bool IsSolidBlock128Unsafe(ref float start)
        {
            var comparison = Vector128.Create(start);

            return
                comparison == Vector128.LoadUnsafe(ref start, (nuint)(0 * Vector128<float>.Count)) &&
                comparison == Vector128.LoadUnsafe(ref start, (nuint)(1 * Vector128<float>.Count)) &&
                comparison == Vector128.LoadUnsafe(ref start, (nuint)(2 * Vector128<float>.Count)) &&
                comparison == Vector128.LoadUnsafe(ref start, (nuint)(3 * Vector128<float>.Count)) &&
                comparison == Vector128.LoadUnsafe(ref start, (nuint)(4 * Vector128<float>.Count)) &&
                comparison == Vector128.LoadUnsafe(ref start, (nuint)(5 * Vector128<float>.Count)) &&
                comparison == Vector128.LoadUnsafe(ref start, (nuint)(6 * Vector128<float>.Count)) &&
                comparison == Vector128.LoadUnsafe(ref start, (nuint)(7 * Vector128<float>.Count)) &&
                comparison == Vector128.LoadUnsafe(ref start, (nuint)(8 * Vector128<float>.Count)) &&
                comparison == Vector128.LoadUnsafe(ref start, (nuint)(9 * Vector128<float>.Count)) &&
                comparison == Vector128.LoadUnsafe(ref start, (nuint)(10 * Vector128<float>.Count)) &&
                comparison == Vector128.LoadUnsafe(ref start, (nuint)(11 * Vector128<float>.Count)) &&
                comparison == Vector128.LoadUnsafe(ref start, (nuint)(12 * Vector128<float>.Count)) &&
                comparison == Vector128.LoadUnsafe(ref start, (nuint)(13 * Vector128<float>.Count)) &&
                comparison == Vector128.LoadUnsafe(ref start, (nuint)(14 * Vector128<float>.Count)) &&
                comparison == Vector128.LoadUnsafe(ref start, (nuint)(15 * Vector128<float>.Count));
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static bool IsSolidBlock128Unsafe(ref Vector128<float> start)
        {
            return IsSolidBlock128Unsafe(ref Unsafe.As<Vector128<float>, float>(ref start));
        }
#endif

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void FillSolidBlockScalar(float[] blocks, int blockBase, float value)
        {
            for (var i = 0; i < BlockSize; i++)
            {
                blocks[blockBase + i] = value;
            }
        }

#if NET8_0_OR_GREATER
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void FillSolidBlock128Unsafe(ref Vector128<float> pStart, float value)
        {
            var rowsPerBlock = BlockSize / Vector128<float>.Count;

            for (var row = 0; row < rowsPerBlock; row++)
            {
                Unsafe.Add(ref pStart, row) = Vector128.Create(value);
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void FillSolidBlock256Unsafe(ref Vector256<float> pStart, float value)
        {
            var rowsPerBlock = BlockSize / Vector256<float>.Count;

            for (var row = 0; row < rowsPerBlock; row++)
            {
                Unsafe.Add(ref pStart, row) = Vector256.Create(value);
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void FillBlock256Unsafe(
            int[] pDestinationBlock,
            Vector256<float> row0,
            Vector256<float> row1,
            Vector256<float> row2,
            Vector256<float> row3,
            Vector256<float> row4,
            Vector256<float> row5,
            Vector256<float> row6,
            Vector256<float> row7
            )
        {
            ref var pVector0 = ref Unsafe.As<int, Vector256<int>>(ref MemoryMarshal.GetArrayDataReference(pDestinationBlock));

            Unsafe.Add(ref pVector0, 0) = Avx.ConvertToVector256Int32(row0);
            Unsafe.Add(ref pVector0, 1) = Avx.ConvertToVector256Int32(row1);
            Unsafe.Add(ref pVector0, 2) = Avx.ConvertToVector256Int32(row2);
            Unsafe.Add(ref pVector0, 3) = Avx.ConvertToVector256Int32(row3);
            Unsafe.Add(ref pVector0, 4) = Avx.ConvertToVector256Int32(row4);
            Unsafe.Add(ref pVector0, 5) = Avx.ConvertToVector256Int32(row5);
            Unsafe.Add(ref pVector0, 6) = Avx.ConvertToVector256Int32(row6);
            Unsafe.Add(ref pVector0, 7) = Avx.ConvertToVector256Int32(row7);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void FillBlock128Unsafe(
            int[] pDestinationBlock,
            Vector128<float> row0_lo, Vector128<float> row0_hi,
            Vector128<float> row1_lo, Vector128<float> row1_hi,
            Vector128<float> row2_lo, Vector128<float> row2_hi,
            Vector128<float> row3_lo, Vector128<float> row3_hi,
            Vector128<float> row4_lo, Vector128<float> row4_hi,
            Vector128<float> row5_lo, Vector128<float> row5_hi,
            Vector128<float> row6_lo, Vector128<float> row6_hi,
            Vector128<float> row7_lo, Vector128<float> row7_hi
            )
        {
            ref var pVector0 = ref Unsafe.As<int, Vector128<int>>(ref MemoryMarshal.GetArrayDataReference(pDestinationBlock));

            Unsafe.Add(ref pVector0, 0) = Sse2.ConvertToVector128Int32(row0_lo);
            Unsafe.Add(ref pVector0, 1) = Sse2.ConvertToVector128Int32(row0_hi);
            Unsafe.Add(ref pVector0, 2) = Sse2.ConvertToVector128Int32(row1_lo);
            Unsafe.Add(ref pVector0, 3) = Sse2.ConvertToVector128Int32(row1_hi);
            Unsafe.Add(ref pVector0, 4) = Sse2.ConvertToVector128Int32(row2_lo);
            Unsafe.Add(ref pVector0, 5) = Sse2.ConvertToVector128Int32(row2_hi);
            Unsafe.Add(ref pVector0, 6) = Sse2.ConvertToVector128Int32(row3_lo);
            Unsafe.Add(ref pVector0, 7) = Sse2.ConvertToVector128Int32(row3_hi);
            Unsafe.Add(ref pVector0, 8) = Sse2.ConvertToVector128Int32(row4_lo);
            Unsafe.Add(ref pVector0, 9) = Sse2.ConvertToVector128Int32(row4_hi);
            Unsafe.Add(ref pVector0, 10) = Sse2.ConvertToVector128Int32(row5_lo);
            Unsafe.Add(ref pVector0, 11) = Sse2.ConvertToVector128Int32(row5_hi);
            Unsafe.Add(ref pVector0, 12) = Sse2.ConvertToVector128Int32(row6_lo);
            Unsafe.Add(ref pVector0, 13) = Sse2.ConvertToVector128Int32(row6_hi);
            Unsafe.Add(ref pVector0, 14) = Sse2.ConvertToVector128Int32(row7_lo);
            Unsafe.Add(ref pVector0, 15) = Sse2.ConvertToVector128Int32(row7_hi);
        }
#endif

        private static void Swap<T>(ref T a, ref T b)
        {
            var temp = a;
            a = b;
            b = temp;
        }
    }
}
