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
        public void ToRgb8(float[] input, int inputOffset, byte[] rgbBuffer, int rgbBufferOffset, int count)
        {
            float red, green, blue;

            for (var i = 0; i < count; i++)
            {
                ToRgb(input, ref inputOffset, out red, out green, out blue);

                rgbBuffer[rgbBufferOffset++] = ToRgb8Component(red);
                rgbBuffer[rgbBufferOffset++] = ToRgb8Component(green);
                rgbBuffer[rgbBufferOffset++] = ToRgb8Component(blue);
            }
        }

        public void ToRgba8(float[] input, int inputOffset, byte[] rgbBuffer, int rgbBufferOffset, int count)
        {
            float red, green, blue;

            for (var i = 0; i < count; i++)
            {
                ToRgb(input, ref inputOffset, out red, out green, out blue);

                rgbBuffer[rgbBufferOffset++] = ToRgb8Component(red);
                rgbBuffer[rgbBufferOffset++] = ToRgb8Component(green);
                rgbBuffer[rgbBufferOffset++] = ToRgb8Component(blue);
                rgbBuffer[rgbBufferOffset++] = 0;
            }
        }

        // TODO test
        public void ToRgb16BE(float[] input, int inputOffset, byte[] rgbBuffer, int rgbBufferOffset, int count)
        {
            float red, green, blue;

            for (var i = 0; i < count; i++)
            {
                ToRgb(input, ref inputOffset, out red, out green, out blue);

                var intRed = ToRgb16Component(red);
                var intGreen = ToRgb16Component(green);
                var intBlue = ToRgb16Component(blue);

                rgbBuffer[rgbBufferOffset++] = unchecked((byte)(intRed >> 8));
                rgbBuffer[rgbBufferOffset++] = unchecked((byte)intRed);

                rgbBuffer[rgbBufferOffset++] = unchecked((byte)(intGreen >> 8));
                rgbBuffer[rgbBufferOffset++] = unchecked((byte)intGreen);

                rgbBuffer[rgbBufferOffset++] = unchecked((byte)(intBlue >> 8));
                rgbBuffer[rgbBufferOffset++] = unchecked((byte)intBlue);
            }
        }

        // TODO test
        public void ToRgba16BE(float[] input, int inputOffset, byte[] rgbBuffer, int rgbBufferOffset, int count)
        {
            float red, green, blue;

            for (var i = 0; i < count; i++)
            {
                ToRgb(input, ref inputOffset, out red, out green, out blue);

                var intRed = ToRgb16Component(red);
                var intGreen = ToRgb16Component(green);
                var intBlue = ToRgb16Component(blue);

                rgbBuffer[rgbBufferOffset++] = unchecked((byte)(intRed >> 8));
                rgbBuffer[rgbBufferOffset++] = unchecked((byte)intRed);

                rgbBuffer[rgbBufferOffset++] = unchecked((byte)(intGreen >> 8));
                rgbBuffer[rgbBufferOffset++] = unchecked((byte)intGreen);

                rgbBuffer[rgbBufferOffset++] = unchecked((byte)(intBlue >> 8));
                rgbBuffer[rgbBufferOffset++] = unchecked((byte)intBlue);

                rgbBuffer[rgbBufferOffset++] = 0;
                rgbBuffer[rgbBufferOffset++] = 0;
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
        private static byte ToRgb8Component(float value)
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
