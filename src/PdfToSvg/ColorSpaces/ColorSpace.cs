// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Functions;
using PdfToSvg.Imaging;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.ColorSpaces
{
    internal abstract class ColorSpace
    {
        public virtual void ToRgb8(float[] input, int inputOffset, byte[] rgbBuffer, int rgbBufferOffset, int sampleCount)
        {
            float red, green, blue;

            for (var i = 0; i < sampleCount; i++)
            {
                ToRgb(input, ref inputOffset, out red, out green, out blue);

                rgbBuffer[rgbBufferOffset + 0] = ToRgb8Component(red);
                rgbBuffer[rgbBufferOffset + 1] = ToRgb8Component(green);
                rgbBuffer[rgbBufferOffset + 2] = ToRgb8Component(blue);

                rgbBufferOffset += 3;
            }
        }

        public virtual void ToRgb8(float[] input, int inputOffset, float[] rgbBuffer, int rgbBufferOffset, int sampleCount)
        {
            float red, green, blue;

            for (var i = 0; i < sampleCount; i++)
            {
                ToRgb(input, ref inputOffset, out red, out green, out blue);

                rgbBuffer[rgbBufferOffset + 0] = MathUtils.Clamp(red * 255f, 0f, 255f);
                rgbBuffer[rgbBufferOffset + 1] = MathUtils.Clamp(green * 255f, 0f, 255f);
                rgbBuffer[rgbBufferOffset + 2] = MathUtils.Clamp(blue * 255f, 0f, 255f);

                rgbBufferOffset += 3;
            }
        }

        public virtual void ToRgba8(float[] input, int inputOffset, byte[] rgbaBuffer, int rgbaBufferOffset, int sampleCount)
        {
            float red, green, blue;

            for (var i = 0; i < sampleCount; i++)
            {
                ToRgb(input, ref inputOffset, out red, out green, out blue);

                rgbaBuffer[rgbaBufferOffset + 0] = ToRgb8Component(red);
                rgbaBuffer[rgbaBufferOffset + 1] = ToRgb8Component(green);
                rgbaBuffer[rgbaBufferOffset + 2] = ToRgb8Component(blue);
                rgbaBuffer[rgbaBufferOffset + 3] = 0;

                rgbaBufferOffset += 4;
            }
        }

        public void ToRgb(float[] input, out float red, out float green, out float blue)
        {
            var index = 0;
            ToRgb(input, ref index, out red, out green, out blue);
        }

        public abstract void ToRgb(float[] input, ref int inputOffset, out float red, out float green, out float blue);

        public abstract DecodeArray GetDefaultDecodeArray(int bitsPerComponent);

        public abstract int ComponentsPerSample { get; }

        public abstract float[] DefaultColor { get; }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        protected static byte ToRgb8Component(float value)
        {
            var intValue = unchecked((int)(value * 255f + 0.5f));

            if (intValue < 0)
            {
                intValue = 0;
            }
            else if (intValue > 255)
            {
                intValue = 255;
            }

            return unchecked((byte)intValue);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static int ToRgb16Component(float value)
        {
            var intValue = unchecked((int)(value * ushort.MaxValue + 0.5f));

            if (intValue < ushort.MinValue)
            {
                intValue = ushort.MinValue;
            }
            else if (intValue > ushort.MaxValue)
            {
                intValue = ushort.MaxValue;
            }

            return intValue;
        }
    }
}
