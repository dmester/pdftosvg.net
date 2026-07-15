// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace PdfToSvg.Common
{
    internal static class VectorUtils
    {
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static Vector128<float> ClampNative(Vector128<float> input, Vector128<float> min, Vector128<float> max)
        {
#if NET9_0_OR_GREATER
            return Vector128.ClampNative(input, min, max);
#else
            // .NET 9 changed Vector128.Min/Max to IEEE 754:2019 semantics (NaN propagation, -0 < +0), which costs
            // extra instructions. We don't need that handling, so use SSE/AdvSimd directly.
            if (Sse.IsSupported)
            {
                return Sse.Min(Sse.Max(input, min), max);
            }
            else if (AdvSimd.IsSupported)
            {
                return AdvSimd.Min(AdvSimd.Max(input, min), max);
            }
            else
            {
                return Vector128.Min(Vector128.Max(input, min), max);
            }
#endif
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static Vector256<float> ClampNative(Vector256<float> input, Vector256<float> min, Vector256<float> max)
        {
#if NET9_0_OR_GREATER
            return Vector256.ClampNative(input, min, max);
#else
            // .NET 9 changed Vector256.Min/Max to IEEE 754:2019 semantics (NaN propagation, -0 < +0), which costs
            // extra instructions. We don't need that handling, so use AVX directly.
            if (Avx.IsSupported)
            {
                return Avx.Min(Avx.Max(input, min), max);
            }
            else
            {
                return Vector256.Min(Vector256.Max(input, min), max);
            }
#endif
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static Vector128<int> ConvertToInt32RoundToEven(Vector128<float> value)
        {
            if (Sse2.IsSupported)
            {
                return Sse2.ConvertToVector128Int32(value);
            }
            else if (AdvSimd.IsSupported)
            {
                return AdvSimd.ConvertToInt32RoundToEven(value);
            }
            else
            {
                return Vector128.Create(
                    (int)MathF.Round(value[0]),
                    (int)MathF.Round(value[1]),
                    (int)MathF.Round(value[2]),
                    (int)MathF.Round(value[3]));
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static Vector256<int> ConvertToInt32RoundToEven(Vector256<float> value)
        {
            if (Avx.IsSupported)
            {
                return Avx.ConvertToVector256Int32(value);
            }
            else if (Sse2.IsSupported)
            {
                return Vector256.Create(
                    Sse2.ConvertToVector128Int32(value.GetLower()),
                    Sse2.ConvertToVector128Int32(value.GetUpper()));
            }
            else if (AdvSimd.IsSupported)
            {
                return Vector256.Create(
                    AdvSimd.ConvertToInt32RoundToEven(value.GetLower()),
                    AdvSimd.ConvertToInt32RoundToEven(value.GetUpper()));
            }
            else
            {
                return Vector256.Create(
                    (int)MathF.Round(value[0]),
                    (int)MathF.Round(value[1]),
                    (int)MathF.Round(value[2]),
                    (int)MathF.Round(value[3]),
                    (int)MathF.Round(value[4]),
                    (int)MathF.Round(value[5]),
                    (int)MathF.Round(value[6]),
                    (int)MathF.Round(value[7]));
            }
        }

        /// <summary>
        /// Interleaves the elements of the lower halves of <paramref name="a"/> and <paramref name="b"/>,
        /// producing (a0, b0, a1, b1).
        /// </summary>
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static Vector128<float> InterleaveLow(Vector128<float> a, Vector128<float> b)
        {
            if (Sse.IsSupported)
            {
                return Sse.UnpackLow(a, b);
            }
            else if (AdvSimd.Arm64.IsSupported)
            {
                return AdvSimd.Arm64.ZipLow(a, b);
            }
            else
            {
                return Vector128.Create(a[0], b[0], a[1], b[1]);
            }
        }

        /// <summary>
        /// Interleaves the elements of the upper halves of <paramref name="a"/> and <paramref name="b"/>,
        /// producing (a2, b2, a3, b3).
        /// </summary>
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static Vector128<float> InterleaveHigh(Vector128<float> a, Vector128<float> b)
        {
            if (Sse.IsSupported)
            {
                return Sse.UnpackHigh(a, b);
            }
            else if (AdvSimd.Arm64.IsSupported)
            {
                return AdvSimd.Arm64.ZipHigh(a, b);
            }
            else
            {
                return Vector128.Create(a[2], b[2], a[3], b[3]);
            }
        }
    }
}

#endif
