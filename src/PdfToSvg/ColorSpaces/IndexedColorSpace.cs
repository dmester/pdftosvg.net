// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.ColorSpaces
{
    internal class IndexedColorSpace : ColorSpace, IEquatable<IndexedColorSpace>
    {
        private const int RgbComponents = 3;

        private readonly ColorSpace baseSpace;
        private readonly float[] rgbLookup;

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
            var rgbLookup = new float[colors * RgbComponents];
            var mappedColorIndex = 0;

            for (var i = 0; i < colors; i++)
            {
                var redIndex = i * RgbComponents;

                baseSpace.ToRgb(
                    mappedColors, ref mappedColorIndex,
                    out rgbLookup[redIndex + 0],
                    out rgbLookup[redIndex + 1],
                    out rgbLookup[redIndex + 2]);
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
