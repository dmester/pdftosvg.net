// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging;
using System;
using System.Runtime.CompilerServices;

#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
#endif

namespace PdfToSvg.ColorSpaces
{
    internal class DeviceCmykColorSpace : ColorSpace, IEquatable<DeviceCmykColorSpace>
    {
        public override void ToRgb(float[] input, ref int inputOffset, out float red, out float green, out float blue)
        {
            var c = input[inputOffset++];
            var m = input[inputOffset++];
            var y = input[inputOffset++];
            var k = input[inputOffset++];

            ToRgb(c, m, y, k, out red, out green, out blue);
        }

        public override void ToRgb8(float[] input, int inputOffset, byte[] rgbBuffer, int rgbBufferOffset, int sampleCount)
        {
            const int InputComponentsPerSample = 4;
            const int OutputComponentsPerSample = 3;

            if (inputOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inputOffset));
            }
            if (rgbBufferOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rgbBufferOffset));
            }
            if (sampleCount < 0 ||
                checked(sampleCount * InputComponentsPerSample) > input.Length - inputOffset ||
                checked(sampleCount * OutputComponentsPerSample) > rgbBuffer.Length - rgbBufferOffset)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleCount));
            }

            var i = 0;

#if NET8_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated && Avx2.IsSupported)
            {
                ref var pInput = ref MemoryMarshal.GetArrayDataReference(input);
                ref var pRgbBuffer = ref MemoryMarshal.GetArrayDataReference(rgbBuffer);

                var min = Vector256<float>.Zero;
                var max = Vector256.Create(255f);
                var half = Vector256.Create(0.5f);

                var compactMask = Vector256.Create(
                    (byte)0, 1, 2, 4, 5, 6, 8, 9, 10, 12, 13, 14, 0, 0, 0, 0,
                    0, 1, 2, 4, 5, 6, 8, 9, 10, 12, 13, 14, 0, 0, 0, 0);

                // The output from each loop iteration is (r, g, b, r, g, b, 0, 0), where the 0 is overwritten in the
                // next iteration or the scalar tail.
                var upperHalfOutputOffset = Vector128<float>.Count * OutputComponentsPerSample;
                var outputSamplesPerIteration =
                    (upperHalfOutputOffset + Vector128<byte>.Count + OutputComponentsPerSample - 1) /
                    OutputComponentsPerSample;

                for (; i + outputSamplesPerIteration <= sampleCount; i += Vector256<float>.Count)
                {
                    LoadInterleavedCmyk256(ref pInput, (nuint)(inputOffset + i * InputComponentsPerSample),
                        out var c, out var m, out var y, out var k);

                    ToRgb(c, m, y, k, out var red, out var green, out var blue);

                    var red8 = VectorUtils.ConvertToInt32Truncate(VectorUtils.ClampNative(red * max + half, min, max));
                    var green8 = VectorUtils.ConvertToInt32Truncate(VectorUtils.ClampNative(green * max + half, min, max));
                    var blue8 = VectorUtils.ConvertToInt32Truncate(VectorUtils.ClampNative(blue * max + half, min, max));

                    var pixels = BitConverter.IsLittleEndian
                        ? (red8 | (green8 << 8) | (blue8 << 16)).AsByte()
                        : ((red8 << 24) | (green8 << 16) | (blue8 << 8)).AsByte();
                    var packed = Avx2.Shuffle(pixels, compactMask);

                    var outputOffset = rgbBufferOffset + i * OutputComponentsPerSample;

                    packed.GetLower().StoreUnsafe(ref pRgbBuffer, (nuint)outputOffset);
                    packed.GetUpper().StoreUnsafe(ref pRgbBuffer, (nuint)(outputOffset + upperHalfOutputOffset));
                }
            }
            else if (Vector128.IsHardwareAccelerated && (Ssse3.IsSupported || AdvSimd.Arm64.IsSupported))
            {
                ref var pInput = ref MemoryMarshal.GetArrayDataReference(input);
                ref var pRgbBuffer = ref MemoryMarshal.GetArrayDataReference(rgbBuffer);

                var min = Vector128<float>.Zero;
                var max = Vector128.Create(255f);
                var half = Vector128.Create(0.5f);

                var compactMask = Vector128.Create(
                    (byte)0, 1, 2, 4, 5, 6, 8, 9, 10, 12, 13, 14, 0, 0, 0, 0);

                // The output from each loop iteration is (r, g, b, 0), where the 0 is overwritten in the
                // next iteration or the scalar tail.
                var outputSamplesPerIteration =
                    (Vector128<byte>.Count + OutputComponentsPerSample - 1) / OutputComponentsPerSample;

                for (; i + outputSamplesPerIteration <= sampleCount; i += Vector128<float>.Count)
                {
                    LoadInterleavedCmyk128(ref pInput, (nuint)(inputOffset + i * InputComponentsPerSample),
                        out var c, out var m, out var y, out var k);

                    ToRgb(c, m, y, k, out var red, out var green, out var blue);

                    var red8 = VectorUtils.ConvertToInt32Truncate(VectorUtils.ClampNative(red * max + half, min, max));
                    var green8 = VectorUtils.ConvertToInt32Truncate(VectorUtils.ClampNative(green * max + half, min, max));
                    var blue8 = VectorUtils.ConvertToInt32Truncate(VectorUtils.ClampNative(blue * max + half, min, max));

                    var pixels = BitConverter.IsLittleEndian
                        ? (red8 | (green8 << 8) | (blue8 << 16)).AsByte()
                        : ((red8 << 24) | (green8 << 16) | (blue8 << 8)).AsByte();
                    var packed = VectorUtils.ShuffleBytes(pixels, compactMask);

                    packed.StoreUnsafe(ref pRgbBuffer, (nuint)(rgbBufferOffset + i * OutputComponentsPerSample));
                }
            }
