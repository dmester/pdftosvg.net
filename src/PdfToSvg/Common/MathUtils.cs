// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Common
{
    internal static class MathUtils
    {
        // The Clamp methods are signature compatible with the Math.Clamp methods in .NET Core.
        // Included for compatibility with .NET Framework.

#if HAVE_AGGRESSIVE_INLINING
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static float Clamp(float value, float min, float max)
        {
            return
                value < min ? min :
                value > max ? max :
                value;
        }

#if HAVE_AGGRESSIVE_INLINING
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static double Clamp(double value, double min, double max)
        {
            return
                value < min ? min :
                value > max ? max :
                value;
        }

#if HAVE_AGGRESSIVE_INLINING
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static int Clamp(int value, int min, int max)
        {
            return
                value < min ? min :
                value > max ? max :
                value;
        }

        public static bool ToInt(object? value, out int result)
        {
            if (value is int intValue)
            {
                result = intValue;
                return true;
            }

            if (value is double dblValue)
            {
                result = (int)dblValue;
                return true;
            }

            result = 0;
            return false;
        }

        public static bool ToDouble(object? value, out double result)
        {
            if (value is int intValue)
            {
                result = intValue;
                return true;
            }

            if (value is double dblValue)
            {
                result = (int)dblValue;
                return true;
            }

            result = 0;
            return false;
        }

#if HAVE_AGGRESSIVE_INLINING
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static double Interpolate(double x, double xmin, double xmax, double ymin, double ymax)
        {
            return ymin + ((x - xmin) * (ymax - ymin) / (xmax - xmin));
        }

#if HAVE_AGGRESSIVE_INLINING
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static int BitsToBytes(int bits)
        {
            return (bits + 7) >> 3;
        }
    }
}
