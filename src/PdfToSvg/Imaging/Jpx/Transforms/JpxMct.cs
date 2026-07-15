// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Imaging.Jpx.ImageModel;
using System;
using System.Runtime.CompilerServices;

#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
#endif

namespace PdfToSvg.Imaging.Jpx.Transforms
{
    /// <summary>
    /// Inverse multiple component transformations (ITU-T T.800 (06/2019) Annex G)
    /// </summary>
    internal static class JpxMct
    {
        public const int ComponentCount = 3;

        /// <summary>
        /// Returns whether the first three codestream components have equal sub-sampling and precision, so that the
        /// transformation can be applied (ITU-T T.800 (06/2019) Sections G.2 and G.3). Equal sub-sampling implies
        /// equal tile-component dimensions in every tile of the image, including clipped edge tiles (Equations B-7
        /// and B-12 derive every tile-component's area from the same tile bounds and the component's own
        /// sub-sampling factors only), so this check alone determines whether the transformation is applicable to
        /// every tile.
        /// </summary>
        public static bool AreComponentsCompatible(JpxComponent[] components)
        {
            if (components.Length < ComponentCount)
            {
                return false;
            }

            var component0 = components[0];
            var component1 = components[1];
            var component2 = components[2];

            return
                component1.XRsizi == component0.XRsizi && component1.YRsizi == component0.YRsizi &&
                component2.XRsizi == component0.XRsizi && component2.YRsizi == component0.YRsizi &&
                component1.Precision == component0.Precision &&
                component2.Precision == component0.Precision;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void InverseRct(float y0, float y1, float y2, out float i0, out float i1, out float i2)
        {
            // ITU-T T.800 (06/2019) Equations G-6 to G-8
            i1 = y0 - MathF.Floor((y2 + y1) / 4);
            i0 = y2 + i1;
            i2 = y1 + i1;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static void InverseIct(float y0, float y1, float y2, out float i0, out float i1, out float i2)
        {
            // ITU-T T.800 (06/2019) Equations G-12 to G-14
            i0 = y0 + 1.402f * y2;
            i1 = y0 - 0.34413f * y1 - 0.71414f * y2;
            i2 = y0 + 1.772f * y1;
        }

        public static void InverseArray(float[][] planes, int count, bool reversible)
        {
            if (planes.Length < ComponentCount)
            {
                throw new JpxException("The JPEG 2000 multiple component transformation requires three components.");
            }

            var plane0 = planes[0];
            var plane1 = planes[1];
            var plane2 = planes[2];

            if (plane0.Length < count || plane1.Length < count || plane2.Length < count)
            {
                throw new JpxException("Component plane smaller than the JPEG 2000 multiple component transformation area.");
            }

            if (reversible)
            {
                InverseArray(new InverseRctOp(), plane0, plane1, plane2, count);
            }
            else
            {
                InverseArray(new InverseIctOp(), plane0, plane1, plane2, count);
            }
        }

        private static void InverseArray<TOp>(TOp op, float[] plane0, float[] plane1, float[] plane2, int count)
            where TOp : struct, IMctOp
        {
            var i = 0;

#if NET8_0_OR_GREATER
            if (Vector256.IsHardwareAccelerated)
            {
                ref var plane0Ref = ref MemoryMarshal.GetArrayDataReference(plane0);
                ref var plane1Ref = ref MemoryMarshal.GetArrayDataReference(plane1);
                ref var plane2Ref = ref MemoryMarshal.GetArrayDataReference(plane2);

                for (; i + Vector256<float>.Count <= count; i += Vector256<float>.Count)
                {
                    var y0 = Vector256.LoadUnsafe(ref plane0Ref, (nuint)i);
                    var y1 = Vector256.LoadUnsafe(ref plane1Ref, (nuint)i);
                    var y2 = Vector256.LoadUnsafe(ref plane2Ref, (nuint)i);

                    op.Apply(y0, y1, y2, out var i0, out var i1, out var i2);

                    i0.StoreUnsafe(ref plane0Ref, (nuint)i);
                    i1.StoreUnsafe(ref plane1Ref, (nuint)i);
                    i2.StoreUnsafe(ref plane2Ref, (nuint)i);
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                ref var plane0Ref = ref MemoryMarshal.GetArrayDataReference(plane0);
                ref var plane1Ref = ref MemoryMarshal.GetArrayDataReference(plane1);
                ref var plane2Ref = ref MemoryMarshal.GetArrayDataReference(plane2);

                for (; i + Vector128<float>.Count <= count; i += Vector128<float>.Count)
                {
                    var y0 = Vector128.LoadUnsafe(ref plane0Ref, (nuint)i);
                    var y1 = Vector128.LoadUnsafe(ref plane1Ref, (nuint)i);
                    var y2 = Vector128.LoadUnsafe(ref plane2Ref, (nuint)i);

                    op.Apply(y0, y1, y2, out var i0, out var i1, out var i2);

                    i0.StoreUnsafe(ref plane0Ref, (nuint)i);
                    i1.StoreUnsafe(ref plane1Ref, (nuint)i);
                    i2.StoreUnsafe(ref plane2Ref, (nuint)i);
                }
            }
#endif

            for (; i < count; i++)
            {
                op.Apply(plane0[i], plane1[i], plane2[i], out var i0, out var i1, out var i2);

                plane0[i] = i0;
                plane1[i] = i1;
                plane2[i] = i2;
            }
        }

        private interface IMctOp
        {
            void Apply(float y0, float y1, float y2, out float i0, out float i1, out float i2);

#if NET8_0_OR_GREATER
            void Apply(
                Vector256<float> y0,
                Vector256<float> y1,
                Vector256<float> y2,
                out Vector256<float> i0,
                out Vector256<float> i1,
                out Vector256<float> i2);

            void Apply(
                Vector128<float> y0,
                Vector128<float> y1,
                Vector128<float> y2,
                out Vector128<float> i0,
                out Vector128<float> i1,
                out Vector128<float> i2);
#endif
        }

        private readonly struct InverseRctOp : IMctOp
        {
            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public void Apply(float y0, float y1, float y2, out float i0, out float i1, out float i2)
            {
                InverseRct(y0, y1, y2, out i0, out i1, out i2);
            }

#if NET8_0_OR_GREATER
            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public void Apply(
                Vector256<float> y0,
                Vector256<float> y1,
                Vector256<float> y2,
                out Vector256<float> i0,
                out Vector256<float> i1,
                out Vector256<float> i2)
            {
                // ITU-T T.800 (06/2019) Equations G-6 to G-8
                i1 = y0 - Vector256.Floor((y2 + y1) / 4f);
                i0 = y2 + i1;
                i2 = y1 + i1;
            }

            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public void Apply(
                Vector128<float> y0,
                Vector128<float> y1,
                Vector128<float> y2,
                out Vector128<float> i0,
                out Vector128<float> i1,
                out Vector128<float> i2)
            {
                // ITU-T T.800 (06/2019) Equations G-6 to G-8
                i1 = y0 - Vector128.Floor((y2 + y1) / 4f);
                i0 = y2 + i1;
                i2 = y1 + i1;
            }
#endif
        }

        private readonly struct InverseIctOp : IMctOp
        {
            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public void Apply(float y0, float y1, float y2, out float i0, out float i1, out float i2)
            {
                InverseIct(y0, y1, y2, out i0, out i1, out i2);
            }

#if NET8_0_OR_GREATER
            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public void Apply(
                Vector256<float> y0,
                Vector256<float> y1,
                Vector256<float> y2,
                out Vector256<float> i0,
                out Vector256<float> i1,
                out Vector256<float> i2)
            {
                // ITU-T T.800 (06/2019) Equations G-12 to G-14
                i0 = y0 + 1.402f * y2;
                i1 = y0 - 0.34413f * y1 - 0.71414f * y2;
                i2 = y0 + 1.772f * y1;
            }

            [MethodImpl(MethodInliningOptions.AggressiveInlining)]
            public void Apply(
                Vector128<float> y0,
                Vector128<float> y1,
                Vector128<float> y2,
                out Vector128<float> i0,
                out Vector128<float> i1,
                out Vector128<float> i2)
            {
                // ITU-T T.800 (06/2019) Equations G-12 to G-14
                i0 = y0 + 1.402f * y2;
                i1 = y0 - 0.34413f * y1 - 0.71414f * y2;
                i2 = y0 + 1.772f * y1;
            }
#endif
        }
    }
}
