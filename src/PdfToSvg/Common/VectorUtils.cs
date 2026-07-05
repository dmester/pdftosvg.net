// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

#if NET8_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
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
            // extra instructions. We don't need that handling, so use SSE directly.
            if (Sse.IsSupported)
            {
                return Sse.Min(Sse.Max(input, min), max);
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
    }
}

#endif
