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

namespace PdfToSvg.Imaging.Jpeg
{
    internal static class JpegVectorUtils
    {
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static (Vector128<float> Lower, Vector128<float> Upper) ConvertToVector128Single(Vector128<short> source)
        {
            var (lo, hi) = Vector128.Widen(source);
            return (Vector128.ConvertToSingle(lo), Vector128.ConvertToSingle(hi));
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static Vector256<float> ConvertToVector256Single(Vector128<short> source)
        {
            return Vector256.ConvertToSingle(Widen(source));
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static (Vector256<float> Lower, Vector256<float> Upper) ConvertToVector256Single(Vector256<short> source)
        {
            if (Avx2.IsSupported)
            {
                return (
                    Avx.ConvertToVector256Single(Avx2.ConvertToVector256Int32(source.GetLower())),
                    Avx.ConvertToVector256Single(Avx2.ConvertToVector256Int32(source.GetUpper())));
            }
            else
            {
                var result = Vector256.Widen(source);
                return (Vector256.ConvertToSingle(result.Lower), Vector256.ConvertToSingle(result.Upper));
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static (Vector256<int> Lower, Vector256<int> Upper) Widen(Vector256<short> source)
        {
            if (Avx2.IsSupported)
            {
                return (Avx2.ConvertToVector256Int32(source.GetLower()), Avx2.ConvertToVector256Int32(source.GetUpper()));
            }
            else
            {
                return Vector256.Widen(source);
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static Vector256<int> Widen(Vector128<short> source)
        {
            if (Avx2.IsSupported)
            {
                return Avx2.ConvertToVector256Int32(source);
            }
            else
            {
                return Vector256.Widen(source.ToVector256()).Lower;
            }
        }

        /// <summary>
        /// Converts two 128 vectors containing floats to shorts, with ToEven rounding.
        /// </summary>
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static Vector128<short> ConvertToVector128Int16_Sse2(Vector128<float> lower, Vector128<float> upper)
        {
            return Sse2.PackSignedSaturate(Sse2.ConvertToVector128Int32(lower), Sse2.ConvertToVector128Int32(upper));
        }

        /// <summary>
        /// Converts two 256 vectors containing floats to shorts, with ToEven rounding.
        /// </summary>
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static Vector256<short> ConvertToVector256Int16_Avx2(Vector256<float> lower, Vector256<float> upper)
        {
            return NarrowWithSaturation_Avx2(Avx.ConvertToVector256Int32(lower), Avx.ConvertToVector256Int32(upper));
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static Vector256<short> NarrowWithSaturation_Avx2(Vector256<int> lower, Vector256<int> upper)
        {
            return Avx2.Permute4x64(Avx2.PackSignedSaturate(lower, upper).AsInt64(), 0b_11_01_10_00).AsInt16();
        }

        public static Vector128<float> ClampNative(Vector128<float> input, Vector128<float> min, Vector128<float> max)
        {
#if NET10_0_OR_GREATER
            return Vector128.ClampNative(input, min, max);
#else
            return Vector128.Max(min, Vector128.Min(input, max));
#endif
        }

        public static Vector256<float> ClampNative(Vector256<float> input, Vector256<float> min, Vector256<float> max)
        {
#if NET10_0_OR_GREATER
            return Vector256.ClampNative(input, min, max);
#else
            return Vector256.Max(min, Vector256.Min(input, max));
#endif
        }
    }
}

#endif
