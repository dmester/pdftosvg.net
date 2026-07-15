// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

#if !NETCOREAPP2_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
    internal static class MathF
    {
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static float Floor(float value)
        {
            return (float)Math.Floor(value);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static float Ceiling(float value)
        {
            return (float)Math.Ceiling(value);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static float Round(float value)
        {
            return (float)Math.Round(value);
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static float Pow(float x, float y)
        {
            return (float)Math.Pow(x, y);
        }
    }
}
#endif
