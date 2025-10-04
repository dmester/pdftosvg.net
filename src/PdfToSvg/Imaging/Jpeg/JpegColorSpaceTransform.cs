// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace PdfToSvg.Imaging.Jpeg
{
    internal static class JpegColorSpaceTransform
    {
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void YccToRgb(float yccY, float yccCb, float yccCr, out float rgbR, out float rgbG, out float rgbB)
        {
            // The relationship between RGB and YCC is documented in section 13.2:
            // https://www.pdfa.org/norm-refs/5116.DCT_Filter.pdf

            rgbR = yccY + 1.4020f * yccCr - 179.456f;
            rgbG = yccY - 0.3441363f * yccCb - 0.71413636f * yccCr + 135.45890048f;
            rgbB = yccY + 1.772f * yccCb - 226.816f;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void RgbToYcc(float rgbR, float rgbG, float rgbB, out float yccY, out float yccCb, out float yccCr)
        {
            // The relationship between RGB and YCC is documented in section 13.2:
            // https://www.pdfa.org/norm-refs/5116.DCT_Filter.pdf

            yccY = .299f * rgbR + .587f * rgbG + .114f * rgbB;
            yccCb = -.168736f * rgbR - .331264f * rgbG + .500f * rgbB + 128;
            yccCr = .500f * rgbR - .4186876f * rgbG - .08131241f * rgbB + 128;
        }

        public static int RgbToYcc(short[] data, int offset, int count)
        {
            for (var inputCursor = 0; inputCursor + 2 < count; inputCursor += 3)
            {
                var rgbR = data[offset + inputCursor + 0];
                var rgbG = data[offset + inputCursor + 1];
                var rgbB = data[offset + inputCursor + 2];

                RgbToYcc(rgbR, rgbG, rgbB, out var yccY, out var yccCb, out var yccCr);

                data[offset + inputCursor + 0] = (short)MathUtils.Clamp(yccY, 0, 255);
                data[offset + inputCursor + 1] = (short)MathUtils.Clamp(yccCb, 0, 255);
                data[offset + inputCursor + 2] = (short)MathUtils.Clamp(yccCr, 0, 255);
            }

            return count;
        }

        public static int YccToRgb(short[] data, int offset, int count)
        {
            for (var inputCursor = 0; inputCursor + 2 < count; inputCursor += 3)
            {
                var yccY = data[offset + inputCursor + 0];
                var yccCb = data[offset + inputCursor + 1];
                var yccCr = data[offset + inputCursor + 2];

                YccToRgb(yccY, yccCb, yccCr, out var rgbR, out var rgbG, out var rgbB);

                data[offset + inputCursor + 0] = (short)MathUtils.Clamp(rgbR, 0, 255);
                data[offset + inputCursor + 1] = (short)MathUtils.Clamp(rgbG, 0, 255);
                data[offset + inputCursor + 2] = (short)MathUtils.Clamp(rgbB, 0, 255);
            }

            return count;
        }

        public static int CmykToYcc(short[] data, int offset, int count)
        {
            var outputCursor = 0;

            for (var inputCursor = 0; inputCursor + 3 < count; inputCursor += 4)
            {
                var cmykC = data[offset + inputCursor + 0] * (1f / 255);
                var cmykM = data[offset + inputCursor + 1] * (1f / 255);
                var cmykY = data[offset + inputCursor + 2] * (1f / 255);
                var cmykK = data[offset + inputCursor + 3] * (1f / 255);

                DeviceCmykColorSpace.ToRgb(cmykC, cmykM, cmykY, cmykK, out var rgbR, out var rgbG, out var rgbB);

                rgbR *= 255;
                rgbG *= 255;
                rgbB *= 255;

                RgbToYcc(rgbR, rgbG, rgbB, out var yccY, out var yccCb, out var yccCr);

                data[outputCursor++] = (short)MathUtils.Clamp(yccY, 0, 255);
                data[outputCursor++] = (short)MathUtils.Clamp(yccCb, 0, 255);
                data[outputCursor++] = (short)MathUtils.Clamp(yccCr, 0, 255);
            }

            return outputCursor;
        }

        public static int YcckToYcc(short[] data, int offset, int count)
        {
            var outputCursor = offset;

            for (var inputCursor = 0; inputCursor + 3 < count; inputCursor += 4)
            {
                var ycckY = data[offset + inputCursor + 0];
                var ycckCb = data[offset + inputCursor + 1];
                var ycckCr = data[offset + inputCursor + 2];
                var ycckK = data[offset + inputCursor + 3];

                YccToRgb(ycckY, ycckCb, ycckCr, out var ycckR, out var ycckG, out var ycckB);

                // The relationship between CMYK and YCCK is documented in section 13.1:
                // https://www.pdfa.org/norm-refs/5116.DCT_Filter.pdf
                var cmykC = (255 - ycckR) * (1f / 255);
                var cmykM = (255 - ycckG) * (1f / 255);
                var cmykY = (255 - ycckB) * (1f / 255);
                var cmykK = ycckK * (1f / 255);

                DeviceCmykColorSpace.ToRgb(cmykC, cmykM, cmykY, cmykK, out var rgbR, out var rgbG, out var rgbB);

                rgbR *= 255;
                rgbG *= 255;
                rgbB *= 255;

                RgbToYcc(rgbR, rgbG, rgbB, out var yccY, out var yccCb, out var yccCr);

                data[outputCursor++] = (short)MathUtils.Clamp(yccY, 0, 255);
                data[outputCursor++] = (short)MathUtils.Clamp(yccCb, 0, 255);
                data[outputCursor++] = (short)MathUtils.Clamp(yccCr, 0, 255);
            }

            return outputCursor;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void YccToRgb(float[] data, int offset, int count)
        {
            for (var i = 0; i + 2 < count; i += 3)
            {
                var ycckY = data[offset + i + 0];
                var ycckCb = data[offset + i + 1];
                var ycckCr = data[offset + i + 2];

                YccToRgb(ycckY, ycckCb, ycckCr, out var rgbR, out var rgbG, out var rgbB);

                data[offset + i + 0] = MathUtils.Clamp(rgbR, 0f, 255f);
                data[offset + i + 1] = MathUtils.Clamp(rgbG, 0f, 255f);
                data[offset + i + 2] = MathUtils.Clamp(rgbB, 0f, 255f);
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void YcckToCmyk(float[] data, int offset, int count)
        {
            for (var i = 0; i + 3 < count; i += 4)
            {
                var ycckY = data[offset + i + 0];
                var ycckCb = data[offset + i + 1];
                var ycckCr = data[offset + i + 2];

                YccToRgb(ycckY, ycckCb, ycckCr, out var ycckR, out var ycckG, out var ycckB);

                // The relationship between CMYK and YCCK is documented in section 13.1:
                // https://www.pdfa.org/norm-refs/5116.DCT_Filter.pdf
                var cmykC = 255f - ycckR;
                var cmykM = 255f - ycckG;
                var cmykY = 255f - ycckB;

                data[offset + i + 0] = MathUtils.Clamp(cmykC, 0f, 255f);
                data[offset + i + 1] = MathUtils.Clamp(cmykM, 0f, 255f);
                data[offset + i + 2] = MathUtils.Clamp(cmykY, 0f, 255f);
            }
        }

    }
}
