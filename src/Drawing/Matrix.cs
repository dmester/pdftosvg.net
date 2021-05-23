using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    internal class Matrix : IEquatable<Matrix>
    {
        public Matrix(
            double a,
            double b,
            double c,
            double d,
            double e,
            double f)
        {
            A = a;
            B = b;
            C = c;
            D = d;
            E = e;
            F = f;
        }

        public double A { get; private set; }
        public double B { get; private set; }
        public double C { get; private set; }
        public double D { get; private set; }
        public double E { get; private set; }
        public double F { get; private set; }

        public bool IsIdentity =>
            A == 1 && B == 0 &&
            C == 0 && D == 1 &&
            E == 0 && F == 0;

        public static Matrix Identity { get; } = new Matrix(1, 0, 0, 1, 0, 0);

        public static Matrix operator *(Matrix a, Matrix b) => Multiply(a, b);

        public static bool operator ==(Matrix a, Matrix b) => (object)a == null ? (object)b == null : a.Equals(b);

        public static bool operator !=(Matrix a, Matrix b) => !(a == b);

        public static Point operator *(Matrix a, Point b)
        {
            return new Point(
                a.A * b.X + a.C * b.Y + a.E,
                a.B * b.X + a.D * b.Y + a.F
                );
        }

        public static Matrix Multiply(Matrix a, Matrix b)
        {
            return new Matrix(
                a.A * b.A + a.B * b.C,
                a.A * b.B + a.B * b.D,
                a.C * b.A + a.D * b.C,
                a.C * b.B + a.D * b.D,
                a.E * b.A + a.F * b.C + b.E,
                a.E * b.B + a.F * b.D + b.F
                );
        }

        public static Matrix Translate(double dx, double dy) => new Matrix(1, 0, 0, 1, dx, dy);

        public static Matrix Scale(double sx, double sy) => new Matrix(sx, 0, 0, sy, 0, 0);

        public static Matrix Translate(double dx, double dy, Matrix source)
        {
            return new Matrix(
                source.A, source.B,
                source.C, source.D,
                source.E + source.A * dx + source.C * dy,
                source.F + source.B * dx + source.D * dy);
        }

        public static Matrix Scale(double sx, double sy, Matrix source)
        {
            return new Matrix(
                sx * source.A,
                sx * source.B,
                sy * source.C,
                sy * source.D,
                source.E, source.F);
        }

        public void DecomposeScale(out double scale, out Matrix remainder)
        {
            var scaleX = Math.Sqrt(A * A + B * B);
            var scaleY = Math.Sqrt(C * C + D * D);

            scale = Math.Min(scaleX, scaleY);

            if (scale == 0)
            {
                scale = 1;
            }

            remainder = new Matrix(
                A / scale,
                B / scale,
                C / scale,
                D / scale,
                E, F);
        }

        public void DecomposeScaleX(out double scaleX)
        {
            scaleX = Math.Sqrt(A * A + B * B);
        }

        public void DecomposeTranslate(out double dx, out double dy, out Matrix remainder)
        {
            var ad = A * D;
            var bc = B * C;

            if (ad == bc)
            {
                dx = 0;
                dy = 0;
                remainder = this;
            }
            else
            {
                dx = (C * F - D * E) / (bc - ad);
                dy = (A * F - B * E) / (ad - bc);
                remainder = new Matrix(A, B, C, D, 0, 0);
            }
        }

        public bool Equals(Matrix other)
        {
            return
                ReferenceEquals(this, other) ||
                (object)other != null &&
                other.A == A &&
                other.B == B &&
                other.E == E &&
                other.F == F &&
                other.C == C &&
                other.D == D;
        }

        public override bool Equals(object obj) => Equals(obj as Matrix);

        public override int GetHashCode()
        {
            return
                unchecked((int)(A * 100)) ^
                unchecked((int)(B * 10000)) ^
                unchecked((int)(C * 100000)) ^
                unchecked((int)(D * 1000000)) ^
                unchecked((int)(E * 10000000)) ^
                unchecked((int)(F * 100000000));
        }

        public override string ToString()
        {
            return $"[ {A:0.##} {B:0.##} 0 ] \r\n[ {C:0.##} {D:0.##} 0 ] \r\n[ {E:0.##} {F:0.##} 1 ]";
        }
    }
}
