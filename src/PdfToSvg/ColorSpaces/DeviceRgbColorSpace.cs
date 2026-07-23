// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging;
using System;

#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
#endif

namespace PdfToSvg.ColorSpaces
{
    internal class DeviceRgbColorSpace : ColorSpace, IEquatable<DeviceRgbColorSpace>
    {
        public override void ToRgb(float[] input, ref int inputOffset, out float red, out float green, out float blue)
        {
            red = input[inputOffset++];
            green = input[inputOffset++];
            blue = input[inputOffset++];
        }

        public override void ToRgb8(
            float[] input, int inputOffset, float[] rgbBuffer, int rgbBufferOffset, int sampleCount)
        {
            var componentCount = checked(sampleCount * 3);

            if (inputOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inputOffset));
            }
            if (rgbBufferOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rgbBufferOffset));
            }
            if (componentCount < 0 ||
                componentCount > input.Length - inputOffset ||
                componentCount > rgbBuffer.Length - rgbBufferOffset)
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

                for (; i + Vector256<float>.Count <= componentCount; i += Vector256<float>.Count)
                {
                    var source = Vector256.LoadUnsafe(ref pInput, (nuint)(inputOffset + i));
                    var scaled = VectorUtils.ClampNative(source * max, min, max);
                    scaled.StoreUnsafe(ref pRgbBuffer, (nuint)(rgbBufferOffset + i));
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                ref var pInput = ref MemoryMarshal.GetArrayDataReference(input);
                ref var pRgbBuffer = ref MemoryMarshal.GetArrayDataReference(rgbBuffer);

                var min = Vector128<float>.Zero;
                var max = Vector128.Create(255f);

                for (; i + Vector128<float>.Count <= componentCount; i += Vector128<float>.Count)
                {
                    var source = Vector128.LoadUnsafe(ref pInput, (nuint)(inputOffset + i));
                    var scaled = VectorUtils.ClampNative(source * max, min, max);
                    scaled.StoreUnsafe(ref pRgbBuffer, (nuint)(rgbBufferOffset + i));
                }
            }
#endif

            for (; i < componentCount; i++)
            {
                rgbBuffer[rgbBufferOffset + i] = MathUtils.Clamp(input[inputOffset + i] * 255f, 0f, 255f);
            }
        }

        public override void ToRgb8(
            float[] input, int inputOffset, byte[] rgbBuffer, int rgbBufferOffset, int sampleCount)
        {
            var componentCount = checked(sampleCount * 3);

            if (inputOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inputOffset));
            }
            if (rgbBufferOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rgbBufferOffset));
            }
            if (componentCount < 0 ||
                componentCount > input.Length - inputOffset ||
                componentCount > rgbBuffer.Length - rgbBufferOffset)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleCount));
            }

            var i = 0;

