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
            var result = 0;

            while ((value >>= 1) != 0)
            {
                result++;
            }

            return result;
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

        public static int FloorDiv(int x, int y)
        {
            var neg = false;

            if (x < 0)
            {
                neg = true;
                x = -x;
            }

            if (y < 0)
            {
                neg ^= true;
                y = -y;
            }

            if (neg)
            {
                x += y - 1;
            }

            var result = x / y;

            if (neg)
            {
                return -result;
            }
            else
            {
                return result;
            }
        }
    }
}