#endif

            for (; i < sampleCount; i++)
            {
                var sampleInputOffset = inputOffset + i * InputComponentsPerSample;
                var sampleOutputOffset = rgbBufferOffset + i * OutputComponentsPerSample;

                ToRgb(
                    input[sampleInputOffset + 0],
                    input[sampleInputOffset + 1],
                    input[sampleInputOffset + 2],
                    input[sampleInputOffset + 3],
                    out var red, out var green, out var blue);

                rgbBuffer[sampleOutputOffset + 0] = ToRgb8Component(red);
                rgbBuffer[sampleOutputOffset + 1] = ToRgb8Component(green);
                rgbBuffer[sampleOutputOffset + 2] = ToRgb8Component(blue);
            }
        }

        public override void ToRgb8(float[] input, int inputOffset, float[] rgbBuffer, int rgbBufferOffset, int sampleCount)
        {
            const int InputComponentsPerSample = 4;
            const int OutputComponentsPerSample = 3;

            if (inputOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inputOffset));
            }
            if (rgbBufferOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rgbBufferOffset));
            }
            if (sampleCount < 0 ||
                checked(sampleCount * InputComponentsPerSample) > input.Length - inputOffset ||
                checked(sampleCount * OutputComponentsPerSample) > rgbBuffer.Length - rgbBufferOffset)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleCount));
            }

            var i = 0;

