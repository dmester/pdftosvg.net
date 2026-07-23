// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PdfToSvg.ColorSpaces
{
    /// <summary>
    /// Applies an image's decode array and converts the decoded components to RGB.
    /// </summary>
    internal readonly struct ColorConversionContext
    {
        private readonly ColorSpace colorSpace;
        private readonly DecodeArray decodeArray;
        private readonly CachedColorConverter? cachedConverter;

        public int ComponentsPerSample => colorSpace.ComponentsPerSample;

        public ColorConversionContext(ColorSpace colorSpace, DecodeArray decodeArray)
        {
            this.colorSpace = colorSpace ?? throw new ArgumentNullException(nameof(colorSpace));
            this.decodeArray = decodeArray ?? throw new ArgumentNullException(nameof(decodeArray));
            this.cachedConverter = null;
        }

        private ColorConversionContext(
            ColorSpace colorSpace,
            DecodeArray decodeArray,
            CachedColorConverter cachedConverter)
        {
            this.colorSpace = colorSpace;
            this.decodeArray = decodeArray;
            this.cachedConverter = cachedConverter;
        }

        public static ColorConversionContext Create(
            ColorSpace colorSpace,
            int bitsPerComponent,
            DecodeArray decodeArray)
        {
            if (bitsPerComponent >= 1 && bitsPerComponent <= 8)
            {
                if (colorSpace.ComponentsPerSample == 1)
                {
                    return new ColorConversionContext(
                        colorSpace,
                        decodeArray,
                        new CachedColorConverter1(colorSpace, bitsPerComponent, decodeArray));
                }

                if (colorSpace.ComponentsPerSample >= 2 && colorSpace.ComponentsPerSample <= 4)
                {
                    return new ColorConversionContext(
                        colorSpace,
                        decodeArray,
                        new CachedColorConverter24(colorSpace, bitsPerComponent, decodeArray));
                }
            }

            return new ColorConversionContext(colorSpace, decodeArray);
        }

        public void DecodeToRgb8(
            float[] input,
            int inputOffset,
            byte[] rgbBuffer,
            int rgbBufferOffset,
            int sampleCount)
        {
            if (cachedConverter != null)
            {
                cachedConverter.DecodeToRgb8(input, inputOffset, rgbBuffer, rgbBufferOffset, sampleCount);
            }
            else
            {
                decodeArray.Decode(input, inputOffset, sampleCount * colorSpace.ComponentsPerSample);
                colorSpace.ToRgb8(input, inputOffset, rgbBuffer, rgbBufferOffset, sampleCount);
            }
        }

        public void DecodeToRgb8(
            float[] input,
            int inputOffset,
            float[] rgbBuffer,
            int rgbBufferOffset,
            int sampleCount)
        {
            if (cachedConverter != null)
            {
                cachedConverter.DecodeToRgb8(input, inputOffset, rgbBuffer, rgbBufferOffset, sampleCount);
            }
            else
            {
                decodeArray.Decode(input, inputOffset, sampleCount * colorSpace.ComponentsPerSample);
                colorSpace.ToRgb8(input, inputOffset, rgbBuffer, rgbBufferOffset, sampleCount);
            }
        }

        public void DecodeToRgba8(
            float[] input,
            int inputOffset,
            byte[] rgbaBuffer,
            int rgbaBufferOffset,
            int sampleCount)
        {
            if (cachedConverter != null)
            {
                cachedConverter.DecodeToRgba8(input, inputOffset, rgbaBuffer, rgbaBufferOffset, sampleCount);
            }
            else
            {
                decodeArray.Decode(input, inputOffset, sampleCount * colorSpace.ComponentsPerSample);
                colorSpace.ToRgba8(input, inputOffset, rgbaBuffer, rgbaBufferOffset, sampleCount);
            }
        }

        private abstract class CachedColorConverter
        {
            public abstract void DecodeToRgb8(
                float[] input,
                int inputOffset,
                byte[] rgbBuffer,
                int rgbBufferOffset,
                int sampleCount);

            public abstract void DecodeToRgb8(
                float[] input,
                int inputOffset,
                float[] rgbBuffer,
                int rgbBufferOffset,
                int sampleCount);

            public abstract void DecodeToRgba8(
                float[] input,
                int inputOffset,
                byte[] rgbaBuffer,
                int rgbaBufferOffset,
                int sampleCount);
        }

        private sealed class CachedColorConverter1 : CachedColorConverter
        {
            [DebuggerDisplay("{DebugView,nq}")]
            private readonly struct CachedColor
            {
                public readonly byte R;
                public readonly byte G;
                public readonly byte B;
                public readonly bool Populated;

                public CachedColor(float red, float green, float blue)
                {
                    R = ToRgb8Component(red);
                    G = ToRgb8Component(green);
                    B = ToRgb8Component(blue);
                    Populated = true;
                }

                public string DebugView => Populated ? "(" + R + ", " + G + ", " + B + ")" : "<empty>";
            }

            private readonly ColorSpace colorSpace;
            private readonly DecodeArray decodeArray;
            private readonly CachedColor[] lookupTable;
            private readonly float[] singleSlot = new float[1];
            private readonly int inputMaxValue;

            public CachedColorConverter1(ColorSpace colorSpace, int bitsPerComponent, DecodeArray decodeArray)
            {
                if (colorSpace.ComponentsPerSample != 1)
                {
                    throw new ArgumentException(
                        "Only color spaces with a single component per sample are supported", nameof(colorSpace));
                }

                this.colorSpace = colorSpace;
                this.decodeArray = decodeArray;
                this.inputMaxValue = (1 << bitsPerComponent) - 1;
                this.lookupTable = new CachedColor[inputMaxValue + 1];
            }

            public override void DecodeToRgb8(
                float[] input,
                int inputOffset,
                byte[] rgbBuffer,
                int rgbBufferOffset,
                int sampleCount)
            {
                for (var i = 0; i < sampleCount; i++)
                {
                    var color = GetColor(input[inputOffset++]);

                    rgbBuffer[rgbBufferOffset + 0] = color.R;
                    rgbBuffer[rgbBufferOffset + 1] = color.G;
                    rgbBuffer[rgbBufferOffset + 2] = color.B;
                    rgbBufferOffset += 3;
                }
            }

            public override void DecodeToRgb8(
                float[] input,
                int inputOffset,
                float[] rgbBuffer,
                int rgbBufferOffset,
                int sampleCount)
            {
                for (var i = 0; i < sampleCount; i++)
                {
                    var color = GetColor(input[inputOffset++]);

                    rgbBuffer[rgbBufferOffset + 0] = color.R;
                    rgbBuffer[rgbBufferOffset + 1] = color.G;
                    rgbBuffer[rgbBufferOffset + 2] = color.B;
                    rgbBufferOffset += 3;
                }
            }

            public override void DecodeToRgba8(
                float[] input,
                int inputOffset,
                byte[] rgbaBuffer,
                int rgbaBufferOffset,
                int sampleCount)
            {
                for (var i = 0; i < sampleCount; i++)
                {
                    var color = GetColor(input[inputOffset++]);

                    rgbaBuffer[rgbaBufferOffset + 0] = color.R;
                    rgbaBuffer[rgbaBufferOffset + 1] = color.G;
                    rgbaBuffer[rgbaBufferOffset + 2] = color.B;
                    rgbaBuffer[rgbaBufferOffset + 3] = 0;
                    rgbaBufferOffset += 4;
                }
            }

            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            private CachedColor GetColor(float input)
            {
                var cacheKey = GetCacheKey(input, inputMaxValue);
                ref var cached = ref lookupTable[cacheKey];

                if (!cached.Populated)
                {
                    var singleSlotIndex = 0;
                    singleSlot[0] = cacheKey;
                    decodeArray.Decode(singleSlot, 0, 1);
                    colorSpace.ToRgb(singleSlot, ref singleSlotIndex, out var red, out var green, out var blue);
                    cached = new CachedColor(red, green, blue);
                }

                return cached;
            }
        }

        private sealed class CachedColorConverter24 : CachedColorConverter
        {
            [DebuggerDisplay("{DebugView,nq}")]
            private readonly struct CachedColor
            {
                public readonly byte R;
                public readonly byte G;
                public readonly byte B;

                public CachedColor(float red, float green, float blue)
                {
                    R = ToRgb8Component(red);
                    G = ToRgb8Component(green);
                    B = ToRgb8Component(blue);
                }

                public string DebugView => "(" + R + ", " + G + ", " + B + ")";
            }

            private const int MaxSize = 8192;

            private readonly ColorSpace colorSpace;
            private readonly DecodeArray decodeArray;
            private readonly Dictionary<uint, CachedColor> lookupTable = new();
            private readonly float[] singleSlot;
            private readonly int inputMaxValue;

            public CachedColorConverter24(ColorSpace colorSpace, int bitsPerComponent, DecodeArray decodeArray)
            {
                if (colorSpace.ComponentsPerSample < 2 || colorSpace.ComponentsPerSample > 4)
                {
                    throw new ArgumentException(
                        "Only color spaces with 2-4 components per sample are supported", nameof(colorSpace));
                }

                this.colorSpace = colorSpace;
                this.decodeArray = decodeArray;
                this.singleSlot = new float[colorSpace.ComponentsPerSample];
                this.inputMaxValue = (1 << bitsPerComponent) - 1;
            }

            public override void DecodeToRgb8(
                float[] input,
                int inputOffset,
                byte[] rgbBuffer,
                int rgbBufferOffset,
                int sampleCount)
            {
                var componentsPerSample = colorSpace.ComponentsPerSample;

                for (var i = 0; i < sampleCount; i++)
                {
                    var color = GetColor(input, inputOffset, componentsPerSample);

                    rgbBuffer[rgbBufferOffset++] = color.R;
                    rgbBuffer[rgbBufferOffset++] = color.G;
                    rgbBuffer[rgbBufferOffset++] = color.B;
                    inputOffset += componentsPerSample;
                }
            }

            public override void DecodeToRgb8(
                float[] input,
                int inputOffset,
                float[] rgbBuffer,
                int rgbBufferOffset,
                int sampleCount)
            {
                var componentsPerSample = colorSpace.ComponentsPerSample;

                for (var i = 0; i < sampleCount; i++)
                {
                    var color = GetColor(input, inputOffset, componentsPerSample);

                    rgbBuffer[rgbBufferOffset++] = color.R;
                    rgbBuffer[rgbBufferOffset++] = color.G;
                    rgbBuffer[rgbBufferOffset++] = color.B;
                    inputOffset += componentsPerSample;
                }
            }

            public override void DecodeToRgba8(
                float[] input,
                int inputOffset,
                byte[] rgbaBuffer,
                int rgbaBufferOffset,
                int sampleCount)
            {
                var componentsPerSample = colorSpace.ComponentsPerSample;

                for (var i = 0; i < sampleCount; i++)
                {
                    var color = GetColor(input, inputOffset, componentsPerSample);

                    rgbaBuffer[rgbaBufferOffset++] = color.R;
                    rgbaBuffer[rgbaBufferOffset++] = color.G;
                    rgbaBuffer[rgbaBufferOffset++] = color.B;
                    rgbaBuffer[rgbaBufferOffset++] = 0;
                    inputOffset += componentsPerSample;
                }
            }

            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            private CachedColor GetColor(float[] input, int inputOffset, int componentsPerSample)
            {
                var cacheKey = GetCacheKey(input, inputOffset, componentsPerSample, inputMaxValue);

                if (!lookupTable.TryGetValue(cacheKey, out var cachedColor))
                {
                    var singleSlot = this.singleSlot;

                    for (var i = 0; i < componentsPerSample; i++)
                    {
                        singleSlot[i] = ColorConversionContext.GetCacheKey(input[inputOffset + i], inputMaxValue);
                    }

                    decodeArray.Decode(singleSlot, 0, componentsPerSample);
                    var singleSlotIndex = 0;
                    colorSpace.ToRgb(singleSlot, ref singleSlotIndex, out var red, out var green, out var blue);
                    cachedColor = new CachedColor(red, green, blue);

                    if (lookupTable.Count >= MaxSize)
                    {
                        lookupTable.Clear();
                    }

                    lookupTable[cacheKey] = cachedColor;
                }

                return cachedColor;
            }

            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            private static uint GetCacheKey(
                float[] input,
                int inputOffset,
                int componentsPerSample,
                int inputMaxValue)
            {
                var result = 0u;

                for (var i = 0; i < componentsPerSample; i++)
                {
                    result = (result << 8) |
                        (uint)ColorConversionContext.GetCacheKey(input[inputOffset + i], inputMaxValue);
                }

                return result;
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static int GetCacheKey(float value, int inputMaxValue)
        {
            return MathUtils.Clamp((int)(value + 0.5f), 0, inputMaxValue);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static byte ToRgb8Component(float value)
        {
            return unchecked((byte)MathUtils.Clamp((int)(value * 255f + 0.5f), 0, 255));
        }
    }
}
