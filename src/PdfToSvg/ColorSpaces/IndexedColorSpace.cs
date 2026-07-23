// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging;
using System;
using System.Runtime.CompilerServices;

namespace PdfToSvg.ColorSpaces
{
    internal class IndexedColorSpace : ColorSpace, IEquatable<IndexedColorSpace>
    {
        private const int RgbComponents = 3;

        private readonly ColorSpace baseSpace;
        private readonly byte[] rgbLookup; // 0..255

        public IndexedColorSpace(ColorSpace baseSpace, byte[] lookup) : this(baseSpace, lookup, lookup.Length) { }

        public IndexedColorSpace(ColorSpace baseSpace, byte[] lookup, int lookupLength)
        {
            if (baseSpace == null) throw new ArgumentNullException(nameof(baseSpace));
            if (lookup == null) throw new ArgumentNullException(nameof(lookup));
            if (lookupLength < 0 || lookupLength > lookup.Length) throw new ArgumentOutOfRangeException(nameof(lookupLength));

            var componentsPerSample = baseSpace.ComponentsPerSample;
            var colors = lookupLength / componentsPerSample;

            // Convert to float
            var mappedColors = new float[colors * componentsPerSample];
            for (var i = 0; i < mappedColors.Length; i++)
            {
                mappedColors[i] = lookup[i];
            }

            // Decode
            var decodeArray = baseSpace.GetDefaultDecodeArray(bitsPerComponent: 8);
            decodeArray.Decode(mappedColors);

            // Map to RGB
            var rgbLookup = new byte[colors * RgbComponents];
            var mappedColorIndex = 0;

            for (var i = 0; i < colors; i++)
            {
                var redIndex = i * RgbComponents;

                baseSpace.ToRgb(
                    mappedColors, ref mappedColorIndex,
                    out var red, out var green, out var blue);

                rgbLookup[redIndex + 0] = (byte)MathUtils.Clamp(red * 255f + 0.5f, 0f, 255f);
                rgbLookup[redIndex + 1] = (byte)MathUtils.Clamp(green * 255f + 0.5f, 0f, 255f);
                rgbLookup[redIndex + 2] = (byte)MathUtils.Clamp(blue * 255f + 0.5f, 0f, 255f);
            }

            this.baseSpace = baseSpace;
            this.ColorCount = colors;
            this.rgbLookup = rgbLookup;
        }

        public override void ToRgb(float[] input, ref int inputOffset, out float red, out float green, out float blue)
        {
            var index = (int)(input[inputOffset++] + 0.5f);
            var rgbStartIndex = index * RgbComponents;

            if (rgbStartIndex + RgbComponents <= rgbLookup.Length)
            {
                red = rgbLookup[rgbStartIndex + 0] * (1f / 255f);
                green = rgbLookup[rgbStartIndex + 1] * (1f / 255f);
                blue = rgbLookup[rgbStartIndex + 2] * (1f / 255f);
            }
            else
            {
                red = 0;
                green = 0;
                blue = 0;
            }
        }

        public override void ToRgb8(float[] input, int inputOffset, float[] rgbBuffer, int rgbBufferOffset, int sampleCount)
        {
            for (var i = 0; i < sampleCount; i++)
            {
                ToRgb(input[inputOffset + i], out var red, out var green, out var blue);

                rgbBuffer[rgbBufferOffset + 0] = red;
                rgbBuffer[rgbBufferOffset + 1] = green;
                rgbBuffer[rgbBufferOffset + 2] = blue;

                rgbBufferOffset += 3;
            }
        }

        public override void ToRgb8(float[] input, int inputOffset, byte[] rgbBuffer, int rgbBufferOffset, int sampleCount)
        {
            for (var i = 0; i < sampleCount; i++)
            {
                ToRgb(input[inputOffset + i],
                    out rgbBuffer[rgbBufferOffset + 0],
                    out rgbBuffer[rgbBufferOffset + 1],
                    out rgbBuffer[rgbBufferOffset + 2]);

                rgbBufferOffset += 3;
            }
        }

        public override void ToRgba8(float[] input, int inputOffset, byte[] rgbaBuffer, int rgbaBufferOffset, int sampleCount)
        {
            for (var i = 0; i < sampleCount; i++)
            {
                ToRgb(input[inputOffset + i],
                    out rgbaBuffer[rgbaBufferOffset + 0],
                    out rgbaBuffer[rgbaBufferOffset + 1],
                    out rgbaBuffer[rgbaBufferOffset + 2]);
                rgbaBuffer[rgbaBufferOffset + 3] = 0;

                rgbaBufferOffset += 4;
            }
        }
        
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public void ToRgb(float input, out byte red, out byte green, out byte blue)
        {
            var index = (int)(input + 0.5f);
            var rgbStartIndex = index * RgbComponents;

            if (rgbStartIndex + RgbComponents <= rgbLookup.Length)
            {
                red = rgbLookup[rgbStartIndex + 0];
                green = rgbLookup[rgbStartIndex + 1];
                blue = rgbLookup[rgbStartIndex + 2];
            }
            else
            {
                red = 0;
                green = 0;
                blue = 0;
            }
        }

        public override DecodeArray GetDefaultDecodeArray(int bitsPerComponent)
        {
            return new DecodeArray(bitsPerComponent, new[] { 0f, (1 << bitsPerComponent) - 1f });
        }

        public override int ComponentsPerSample => 1;

        public ColorSpace BaseSpace => baseSpace;

        public override float[] DefaultColor => [0f];

        public int ColorCount { get; }

        public override int GetHashCode() => baseSpace.GetHashCode() ^ rgbLookup.Length;
        public override bool Equals(object? obj) => Equals(obj as IndexedColorSpace);
        public bool Equals(IndexedColorSpace? other)
        {
            if (other == null)
            {
                return false;
            }

            if (!ReferenceEquals(this, other))
            {
                if (!other.baseSpace.Equals(baseSpace)) return false;
                if (other.rgbLookup.Length != rgbLookup.Length) return false;

                for (var i = 0; i < rgbLookup.Length && i < other.rgbLookup.Length; i++)
                {
                    if (other.rgbLookup[i] != rgbLookup[i])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override string ToString() => "Indexed";
    }
}
