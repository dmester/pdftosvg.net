// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
#endif

namespace PdfToSvg.Imaging.Jpx.Transforms
{
    internal static class JpxColorSpaceTransform
    {
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void YccToRgb(float yccY, float yccCb, float yccCr, out float rgbR, out float rgbG, out float rgbB)
        {
            // Input values are in the range [0, 1], center chrominance around 0
            yccCr -= 0.5f;
            yccCb -= 0.5f;

            rgbR = MathUtils.Clamp(yccY + 1.4020f * yccCr, 0f, 1f);
            rgbG = MathUtils.Clamp(yccY - 0.3441363f * yccCb - 0.71413636f * yccCr, 0f, 1f);
            rgbB = MathUtils.Clamp(yccY + 1.772f * yccCb, 0f, 1f);
        }

#if NET8_0_OR_GREATER
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static void YccToRgb(
            Vector128<float> yccY,
            Vector128<float> yccCb,
            Vector128<float> yccCr,
            out Vector128<float> rgbR,
            out Vector128<float> rgbG,
            out Vector128<float> rgbB)
        {
            // Input values are in the range [0, 1], center chrominance around 0
            yccCr -= Vector128.Create(0.5f);
            yccCb -= Vector128.Create(0.5f);

            rgbR = VectorUtils.ClampNative(
                yccY + 1.4020f * yccCr,
                Vector128<float>.Zero, Vector128<float>.One);

            rgbG = VectorUtils.ClampNative(
                yccY - 0.3441363f * yccCb - 0.71413636f * yccCr,
                Vector128<float>.Zero, Vector128<float>.One);

            rgbB = VectorUtils.ClampNative(
                yccY + 1.772f * yccCb,
                Vector128<float>.Zero, Vector128<float>.One);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        private static void YccToRgb(
            Vector256<float> yccY,
            Vector256<float> yccCb,
            Vector256<float> yccCr,
            out Vector256<float> rgbR,
            out Vector256<float> rgbG,
            out Vector256<float> rgbB)
        {
            // Input values are in the range [0, 1], center chrominance around 0
            yccCr -= Vector256.Create(0.5f);
            yccCb -= Vector256.Create(0.5f);

            rgbR = VectorUtils.ClampNative(
                yccY + 1.4020f * yccCr,
                Vector256<float>.Zero, Vector256<float>.One);

            rgbG = VectorUtils.ClampNative(
                yccY - 0.3441363f * yccCb - 0.71413636f * yccCr,
                Vector256<float>.Zero, Vector256<float>.One);

            rgbB = VectorUtils.ClampNative(
                yccY + 1.772f * yccCb,
                Vector256<float>.Zero, Vector256<float>.One);
        }
#endif

        /// <summary>
        /// Converts the first <paramref name="count"/> samples of the first three planes of
        /// <paramref name="planes"/> in place from sYCC to RGB. All values are in [0, 1].
        /// </summary>
        public static void YccToRgb(float[][] planes, int count)
        {
            if (planes == null)
            {
                throw new ArgumentNullException(nameof(planes));
            }
            if (planes.Length < 3)
            {
                throw new ArgumentException("The sYCC to RGB transformation requires three color channels", nameof(planes));
            }

            var planeY = planes[0];
            var planeCb = planes[1];
            var planeCr = planes[2];

            if (planeY.Length < count || planeCb.Length < count || planeCr.Length < count)
            {
                throw new ArgumentException("The color parameter was larger than one of the color channel planes", nameof(count));
            }

            var i = 0;

#if NET8_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated)
            {
                ref var pPlaneY = ref MemoryMarshal.GetArrayDataReference(planeY);
                ref var pPlaneCb = ref MemoryMarshal.GetArrayDataReference(planeCb);
                ref var pPlaneCr = ref MemoryMarshal.GetArrayDataReference(planeCr);

                for (; i + Vector256<float>.Count <= count; i += Vector256<float>.Count)
                {
                    var inputY = Vector256.LoadUnsafe(ref pPlaneY, (nuint)i);
                    var inputCb = Vector256.LoadUnsafe(ref pPlaneCb, (nuint)i);
                    var inputCr = Vector256.LoadUnsafe(ref pPlaneCr, (nuint)i);

                    YccToRgb(
                        yccY: inputY,
                        yccCb: inputCb,
                        yccCr: inputCr,
                        out var outputY,
                        out var outputCb,
                        out var outputCr);

                    outputY.StoreUnsafe(ref pPlaneY, (nuint)i);
                    outputCb.StoreUnsafe(ref pPlaneCb, (nuint)i);
                    outputCr.StoreUnsafe(ref pPlaneCr, (nuint)i);
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                ref var pPlaneY = ref MemoryMarshal.GetArrayDataReference(planeY);
                ref var pPlaneCb = ref MemoryMarshal.GetArrayDataReference(planeCb);
                ref var pPlaneCr = ref MemoryMarshal.GetArrayDataReference(planeCr);

                for (; i + Vector128<float>.Count <= count; i += Vector128<float>.Count)
                {
                    var inputY = Vector128.LoadUnsafe(ref pPlaneY, (nuint)i);
                    var inputCb = Vector128.LoadUnsafe(ref pPlaneCb, (nuint)i);
                    var inputCr = Vector128.LoadUnsafe(ref pPlaneCr, (nuint)i);

                    YccToRgb(
                        yccY: inputY,
                        yccCb: inputCb,
                        yccCr: inputCr,
                        out var outputY,
                        out var outputCb,
                        out var outputCr);

                    outputY.StoreUnsafe(ref pPlaneY, (nuint)i);
                    outputCb.StoreUnsafe(ref pPlaneCb, (nuint)i);
                    outputCr.StoreUnsafe(ref pPlaneCr, (nuint)i);
                }
            }
#endif

            for (; i < count; i++)
            {
                YccToRgb(
                    yccY: planeY[i],
                    yccCb: planeCb[i],
                    yccCr: planeCr[i],
                    out planeY[i],
                    out planeCb[i],
                    out planeCr[i]);
            }
        }

    }
}
