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

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static float Clamp(float value, float min, float max)
        {
            return
                value < min ? min :
                value > max ? max :
                value;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Clamp(double value, double min, double max)
        {
            return
                value < min ? min :
                value > max ? max :
                value;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static int Clamp(int value, int min, int max)
        {
            return
                value < min ? min :
                value > max ? max :
                value;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static long Clamp(long value, long min, long max)
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

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Interpolate(double x, double xmin, double xmax, double ymin, double ymax)
        {
            return ymin + ((x - xmin) * (ymax - ymin) / (xmax - xmin));
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static int BitsToBytes(int bits)
        {
            return (bits + 7) >> 3;
        }

        public static int ModBE(byte[] dividend, byte divisor)
        {
            // Adapted from https://stackoverflow.com/a/10441333
            var result = 0;

            for (var i = 0; i < dividend.Length; i++)
            {
                result = (result * (256 % divisor) + (dividend[i] % divisor)) % divisor;
            }

            return result;
        }

        public static int IntLog2(int value)
        {
            // Validation is important for the fallback loop below, which would hang on negative values
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

#if NET5_0_OR_GREATER
            return System.Numerics.BitOperations.Log2(unchecked((uint)value));
#else
            var result = 0;
            while ((value >>= 1) != 0)
            {
                result++;
            }
            return result;
#endif
        }

        public static int IntLog2Ceil(int value)
        {
            var result = 0;

            value--;

            while (value > 0)
            {
                result++;
                value >>= 1;
            }

            return result;
        }

        public static uint FloorDiv(uint dividend, uint divisor)
        {
            return dividend / divisor;
        }

        public static uint CeilDiv(uint dividend, uint divisor)
        {
            return dividend == 0
              ? 0
              : 1 + (dividend - 1) / divisor;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static int FloorDiv(int dividend, int divisor)
        {
            // See example here:
            // https://stackoverflow.com/questions/46265403/fast-floor-of-a-signed-integer-division-in-c-c

            var quotient = dividend / divisor;

            // (x ^ y) is negative when one of the inputs are negative
            if ((dividend ^ divisor) < 0 && quotient * divisor != dividend)
            {
                return quotient - 1;
            }
            else
            {
                return quotient;
            }
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static int CeilDiv(int dividend, int divisor)
        {
            var quotient = dividend / divisor;

            // (x ^ y) is negative when one of the inputs are negative
            if ((dividend ^ divisor) > 0 && quotient * divisor != dividend)
            {
                return quotient + 1;
            }
            else
            {
                return quotient;
            }
        }

        /// <summary>
        /// Computes <c>ceil(<paramref name="dividend"/> / (2 ^ (<paramref name="divisorPow2"/>)))</c>.
        /// </summary>
        public static int CeilDivPow2(long dividend, int divisorPow2)
        {
            if (divisorPow2 < 0 || divisorPow2 > 62)
            {
                throw new ArgumentOutOfRangeException(nameof(divisorPow2));
            }

            var divisor = 1L << divisorPow2;

            if (dividend >= 0)
            {
                return (int)((dividend + divisor - 1) >> divisorPow2);
            }
            else
            {
                return -(int)((-dividend) >> divisorPow2);
            }
        }
    }
}