#if NET8_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated)
            {
                ref var pInput = ref MemoryMarshal.GetArrayDataReference(input);
                ref var pRgbBuffer = ref MemoryMarshal.GetArrayDataReference(rgbBuffer);

                var min = Vector256<float>.Zero;
                var max = Vector256.Create(255f);

                // The output from each loop iteration is (r, g, b, 0), where the 0 is overwritten in the next
                // iteration or the scalar tail.
                // The '<' condition instead of '<=' is to prevent writing outside the output buffer.
                for (; i + Vector256<float>.Count < sampleCount; i += Vector256<float>.Count)
                {
                    LoadInterleavedCmyk256(ref pInput, (nuint)(inputOffset + i * InputComponentsPerSample),
                        out var c, out var m, out var y, out var k);

                    ToRgb(c, m, y, k, out var red, out var green, out var blue);

                    red = VectorUtils.ClampNative(red * max, min, max);
                    green = VectorUtils.ClampNative(green * max, min, max);
                    blue = VectorUtils.ClampNative(blue * max, min, max);

                    var t0 = VectorUtils.InterleaveLow(red, blue);
                    var t1 = VectorUtils.InterleaveLow(green, green);
                    var t2 = VectorUtils.InterleaveHigh(red, blue);
                    var t3 = VectorUtils.InterleaveHigh(green, green);

                    var row0 = VectorUtils.InterleaveLow(t0, t1);   // r0 g0 b0 x | r4 g4 b4 x
                    var row1 = VectorUtils.InterleaveHigh(t0, t1);  // r1 g1 b1 x | r5 g5 b5 x
                    var row2 = VectorUtils.InterleaveLow(t2, t3);   // r2 g2 b2 x | r6 g6 b6 x
                    var row3 = VectorUtils.InterleaveHigh(t2, t3);  // r3 g3 b3 x | r7 g7 b7 x

                    var outputOffset = (nuint)(rgbBufferOffset + i * OutputComponentsPerSample);

                    row0.GetLower().StoreUnsafe(ref pRgbBuffer, outputOffset + 0 * OutputComponentsPerSample);
                    row1.GetLower().StoreUnsafe(ref pRgbBuffer, outputOffset + 1 * OutputComponentsPerSample);
                    row2.GetLower().StoreUnsafe(ref pRgbBuffer, outputOffset + 2 * OutputComponentsPerSample);
                    row3.GetLower().StoreUnsafe(ref pRgbBuffer, outputOffset + 3 * OutputComponentsPerSample);
                    row0.GetUpper().StoreUnsafe(ref pRgbBuffer, outputOffset + 4 * OutputComponentsPerSample);
                    row1.GetUpper().StoreUnsafe(ref pRgbBuffer, outputOffset + 5 * OutputComponentsPerSample);
                    row2.GetUpper().StoreUnsafe(ref pRgbBuffer, outputOffset + 6 * OutputComponentsPerSample);
                    row3.GetUpper().StoreUnsafe(ref pRgbBuffer, outputOffset + 7 * OutputComponentsPerSample);
                }
            }
            else if (Vector128.IsHardwareAccelerated && (Sse2.IsSupported || AdvSimd.Arm64.IsSupported))
            {
                ref var pInput = ref MemoryMarshal.GetArrayDataReference(input);
                ref var pRgbBuffer = ref MemoryMarshal.GetArrayDataReference(rgbBuffer);

                var min = Vector128<float>.Zero;
                var max = Vector128.Create(255f);

                // The output from each loop iteration is (r, g, b, 0), where the 0 is overwritten in the next
                // iteration or the scalar tail.
                // The '<' condition instead of '<=' is to prevent writing outside the output buffer.
                for (; i + Vector128<float>.Count < sampleCount; i += Vector128<float>.Count)
                {
                    LoadInterleavedCmyk128(ref pInput, (nuint)(inputOffset + i * InputComponentsPerSample),
                        out var c, out var m, out var y, out var k);

                    ToRgb(c, m, y, k, out var red, out var green, out var blue);

                    red = VectorUtils.ClampNative(red * max, min, max);
                    green = VectorUtils.ClampNative(green * max, min, max);
                    blue = VectorUtils.ClampNative(blue * max, min, max);

                    var t0 = VectorUtils.InterleaveLow(red, blue);
                    var t1 = VectorUtils.InterleaveLow(green, green);
                    var t2 = VectorUtils.InterleaveHigh(red, blue);
                    var t3 = VectorUtils.InterleaveHigh(green, green);

                    var row0 = VectorUtils.InterleaveLow(t0, t1);   // r0 g0 b0 x
                    var row1 = VectorUtils.InterleaveHigh(t0, t1);  // r1 g1 b1 x
                    var row2 = VectorUtils.InterleaveLow(t2, t3);   // r2 g2 b2 x
                    var row3 = VectorUtils.InterleaveHigh(t2, t3);  // r3 g3 b3 x

                    var outputOffset = (nuint)(rgbBufferOffset + i * OutputComponentsPerSample);

                    row0.StoreUnsafe(ref pRgbBuffer, outputOffset + 0 * OutputComponentsPerSample);
                    row1.StoreUnsafe(ref pRgbBuffer, outputOffset + 1 * OutputComponentsPerSample);
                    row2.StoreUnsafe(ref pRgbBuffer, outputOffset + 2 * OutputComponentsPerSample);
                    row3.StoreUnsafe(ref pRgbBuffer, outputOffset + 3 * OutputComponentsPerSample);
                }
            }
