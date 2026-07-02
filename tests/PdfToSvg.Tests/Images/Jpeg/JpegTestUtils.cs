// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

#if NET7_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace PdfToSvg.Tests.Images.Jpeg
{
    internal static class JpegTestUtils
    {
#if NET7_0_OR_GREATER
        public delegate void ArrayCall<T>(
            T[] input,
            T[] output
            ) where T : struct;

        public delegate void RefCall_128_8<T>(
            ref Vector128<T> row0,
            ref Vector128<T> row1,
            ref Vector128<T> row2,
            ref Vector128<T> row3,
            ref Vector128<T> row4,
            ref Vector128<T> row5,
            ref Vector128<T> row6,
            ref Vector128<T> row7
            ) where T : struct;

        public delegate void RefCall_128_16<T>(
            ref Vector128<T> row0,
            ref Vector128<T> row1,
            ref Vector128<T> row2,
            ref Vector128<T> row3,
            ref Vector128<T> row4,
            ref Vector128<T> row5,
            ref Vector128<T> row6,
            ref Vector128<T> row7,
            ref Vector128<T> row8,
            ref Vector128<T> row9,
            ref Vector128<T> row10,
            ref Vector128<T> row11,
            ref Vector128<T> row12,
            ref Vector128<T> row13,
            ref Vector128<T> row14,
            ref Vector128<T> row15
            ) where T : struct;

        public delegate void RefCall_256_4<T>(
            ref Vector256<T> row0,
            ref Vector256<T> row1,
            ref Vector256<T> row2,
            ref Vector256<T> row3
            ) where T : struct;

        public delegate void OutCall_256_4<T>(
            Vector256<T> input0,
            Vector256<T> input1,
            Vector256<T> input2,
            Vector256<T> input3,
            out Vector256<T> output0,
            out Vector256<T> output1,
            out Vector256<T> output2,
            out Vector256<T> output3
            ) where T : struct;

        public delegate void RefCall_256_8<T>(
            ref Vector256<T> row0,
            ref Vector256<T> row1,
            ref Vector256<T> row2,
            ref Vector256<T> row3,
            ref Vector256<T> row4,
            ref Vector256<T> row5,
            ref Vector256<T> row6,
            ref Vector256<T> row7
            ) where T : struct;

        public delegate void OutCall_256_8<T>(
            Vector256<T> input0,
            Vector256<T> input1,
            Vector256<T> input2,
            Vector256<T> input3,
            Vector256<T> input4,
            Vector256<T> input5,
            Vector256<T> input6,
            Vector256<T> input7,
            out Vector256<T> output0,
            out Vector256<T> output1,
            out Vector256<T> output2,
            out Vector256<T> output3,
            out Vector256<T> output4,
            out Vector256<T> output5,
            out Vector256<T> output6,
            out Vector256<T> output7
            ) where T : struct;

        public static void Apply<T>(ArrayCall<T> method, T[] data) where T : struct
        {
            var input = (T[])data.Clone();
            method(input, data);
        }

        public static void Apply<T>(OutCall_256_4<T> method, T[] data) where T : struct
        {
            ref var pData = ref Unsafe.As<T, Vector256<T>>(ref MemoryMarshal.GetArrayDataReference(data));

            method(
                pData,
                Unsafe.Add(ref pData, 1),
                Unsafe.Add(ref pData, 2),
                Unsafe.Add(ref pData, 3),
                out pData,
                out Unsafe.Add(ref pData, 1),
                out Unsafe.Add(ref pData, 2),
                out Unsafe.Add(ref pData, 3)
                );
        }

        public static void Apply<T>(RefCall_128_8<T> method, T[] data) where T : struct
        {
            ref var pData = ref Unsafe.As<T, Vector128<T>>(ref MemoryMarshal.GetArrayDataReference(data));

            method(
                ref pData,
                ref Unsafe.Add(ref pData, 1),
                ref Unsafe.Add(ref pData, 2),
                ref Unsafe.Add(ref pData, 3),
                ref Unsafe.Add(ref pData, 4),
                ref Unsafe.Add(ref pData, 5),
                ref Unsafe.Add(ref pData, 6),
                ref Unsafe.Add(ref pData, 7)
                );
        }

        public static void Apply<T>(RefCall_128_16<T> method, T[] data) where T : struct
        {
            ref var pData = ref Unsafe.As<T, Vector128<T>>(ref MemoryMarshal.GetArrayDataReference(data));

            method(
                ref pData,
                ref Unsafe.Add(ref pData, 1),
                ref Unsafe.Add(ref pData, 2),
                ref Unsafe.Add(ref pData, 3),
                ref Unsafe.Add(ref pData, 4),
                ref Unsafe.Add(ref pData, 5),
                ref Unsafe.Add(ref pData, 6),
                ref Unsafe.Add(ref pData, 7),
                ref Unsafe.Add(ref pData, 8),
                ref Unsafe.Add(ref pData, 9),
                ref Unsafe.Add(ref pData, 10),
                ref Unsafe.Add(ref pData, 11),
                ref Unsafe.Add(ref pData, 12),
                ref Unsafe.Add(ref pData, 13),
                ref Unsafe.Add(ref pData, 14),
                ref Unsafe.Add(ref pData, 15)
                );
        }

        public static void Apply<T>(RefCall_256_4<T> method, T[] data) where T : struct
        {
            ref var pData = ref Unsafe.As<T, Vector256<T>>(ref MemoryMarshal.GetArrayDataReference(data));

            method(
                ref pData,
                ref Unsafe.Add(ref pData, 1),
                ref Unsafe.Add(ref pData, 2),
                ref Unsafe.Add(ref pData, 3)
                );
        }

        public static void Apply<T>(OutCall_256_8<T> method, T[] data) where T : struct
        {
            ref var pData = ref Unsafe.As<T, Vector256<T>>(ref MemoryMarshal.GetArrayDataReference(data));

            method(
                pData,
                Unsafe.Add(ref pData, 1),
                Unsafe.Add(ref pData, 2),
                Unsafe.Add(ref pData, 3),
                Unsafe.Add(ref pData, 4),
                Unsafe.Add(ref pData, 5),
                Unsafe.Add(ref pData, 6),
                Unsafe.Add(ref pData, 7),
                out pData,
                out Unsafe.Add(ref pData, 1),
                out Unsafe.Add(ref pData, 2),
                out Unsafe.Add(ref pData, 3),
                out Unsafe.Add(ref pData, 4),
                out Unsafe.Add(ref pData, 5),
                out Unsafe.Add(ref pData, 6),
                out Unsafe.Add(ref pData, 7)
                );
        }

        public static void Apply<T>(RefCall_256_8<T> method, T[] data) where T : struct
        {
            ref var pData = ref Unsafe.As<T, Vector256<T>>(ref MemoryMarshal.GetArrayDataReference(data));

            method(
                ref Unsafe.Add(ref pData, 0),
                ref Unsafe.Add(ref pData, 1),
                ref Unsafe.Add(ref pData, 2),
                ref Unsafe.Add(ref pData, 3),
                ref Unsafe.Add(ref pData, 4),
                ref Unsafe.Add(ref pData, 5),
                ref Unsafe.Add(ref pData, 6),
                ref Unsafe.Add(ref pData, 7)
                );
        }
#endif
    }
}