#if NET8_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated)
            {
                ref var pInput = ref MemoryMarshal.GetArrayDataReference(input);
                ref var pRgbBuffer = ref MemoryMarshal.GetArrayDataReference(rgbBuffer);

                var max = Vector256.Create(255f);
                var half = Vector256.Create(0.5f);
                
                for (; i + Vector256<byte>.Count <= componentCount; i += Vector256<byte>.Count)
                {
                    var source0 = Vector256.LoadUnsafe(ref pInput, (nuint)(inputOffset + i + Vector256<float>.Count * 0));
                    var source1 = Vector256.LoadUnsafe(ref pInput, (nuint)(inputOffset + i + Vector256<float>.Count * 1));
                    var source2 = Vector256.LoadUnsafe(ref pInput, (nuint)(inputOffset + i + Vector256<float>.Count * 2));
                    var source3 = Vector256.LoadUnsafe(ref pInput, (nuint)(inputOffset + i + Vector256<float>.Count * 3));

                    var scaled0 = VectorUtils.ConvertToInt32Truncate(source0 * max + half);
                    var scaled1 = VectorUtils.ConvertToInt32Truncate(source1 * max + half);
                    var scaled2 = VectorUtils.ConvertToInt32Truncate(source2 * max + half);
                    var scaled3 = VectorUtils.ConvertToInt32Truncate(source3 * max + half);

                    var bytes = VectorUtils.NarrowWithSaturationToByte(scaled0, scaled1, scaled2, scaled3);

                    bytes.StoreUnsafe(ref pRgbBuffer, (nuint)(rgbBufferOffset + i));
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                ref var pInput = ref MemoryMarshal.GetArrayDataReference(input);
                ref var pRgbBuffer = ref MemoryMarshal.GetArrayDataReference(rgbBuffer);

                var max = Vector128.Create(255f);
                var half = Vector128.Create(0.5f);

                for (; i + Vector128<byte>.Count <= componentCount; i += Vector128<byte>.Count)
                {
                    var source0 = Vector128.LoadUnsafe(ref pInput, (nuint)(inputOffset + i + Vector128<float>.Count * 0));
                    var source1 = Vector128.LoadUnsafe(ref pInput, (nuint)(inputOffset + i + Vector128<float>.Count * 1));
                    var source2 = Vector128.LoadUnsafe(ref pInput, (nuint)(inputOffset + i + Vector128<float>.Count * 2));
                    var source3 = Vector128.LoadUnsafe(ref pInput, (nuint)(inputOffset + i + Vector128<float>.Count * 3));

                    var scaled0 = VectorUtils.ConvertToInt32Truncate(source0 * max + half);
                    var scaled1 = VectorUtils.ConvertToInt32Truncate(source1 * max + half);
                    var scaled2 = VectorUtils.ConvertToInt32Truncate(source2 * max + half);
                    var scaled3 = VectorUtils.ConvertToInt32Truncate(source3 * max + half);

                    var bytes = VectorUtils.NarrowWithSaturationToByte(scaled0, scaled1, scaled2, scaled3);

                    bytes.StoreUnsafe(ref pRgbBuffer, (nuint)(rgbBufferOffset + i));
                }
            }
#endif

            for (; i < componentCount; i++)
            {
                rgbBuffer[rgbBufferOffset + i] = ToRgb8Component(input[inputOffset + i]);
            }
        }

        public override void ToRgba8(
            float[] input, int inputOffset, byte[] rgbaBuffer, int rgbaBufferOffset, int sampleCount)
        {
            const int InputComponentsPerSample = 3;
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
            if (Vector256.IsHardwareAccelerated && Avx2.IsSupported)
            {
                // AVX2
                ref var pInput = ref MemoryMarshal.GetArrayDataReference(input);
                ref var pRgbaBuffer = ref MemoryMarshal.GetArrayDataReference(rgbaBuffer);

                var max = Vector256.Create(255f);
                var half = Vector256.Create(0.5f);

                var laneOrder = Vector256.Create(0, 4, 1, 3, 5, 2, 6, 7);

                const byte Zero = 0x80;

                var shuffleMask = Vector256.Create(
                    0, 1, 2, Zero,
                    3, 4, 5, Zero,
                    6, 7, 8, Zero,
                    9, 10, 11, Zero,
                    0, 1, 2, Zero,
                    3, 4, 5, Zero,
                    6, 7, 8, Zero,
                    9, 10, 11, Zero);

                var samplesPerIteration = Vector256<byte>.Count / OutputComponentsPerSample;

                for (; i + samplesPerIteration <= sampleCount; i += samplesPerIteration)
                {
                    var source0 = Vector256.LoadUnsafe(ref pInput, (nuint)(inputOffset + Vector256<float>.Count * 0));
                    var source1 = Vector256.LoadUnsafe(ref pInput, (nuint)(inputOffset + Vector256<float>.Count * 1));
                    var source2 = Vector256.LoadUnsafe(ref pInput, (nuint)(inputOffset + Vector256<float>.Count * 2));

                    var scaled0 = VectorUtils.ConvertToInt32Truncate(source0 * max + half);
                    var scaled1 = VectorUtils.ConvertToInt32Truncate(source1 * max + half);
                    var scaled2 = VectorUtils.ConvertToInt32Truncate(source2 * max + half);

                    var short01 = Avx2.PackSignedSaturate(scaled0, scaled1);
                    var short2x = Avx2.PackSignedSaturate(scaled2, Vector256<int>.Zero);

                    var packed = Avx2.PackUnsignedSaturate(short01, short2x);

                    packed = Avx2.PermuteVar8x32(packed.AsInt32(), laneOrder).AsByte();

                    var bytes = Avx2.Shuffle(packed, shuffleMask);

                    bytes.StoreUnsafe(ref pRgbaBuffer, (nuint)rgbaBufferOffset);

                    rgbaBufferOffset += samplesPerIteration * OutputComponentsPerSample;
                    inputOffset += samplesPerIteration * InputComponentsPerSample;

                }
            }
            else if (Vector128.IsHardwareAccelerated && Sse2.IsSupported && Ssse3.IsSupported)
            {
                // SSSE3
                ref var pInput = ref MemoryMarshal.GetArrayDataReference(input);
                ref var pRgbaBuffer = ref MemoryMarshal.GetArrayDataReference(rgbaBuffer);

                var max = Vector128.Create(255f);
                var half = Vector128.Create(0.5f);

                const byte Zero = 0x80;

                var shuffleMask = Vector128.Create(
                    0, 1, 2, Zero,
                    3, 4, 5, Zero,
                    6, 7, 8, Zero,
                    9, 10, 11, Zero);

                var samplesPerIteration = Vector128<byte>.Count / OutputComponentsPerSample;

                for (; i + samplesPerIteration <= sampleCount; i += samplesPerIteration)
                {
                    var source0 = Vector128.LoadUnsafe(ref pInput, (nuint)(inputOffset + Vector128<float>.Count * 0));
                    var source1 = Vector128.LoadUnsafe(ref pInput, (nuint)(inputOffset + Vector128<float>.Count * 1));
                    var source2 = Vector128.LoadUnsafe(ref pInput, (nuint)(inputOffset + Vector128<float>.Count * 2));

                    var scaled0 = VectorUtils.ConvertToInt32Truncate(source0 * max + half);
                    var scaled1 = VectorUtils.ConvertToInt32Truncate(source1 * max + half);
                    var scaled2 = VectorUtils.ConvertToInt32Truncate(source2 * max + half);

                    var short01 = Sse2.PackSignedSaturate(scaled0, scaled1);
                    var short2x = Sse2.PackSignedSaturate(scaled2, Vector128<int>.Zero);

                    var packed = Sse2.PackUnsignedSaturate(short01, short2x);

                    var bytes = Ssse3.Shuffle(packed, shuffleMask);

                    bytes.StoreUnsafe(ref pRgbaBuffer, (nuint)rgbaBufferOffset);

                    rgbaBufferOffset += samplesPerIteration * OutputComponentsPerSample;
                    inputOffset += samplesPerIteration * InputComponentsPerSample;
                }
            }
            else if (Vector128.IsHardwareAccelerated && AdvSimd.Arm64.IsSupported)
            {
                // ARM64 Adv SIMD
                ref var pInput = ref MemoryMarshal.GetArrayDataReference(input);
                ref var pRgbaBuffer = ref MemoryMarshal.GetArrayDataReference(rgbaBuffer);

                var max = Vector128.Create(255f);
                var half = Vector128.Create(0.5f);

                var indices = Vector128.Create(
                    0, 1, 2, 0xff,
                    3, 4, 5, 0xff,
                    6, 7, 8, 0xff,
                    9, 10, 11, 0xff);

                var samplesPerIteration = Vector128<byte>.Count / OutputComponentsPerSample;

                for (; i + samplesPerIteration <= sampleCount; i += samplesPerIteration)
                {
                    var source0 = Vector128.LoadUnsafe(ref pInput, (nuint)(inputOffset + Vector128<float>.Count * 0));
                    var source1 = Vector128.LoadUnsafe(ref pInput, (nuint)(inputOffset + Vector128<float>.Count * 1));
                    var source2 = Vector128.LoadUnsafe(ref pInput, (nuint)(inputOffset + Vector128<float>.Count * 2));

                    var scaled0 = VectorUtils.ConvertToInt32Truncate(source0 * max + half);
                    var scaled1 = VectorUtils.ConvertToInt32Truncate(source1 * max + half);
                    var scaled2 = VectorUtils.ConvertToInt32Truncate(source2 * max + half);

                    var short01 = AdvSimd.ExtractNarrowingSaturateUnsignedUpper(
                        AdvSimd.ExtractNarrowingSaturateUnsignedLower(scaled0),
                        scaled1);

                    var short2x = AdvSimd.ExtractNarrowingSaturateUnsignedUpper(
                        AdvSimd.ExtractNarrowingSaturateUnsignedLower(scaled2),
                        Vector128<int>.Zero);

                    var packed = AdvSimd.ExtractNarrowingSaturateUpper(
                        AdvSimd.ExtractNarrowingSaturateLower(short01),
                        short2x);

                    var bytes = AdvSimd.Arm64.VectorTableLookup(packed, indices);

                    bytes.StoreUnsafe(ref pRgbaBuffer, (nuint)rgbaBufferOffset);

                    rgbaBufferOffset += samplesPerIteration * OutputComponentsPerSample;
                    inputOffset += samplesPerIteration * InputComponentsPerSample;
                }
            }
#endif

            for (; i < sampleCount; i++)
            {
                rgbaBuffer[rgbaBufferOffset + 0] = ToRgb8Component(input[inputOffset + 0]);
                rgbaBuffer[rgbaBufferOffset + 1] = ToRgb8Component(input[inputOffset + 1]);
                rgbaBuffer[rgbaBufferOffset + 2] = ToRgb8Component(input[inputOffset + 2]);
                rgbaBuffer[rgbaBufferOffset + 3] = 0;

                rgbaBufferOffset += OutputComponentsPerSample;
                inputOffset += InputComponentsPerSample;
            }
        }

        public override DecodeArray GetDefaultDecodeArray(int bitsPerComponent)
        {
            return new DecodeArray(bitsPerComponent, new[] { 0f, 1f, 0f, 1f, 0f, 1f });
        }

        public override int ComponentsPerSample => 3;

        public override float[] DefaultColor => new[] { 0f, 0f, 0f };

        public override int GetHashCode() => 691308;
        public bool Equals(DeviceRgbColorSpace? other) => other != null;
        public override bool Equals(object? obj) => obj is DeviceRgbColorSpace;

        public override string ToString() => "RGB";
    }
}
