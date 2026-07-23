// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.ColorSpaces
{
    internal class DeviceGrayColorSpace : ColorSpace, IEquatable<DeviceGrayColorSpace>
    {
        public override void ToRgb(float[] input, ref int inputOffset, out float red, out float green, out float blue)
        {
            var value = input[inputOffset++];
            red = value;
            green = value;
            blue = value;
        }

        public override void ToRgb8(float[] input, int inputOffset, byte[] rgbBuffer, int rgbBufferOffset, int sampleCount)
        {
            for (var i = 0; i < sampleCount; i++)
            {
                var gray = input[inputOffset++];
                var scaled = ToRgb8Component(gray);

                rgbBuffer[rgbBufferOffset + 0] = scaled;
                rgbBuffer[rgbBufferOffset + 1] = scaled;
                rgbBuffer[rgbBufferOffset + 2] = scaled;

                rgbBufferOffset += 3;
            }
        }

        public override void ToRgb8(float[] input, int inputOffset, float[] rgbBuffer, int rgbBufferOffset, int sampleCount)
        {
            for (var i = 0; i < sampleCount; i++)
            {
                var gray = input[inputOffset++];
                var scaled = MathUtils.Clamp(gray * 255f, 0f, 255f);

                rgbBuffer[rgbBufferOffset + 0] = scaled;
                rgbBuffer[rgbBufferOffset + 1] = scaled;
                rgbBuffer[rgbBufferOffset + 2] = scaled;

                rgbBufferOffset += 3;
            }
        }

        public override void ToRgba8(float[] input, int inputOffset, byte[] rgbaBuffer, int rgbaBufferOffset, int sampleCount)
        {
            for (var i = 0; i < sampleCount; i++)
            {
                var gray = input[inputOffset++];
                var scaled = ToRgb8Component(gray);

                rgbaBuffer[rgbaBufferOffset + 0] = scaled;
                rgbaBuffer[rgbaBufferOffset + 1] = scaled;
                rgbaBuffer[rgbaBufferOffset + 2] = scaled;
                rgbaBuffer[rgbaBufferOffset + 3] = 0;

                rgbaBufferOffset += 4;
            }
        }

        public override DecodeArray GetDefaultDecodeArray(int bitsPerComponent)
        {
            return new DecodeArray(bitsPerComponent, new[] { 0f, 1f });
        }

        public override int ComponentsPerSample => 1;

        public override float[] DefaultColor => new[] { 0f };

        public override int GetHashCode() => 975312;
        public bool Equals(DeviceGrayColorSpace? other) => other != null;
        public override bool Equals(object? obj) => obj is DeviceGrayColorSpace;

        public override string ToString() => "Gray";
    }
}