#endif

            for (; i < sampleCount; i++)
            {
                var sampleInputOffset = inputOffset + i * InputComponentsPerSample;
                var sampleOutputOffset = rgbBufferOffset + i * OutputComponentsPerSample;

                ToRgb(
                    input[sampleInputOffset + 0],
                    input[sampleInputOffset + 1],
                    input[sampleInputOffset + 2],
                    input[sampleInputOffset + 3],
                    out var red, out var green, out var blue);

                rgbBuffer[sampleOutputOffset + 0] = MathUtils.Clamp(red * 255f, 0f, 255f);
                rgbBuffer[sampleOutputOffset + 1] = MathUtils.Clamp(green * 255f, 0f, 255f);
                rgbBuffer[sampleOutputOffset + 2] = MathUtils.Clamp(blue * 255f, 0f, 255f);
            }
        }

        public override void ToRgba8(float[] input, int inputOffset, byte[] rgbaBuffer, int rgbaBufferOffset, int sampleCount)
        {
            const int InputComponentsPerSample = 4;
            const int OutputComponentsPerSample = 4;

            if (inputOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inputOffset));
            }
            if (rgbaBufferOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rgbaBufferOffset));
            }
            if (sampleCount < 0 ||
                checked(sampleCount * InputComponentsPerSample) > input.Length - inputOffset ||
                checked(sampleCount * OutputComponentsPerSample) > rgbaBuffer.Length - rgbaBufferOffset)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleCount));
            }

            var i = 0;

#if NET8_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated)
            {
                ref var pInput = ref MemoryMarshal.GetArrayDataReference(input);
                ref var pRgbaBuffer = ref MemoryMarshal.GetArrayDataReference(rgbaBuffer);

                var min = Vector256<float>.Zero;
                var max = Vector256.Create(255f);
                var half = Vector256.Create(0.5f);

                for (; i + 8 <= sampleCount; i += 8)
                {
                    LoadInterleavedCmyk256(ref pInput, (nuint)(inputOffset + i * InputComponentsPerSample),
                        out var c, out var m, out var y, out var k);

                    ToRgb(c, m, y, k, out var red, out var green, out var blue);

                    var red8 = VectorUtils.ConvertToInt32Truncate(VectorUtils.ClampNative(red * max + half, min, max));
                    var green8 = VectorUtils.ConvertToInt32Truncate(VectorUtils.ClampNative(green * max + half, min, max));
                    var blue8 = VectorUtils.ConvertToInt32Truncate(VectorUtils.ClampNative(blue * max + half, min, max));

                    var pixels = BitConverter.IsLittleEndian
                        ? (red8 | (green8 << 8) | (blue8 << 16)).AsByte()
                        : ((red8 << 24) | (green8 << 16) | (blue8 << 8)).AsByte();

                    pixels.StoreUnsafe(ref pRgbaBuffer, (nuint)(rgbaBufferOffset + i * OutputComponentsPerSample));
                }
            }
            else if (Vector128.IsHardwareAccelerated && (Sse2.IsSupported || AdvSimd.Arm64.IsSupported))
            {
                ref var pInput = ref MemoryMarshal.GetArrayDataReference(input);
                ref var pRgbaBuffer = ref MemoryMarshal.GetArrayDataReference(rgbaBuffer);

                var min = Vector128<float>.Zero;
                var max = Vector128.Create(255f);
                var half = Vector128.Create(0.5f);

                for (; i + 4 <= sampleCount; i += 4)
                {
                    LoadInterleavedCmyk128(ref pInput, (nuint)(inputOffset + i * InputComponentsPerSample),
                        out var c, out var m, out var y, out var k);

                    ToRgb(c, m, y, k, out var red, out var green, out var blue);

                    var red8 = VectorUtils.ConvertToInt32Truncate(VectorUtils.ClampNative(red * max + half, min, max));
                    var green8 = VectorUtils.ConvertToInt32Truncate(VectorUtils.ClampNative(green * max + half, min, max));
                    var blue8 = VectorUtils.ConvertToInt32Truncate(VectorUtils.ClampNative(blue * max + half, min, max));

                    var pixels = BitConverter.IsLittleEndian
                        ? (red8 | (green8 << 8) | (blue8 << 16)).AsByte()
                        : ((red8 << 24) | (green8 << 16) | (blue8 << 8)).AsByte();

                    pixels.StoreUnsafe(ref pRgbaBuffer, (nuint)(rgbaBufferOffset + i * OutputComponentsPerSample));
                }
            }
