// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Common
{
    internal struct Matrix3x3 : IEquatable<Matrix3x3>
    {
        public float M11;
        public float M12;
        public float M13;
        public float M21;
        public float M22;
        public float M23;
        public float M31;
        public float M32;
        public float M33;

        public Matrix3x3(
            float m11, float m12, float m13,
            float m21, float m22, float m23,
            float m31, float m32, float m33
        )
        {
            M11 = m11;
            M12 = m12;
            M13 = m13;

            M21 = m21;
            M22 = m22;
            M23 = m23;

            M31 = m31;
            M32 = m32;
            M33 = m33;
        }

        public static Matrix3x3 Identity => new Matrix3x3(1, 0, 0, 0, 1, 0, 0, 0, 1);

        public static Matrix3x3 operator *(Matrix3x3 a, Matrix3x3 b)
        {
            Matrix3x3 result;

            result.M11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31;
            result.M12 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32;
            result.M13 = a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33;

            result.M21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31;
            result.M22 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32;
            result.M23 = a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33;

            result.M31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31;
            result.M32 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32;
            result.M33 = a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33;

            return result;
        }

        public static Matrix1x3 operator *(Matrix3x3 a, Matrix1x3 b)
        {
            Matrix1x3 result;

            result.M11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31;
            result.M21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31;
            result.M31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31;

            return result;
        }

        public static bool operator ==(Matrix3x3 a, Matrix3x3 b)
        {
            return
                a.M11 == b.M11 &&
                a.M22 == b.M22 &&
                a.M33 == b.M33 &&
                a.M12 == b.M12 &&
                a.M13 == b.M13 &&
                a.M21 == b.M21 &&
                a.M23 == b.M23 &&
                a.M31 == b.M31 &&
                a.M32 == b.M32;
        }

        public static bool operator !=(Matrix3x3 a, Matrix3x3 b)
        {
            return !(a == b);
        }

        public Matrix3x3 Transpose()
        {
            Matrix3x3 result;

            result.M11 = M11;
            result.M12 = M21;
            result.M13 = M31;

            result.M21 = M12;
            result.M22 = M22;
            result.M23 = M32;

            result.M31 = M13;
            result.M32 = M23;
            result.M33 = M33;

            return result;
        }

        public bool Equals(Matrix3x3 other)
        {
            return this == other;
        }

        public override bool Equals(object? obj)
        {
            return obj is Matrix3x3 matrix && this == matrix;
        }

        public override int GetHashCode()
        {
            return unchecked(
                ((int)M11 * 100003) ^
                ((int)M12 * 100019) ^
                ((int)M13 * 100043) ^
                ((int)M21 * 100049) ^
                ((int)M22 * 100057) ^
                ((int)M23 * 100069) ^
                ((int)M31 * 100103) ^
                ((int)M32 * 100109) ^
                ((int)M33 * 100129));
        }

        public override string ToString()
        {
            return $"[ {M11:0.##} {M12:0.##} {M13:0.##}, {M21:0.##} {M22:0.##} {M23:0.##}, {M31:0.##} {M32:0.##} {M33:0.##} ]";
        }
    }
}
