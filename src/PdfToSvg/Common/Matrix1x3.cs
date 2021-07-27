// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Common
{
    internal struct Matrix1x3 : IEquatable<Matrix1x3>
    {
        public float M11;
        public float M21;
        public float M31;

        public Matrix1x3(float m11, float m21, float m31)
        {
            M11 = m11;
            M21 = m21;
            M31 = m31;
        }

        public static bool operator ==(Matrix1x3 a, Matrix1x3 b)
        {
            return
                a.M11 == b.M11 &&
                a.M21 == b.M21 &&
                a.M31 == b.M31;
        }

        public static bool operator !=(Matrix1x3 a, Matrix1x3 b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return obj is Matrix1x3 matrix && this == matrix;
        }

        public bool Equals(Matrix1x3 other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return unchecked(
                ((int)M11 * 100003) ^
                ((int)M21 * 100019) ^
                ((int)M31 * 100043));
        }

        public override string ToString()
        {
            return $"[ {M11:0.##}, {M21:0.##}, {M31:0.##} ]";
        }
    }
}