#endif

            for (; i < sampleCount; i++)
            {
                var sampleInputOffset = inputOffset + i * InputComponentsPerSample;
                var sampleOutputOffset = rgbaBufferOffset + i * OutputComponentsPerSample;

                ToRgb(
                    input[sampleInputOffset + 0],
                    input[sampleInputOffset + 1],
                    input[sampleInputOffset + 2],
                    input[sampleInputOffset + 3],
                    out var red, out var green, out var blue);

                rgbaBuffer[sampleOutputOffset + 0] = ToRgb8Component(red);
                rgbaBuffer[sampleOutputOffset + 1] = ToRgb8Component(green);
                rgbaBuffer[sampleOutputOffset + 2] = ToRgb8Component(blue);
                rgbaBuffer[sampleOutputOffset + 3] = 0;
            }
        }

#if NET8_0_OR_GREATER
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static void LoadInterleavedCmyk256(ref float pInput, nuint offset,
            out Vector256<float> c, out Vector256<float> m, out Vector256<float> y, out Vector256<float> k)
        {
            const int ComponentsPerSample = 4;

            var s0 = Vector256.Create(
                Vector128.LoadUnsafe(ref pInput, offset + 0 * ComponentsPerSample),
                Vector128.LoadUnsafe(ref pInput, offset + 4 * ComponentsPerSample)); // c0 m0 y0 k0 | c4 m4 y4 k4
            var s1 = Vector256.Create(
                Vector128.LoadUnsafe(ref pInput, offset + 1 * ComponentsPerSample),
                Vector128.LoadUnsafe(ref pInput, offset + 5 * ComponentsPerSample)); // c1 m1 y1 k1 | c5 m5 y5 k5
            var s2 = Vector256.Create(
                Vector128.LoadUnsafe(ref pInput, offset + 2 * ComponentsPerSample),
                Vector128.LoadUnsafe(ref pInput, offset + 6 * ComponentsPerSample)); // c2 m2 y2 k2 | c6 m6 y6 k6
            var s3 = Vector256.Create(
                Vector128.LoadUnsafe(ref pInput, offset + 3 * ComponentsPerSample),
                Vector128.LoadUnsafe(ref pInput, offset + 7 * ComponentsPerSample)); // c3 m3 y3 k3 | c7 m7 y7 k7

            var t0 = VectorUtils.InterleaveLow(s0, s2);   // c0 c2 m0 m2 | c4 c6 m4 m6
            var t1 = VectorUtils.InterleaveLow(s1, s3);   // c1 c3 m1 m3 | c5 c7 m5 m7
            var t2 = VectorUtils.InterleaveHigh(s0, s2);  // y0 y2 k0 k2 | y4 y6 k4 k6
            var t3 = VectorUtils.InterleaveHigh(s1, s3);  // y1 y3 k1 k3 | y5 y7 k5 k7

            c = VectorUtils.InterleaveLow(t0, t1);        // c0 c1 c2 c3 | c4 c5 c6 c7
            m = VectorUtils.InterleaveHigh(t0, t1);       // m0 m1 m2 m3 | m4 m5 m6 m7
            y = VectorUtils.InterleaveLow(t2, t3);        // y0 y1 y2 y3 | y4 y5 y6 y7
            k = VectorUtils.InterleaveHigh(t2, t3);       // k0 k1 k2 k3 | k4 k5 k6 k7
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static void LoadInterleavedCmyk128(ref float pInput, nuint offset,
            out Vector128<float> c, out Vector128<float> m, out Vector128<float> y, out Vector128<float> k)
        {
            const int ComponentsPerSample = 4;

            var s0 = Vector128.LoadUnsafe(ref pInput, offset + 0 * ComponentsPerSample);  // c0 m0 y0 k0
            var s1 = Vector128.LoadUnsafe(ref pInput, offset + 1 * ComponentsPerSample);  // c1 m1 y1 k1
            var s2 = Vector128.LoadUnsafe(ref pInput, offset + 2 * ComponentsPerSample);  // c2 m2 y2 k2
            var s3 = Vector128.LoadUnsafe(ref pInput, offset + 3 * ComponentsPerSample);  // c3 m3 y3 k3

            var t0 = VectorUtils.InterleaveLow(s0, s2);   // c0 c2 m0 m2
            var t1 = VectorUtils.InterleaveLow(s1, s3);   // c1 c3 m1 m3
            var t2 = VectorUtils.InterleaveHigh(s0, s2);  // y0 y2 k0 k2
            var t3 = VectorUtils.InterleaveHigh(s1, s3);  // y1 y3 k1 k3

            c = VectorUtils.InterleaveLow(t0, t1);        // c0 c1 c2 c3
            m = VectorUtils.InterleaveHigh(t0, t1);       // m0 m1 m2 m3
            y = VectorUtils.InterleaveLow(t2, t3);        // y0 y1 y2 y3
            k = VectorUtils.InterleaveHigh(t2, t3);       // k0 k1 k2 k3
        }
#endif

        public static void ToRgb(float c, float m, float y, float k, out float red, out float green, out float blue)
        {
            // Since ICC color profiles are currently not supported, we will do an approximated color conversion from CMYK to sRGB.
            //
            // The approximation is based on the samples provided in
            // "Color Characterization Data for SWOP proofing on Grade 3 coated paper", which can be found here:
            // http://www.npes.org/programs/standardsworkroom/toolsbestpractices/technicalreports.aspx
            //
            // The constants were generated with the ApproximateCmyk tool included in the tools folder.

            red = 1f +
                c * (
                    -1.0131f +
                    c * (-0.0474f + c * -0.0701f) +
                    m * 0.3576f +
                    y * 0.0042f +
                    k * 0.7488f
                ) +
                m * (
                    0.0252f +
                    m * (-0.1693f + m * 0.0704f) +
                    y * -0.0409f +
                    k * 0.0333f
                ) +
                y * (
                    0.0388f +
                    y * (0.03f + y * -0.0248f) +
                    k * 0.0028f
                ) +
                k * (
                    -0.7428f +
                    k * (-0.1789f + k * -0.0285f)
                );

            green = 1f +
                c * (
                    -0.3344f +
                    c * (0.0548f + c * -0.0408f) +
                    m * 0.3254f +
                    y * 0.0079f +
                    k * 0.185f
                ) +
                m * (
                    -0.6792f +
                    m * (-0.2034f + m * 0.0367f) +
                    y * 0.0646f +
                    k * 0.5394f
                ) +
                y * (
                    -0.0247f +
                    y * (-0.0727f + y * 0.0328f) +
                    k * 0.042f
                ) +
                k * (
                    -0.6876f +
                    k * (-0.2106f + k * -0.0134f)
                );

            blue = 1f +
                c * (
                    -0.1558f +
                    c * (0.0928f + c * -0.0163f) +
                    m * 0.0723f +
                    y * 0.1612f +
                    k * 0.0176f
                ) +
                m * (
                    -0.4502f +
                    m * (-0.0255f + m * 0.0337f) +
                    y * 0.2973f +
                    k * 0.2174f
                ) +
                y * (
                    -0.6536f +
                    y * (-0.2041f + y * 0.057f) +
                    k * 0.4823f
                ) +
                k * (
                    -0.6077f +
                    k * (-0.1838f + k * -0.1004f)
                );
        }

#if NET8_0_OR_GREATER
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void ToRgb(Vector256<float> c, Vector256<float> m, Vector256<float> y, Vector256<float> k, out Vector256<float> red, out Vector256<float> green, out Vector256<float> blue)
        {
            red = Vector256.Create(1f) +
                c * (
                    Vector256.Create(-1.0131f) +
                    c * (Vector256.Create(-0.0474f) + c * -0.0701f) +
                    m * 0.3576f +
                    y * 0.0042f +
                    k * 0.7488f
                ) +
                m * (
                    Vector256.Create(0.0252f) +
                    m * (Vector256.Create(-0.1693f) + m * 0.0704f) +
                    y * -0.0409f +
                    k * 0.0333f
                ) +
                y * (
                    Vector256.Create(0.0388f) +
                    y * (Vector256.Create(0.03f) + y * -0.0248f) +
                    k * 0.0028f
                ) +
                k * (
                    Vector256.Create(-0.7428f) +
                    k * (Vector256.Create(-0.1789f) + k * -0.0285f)
                );

            green = Vector256.Create(1f) +
                c * (
                    Vector256.Create(-0.3344f) +
                    c * (Vector256.Create(0.0548f) + c * -0.0408f) +
                    m * 0.3254f +
                    y * 0.0079f +
                    k * 0.185f
                ) +
                m * (
                    Vector256.Create(-0.6792f) +
                    m * (Vector256.Create(-0.2034f) + m * 0.0367f) +
                    y * 0.0646f +
                    k * 0.5394f
                ) +
                y * (
                    Vector256.Create(-0.0247f) +
                    y * (Vector256.Create(-0.0727f) + y * 0.0328f) +
                    k * 0.042f
                ) +
                k * (
                    Vector256.Create(-0.6876f) +
                    k * (Vector256.Create(-0.2106f) + k * -0.0134f)
                );

            blue = Vector256.Create(1f) +
                c * (
                    Vector256.Create(-0.1558f) +
                    c * (Vector256.Create(0.0928f) + c * -0.0163f) +
                    m * 0.0723f +
                    y * 0.1612f +
                    k * 0.0176f
                ) +
                m * (
                    Vector256.Create(-0.4502f) +
                    m * (Vector256.Create(-0.0255f) + m * 0.0337f) +
                    y * 0.2973f +
                    k * 0.2174f
                ) +
                y * (
                    Vector256.Create(-0.6536f) +
                    y * (Vector256.Create(-0.2041f) + y * 0.057f) +
                    k * 0.4823f
                ) +
                k * (
                    Vector256.Create(-0.6077f) +
                    k * (Vector256.Create(-0.1838f) + k * -0.1004f)
                );
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void ToRgb(Vector128<float> c, Vector128<float> m, Vector128<float> y, Vector128<float> k, out Vector128<float> red, out Vector128<float> green, out Vector128<float> blue)
        {
            red = Vector128.Create(1f) +
                c * (
                    Vector128.Create(-1.0131f) +
                    c * (Vector128.Create(-0.0474f) + c * -0.0701f) +
                    m * 0.3576f +
                    y * 0.0042f +
                    k * 0.7488f
                ) +
                m * (
                    Vector128.Create(0.0252f) +
                    m * (Vector128.Create(-0.1693f) + m * 0.0704f) +
                    y * -0.0409f +
                    k * 0.0333f
                ) +
                y * (
                    Vector128.Create(0.0388f) +
                    y * (Vector128.Create(0.03f) + y * -0.0248f) +
                    k * 0.0028f
                ) +
                k * (
                    Vector128.Create(-0.7428f) +
                    k * (Vector128.Create(-0.1789f) + k * -0.0285f)
                );

            green = Vector128.Create(1f) +
                c * (
                    Vector128.Create(-0.3344f) +
                    c * (Vector128.Create(0.0548f) + c * -0.0408f) +
                    m * 0.3254f +
                    y * 0.0079f +
                    k * 0.185f
                ) +
                m * (
                    Vector128.Create(-0.6792f) +
                    m * (Vector128.Create(-0.2034f) + m * 0.0367f) +
                    y * 0.0646f +
                    k * 0.5394f
                ) +
                y * (
                    Vector128.Create(-0.0247f) +
                    y * (Vector128.Create(-0.0727f) + y * 0.0328f) +
                    k * 0.042f
                ) +
                k * (
                    Vector128.Create(-0.6876f) +
                    k * (Vector128.Create(-0.2106f) + k * -0.0134f)
                );

            blue = Vector128.Create(1f) +
                c * (
                    Vector128.Create(-0.1558f) +
                    c * (Vector128.Create(0.0928f) + c * -0.0163f) +
                    m * 0.0723f +
                    y * 0.1612f +
                    k * 0.0176f
                ) +
                m * (
                    Vector128.Create(-0.4502f) +
                    m * (Vector128.Create(-0.0255f) + m * 0.0337f) +
                    y * 0.2973f +
                    k * 0.2174f
                ) +
                y * (
                    Vector128.Create(-0.6536f) +
                    y * (Vector128.Create(-0.2041f) + y * 0.057f) +
                    k * 0.4823f
                ) +
                k * (
                    Vector128.Create(-0.6077f) +
                    k * (Vector128.Create(-0.1838f) + k * -0.1004f)
                );
        }
#endif

        public override DecodeArray GetDefaultDecodeArray(int bitsPerComponent)
        {
            return new DecodeArray(bitsPerComponent, new[] { 0f, 1f, 0f, 1f, 0f, 1f, 0f, 1f });
        }

        public override int ComponentsPerSample => 4;

        public override float[] DefaultColor => new[] { 0f, 0f, 0f, 1f };

        public override int GetHashCode() => 559395;
        public bool Equals(DeviceCmykColorSpace? other) => other != null;
        public override bool Equals(object? obj) => obj is DeviceCmykColorSpace;

        public override string ToString() => "CMYK";
    }
}
