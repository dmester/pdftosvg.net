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
        public static Vector128<int> ClampNative(Vector128<int> input, Vector128<int> min, Vector128<int> max)
        {
#if NET9_0_OR_GREATER
            return Vector128.ClampNative(input, min, max);
#else
            return Vector128.Min(Vector128.Max(input, min), max);
#endif
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static Vector256<int> ClampNative(Vector256<int> input, Vector256<int> min, Vector256<int> max)
        {
#if NET9_0_OR_GREATER
            return Vector256.ClampNative(input, min, max);
#else
            return Vector256.Min(Vector256.Max(input, min), max);
#endif
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static Vector128<short> ClampNative(Vector128<short> input, Vector128<short> min, Vector128<short> max)
        {
#if NET9_0_OR_GREATER
            return Vector128.ClampNative(input, min, max);
#else
            return Vector128.Min(Vector128.Max(input, min), max);
#endif
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static Vector256<short> ClampNative(Vector256<short> input, Vector256<short> min, Vector256<short> max)
        {
#if NET9_0_OR_GREATER
            return Vector256.ClampNative(input, min, max);
#else
            return Vector256.Min(Vector256.Max(input, min), max);
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

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static Vector128<int> ConvertToInt32Truncate(Vector128<float> value)
        {
            if (Sse2.IsSupported)
            {
                return Sse2.ConvertToVector128Int32WithTruncation(value);
            }
            else if (AdvSimd.IsSupported)
            {
                return AdvSimd.ConvertToInt32RoundToZero(value);
            }
            else
            {
                return Vector128.Create((int)value[0], (int)value[1], (int)value[2], (int)value[3]);
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static Vector256<int> ConvertToInt32Truncate(Vector256<float> value)
        {
            if (Avx.IsSupported)
            {
                return Avx.ConvertToVector256Int32WithTruncation(value);
            }
            else
            {
                return Vector256.Create(
                    ConvertToInt32Truncate(value.GetLower()),
                    ConvertToInt32Truncate(value.GetUpper()));
            }
        }

        public static Vector256<short> NarrowWithSaturation(Vector256<int> lower, Vector256<int> upper)
        {
#if NET10_0_OR_GREATER
            return Vector256.NarrowWithSaturation(lower, upper);
#else
            if (Avx2.IsSupported)
            {
                return Avx2.Permute4x64(
                    Avx2.PackSignedSaturate(lower, upper).AsInt64(),
                    0b_11_01_10_00
                    ).AsInt16();
            }
            else
            {
                return Vector256.Narrow(
                    ClampNative(lower, Vector256.Create<int>(short.MinValue), Vector256.Create<int>(short.MaxValue)),
                    ClampNative(upper, Vector256.Create<int>(short.MinValue), Vector256.Create<int>(short.MaxValue))
                    );
            }
#endif
        }

        public static Vector128<short> NarrowWithSaturation(Vector128<int> lower, Vector128<int> upper)
        {
#if NET10_0_OR_GREATER
            return Vector128.NarrowWithSaturation(lower, upper);
#else
            if (Sse2.IsSupported)
            {
                return Sse2.PackSignedSaturate(lower, upper);
            }
            else
            {
                return Vector128.Narrow(
                    ClampNative(lower, Vector128.Create<int>(short.MinValue), Vector128.Create<int>(short.MaxValue)),
                    ClampNative(upper, Vector128.Create<int>(short.MinValue), Vector128.Create<int>(short.MaxValue))
                    );
            }
#endif
        }

        public static Vector256<byte> NarrowWithSaturationToByte(
            Vector256<int> a, Vector256<int> b, Vector256<int> c, Vector256<int> d)
        {
            if (Avx2.IsSupported)
            {
                var ab = Avx2.PackSignedSaturate(a, b);
                var cd = Avx2.PackSignedSaturate(c, d);
                var bytes = Avx2.PackUnsignedSaturate(ab, cd);

                var order = Vector256.Create(0, 4, 1, 5, 2, 6, 3, 7);

                return Avx2.PermuteVar8x32(bytes.AsInt32(), order).AsByte();
            }
            else
            {
                var min = Vector256.Create<int>(byte.MinValue);
                var max = Vector256.Create<int>(byte.MaxValue);

                var ab = Vector256.Narrow(ClampNative(a, min, max), ClampNative(b, min, max)).AsUInt16();
                var cd = Vector256.Narrow(ClampNative(c, min, max), ClampNative(d, min, max)).AsUInt16();

                return Vector256.Narrow(ab, cd);
            }
        }

        public static Vector128<byte> NarrowWithSaturationToByte(
            Vector128<int> a, Vector128<int> b, Vector128<int> c, Vector128<int> d)
        {
            if (Sse2.IsSupported)
            {
                var ab = Sse2.PackSignedSaturate(a, b);
                var cd = Sse2.PackSignedSaturate(c, d);

                return Sse2.PackUnsignedSaturate(ab, cd);
            }
            else
            {
                var min = Vector128.Create<int>(byte.MinValue);
                var max = Vector128.Create<int>(byte.MaxValue);

                var ab = Vector128.Narrow(ClampNative(a, min, max), ClampNative(b, min, max)).AsUInt16();
                var cd = Vector128.Narrow(ClampNative(c, min, max), ClampNative(d, min, max)).AsUInt16();

                return Vector128.Narrow(ab, cd);
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
