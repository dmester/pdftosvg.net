// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace PdfToSvg.Functions.PostScript
{
    internal static class PostScriptOperators
    {
        private const double DegreesToRadians = (2d * Math.PI) / 360d;
        private const double RadiansToDegrees = 360d / (2d * Math.PI);

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Add(double left, double right) => left + right;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Sub(double left, double right) => left - right;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Mul(double left, double right) => left * right;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Div(double left, double right) => left / right;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Idiv(int left, int right) => right == 0
            ? (left < 0 ? double.NegativeInfinity : double.PositiveInfinity)
            : left / right;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Mod(int left, int right) => right == 0 ? double.NaN : left % right;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Neg(double value) => -value;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Abs(double value) => Math.Abs(value);

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Ceiling(double value) => Math.Ceiling(value);

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Floor(double value) => Math.Floor(value);

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Round(double value) => Math.Round(value);

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Truncate(double value) => Math.Truncate(value);

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Sqrt(double value) => Math.Sqrt(value);

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Sin(double value) => Math.Sin(value * DegreesToRadians);

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Cos(double value) => Math.Cos(value * DegreesToRadians);

        public static double Atan(double left, double right)
        {
            // Expected range: 0 - 360
            var atanDegrees = Math.Atan(left / right) * RadiansToDegrees;
            if (atanDegrees < 0)
            {
                atanDegrees += 360d;
            }

            return atanDegrees;
        }

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Exp(double left, double right) => Math.Pow(left, right);

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Ln(double value) => Math.Log(value);

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Log(double value) => Math.Log10(value);

        // Changed behavior in casting from double to int in .NET 9
        // https://learn.microsoft.com/en-us/dotnet/core/compatibility/jit/9.0/fp-to-integer
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static int Cvi(double value) =>
            double.IsNaN(value) ? 0 :
            value <= int.MinValue ? int.MinValue :
            value >= int.MaxValue ? int.MaxValue :
            (int)value;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Cvr(double value) => value;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static bool Eq(in PostScriptValue left, in PostScriptValue right) =>
            left.Type == right.Type && left.Number == right.Number;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static bool Eq(double left, double right) => left == right;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static bool Ne(in PostScriptValue left, in PostScriptValue right) =>
            left.Type != right.Type || left.Number != right.Number;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static bool Ne(double left, double right) => left != right;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static bool Gt(double left, double right) => left > right;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static bool Ge(double left, double right) => left >= right;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static bool Lt(double left, double right) => left < right;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static bool Le(double left, double right) => left <= right;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double LogicalAnd(double left, double right) => left != 0d && right != 0d ? 1d : 0d;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static int BitwiseAnd(int left, int right) => left & right;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double LogicalOr(double left, double right) => left != 0d || right != 0d ? 1d : 0d;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static int BitwiseOr(int left, int right) => left | right;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double LogicalXor(double left, double right) => (left != 0d) != (right != 0d) ? 1d : 0d;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static int BitwiseXor(int left, int right) => left ^ right;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double LogicalNot(double value) => value == 0d ? 1d : 0d;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static int BitwiseNot(int value) => ~value;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static int BitShift(int left, int right) => right > 0 ? left << right : left >> -right;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static bool True() => true;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static bool False() => false;

        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static double Execute(PostScriptOpCode opCode, double left, double right)
        {
            return opCode switch
            {
                PostScriptOpCode.PushValue => left,
                PostScriptOpCode.Add => Add(left, right),
                PostScriptOpCode.Sub => Sub(left, right),
                PostScriptOpCode.Mul => Mul(left, right),
                PostScriptOpCode.Div => Div(left, right),
                PostScriptOpCode.Idiv => Idiv((int)left, (int)right),
                PostScriptOpCode.Mod => Mod((int)left, (int)right),
                PostScriptOpCode.Atan => Atan(left, right),
                PostScriptOpCode.Exp => Exp(left, right),
                PostScriptOpCode.LogicalAnd => LogicalAnd(left, right),
                PostScriptOpCode.BitwiseAnd => BitwiseAnd((int)left, (int)right),
                PostScriptOpCode.LogicalOr => LogicalOr(left, right),
                PostScriptOpCode.BitwiseOr => BitwiseOr((int)left, (int)right),
                PostScriptOpCode.LogicalXor => LogicalXor(left, right),
                PostScriptOpCode.BitwiseXor => BitwiseXor((int)left, (int)right),
                PostScriptOpCode.LogicalNot => LogicalNot(left),
                PostScriptOpCode.BitwiseNot => BitwiseNot((int)left),
                PostScriptOpCode.Eq => Eq(left, right) ? 1d : 0d,
                PostScriptOpCode.Ne => Ne(left, right) ? 1d : 0d,
                PostScriptOpCode.Gt => Gt(left, right) ? 1d : 0d,
                PostScriptOpCode.Ge => Ge(left, right) ? 1d : 0d,
                PostScriptOpCode.Lt => Lt(left, right) ? 1d : 0d,
                PostScriptOpCode.Le => Le(left, right) ? 1d : 0d,
                PostScriptOpCode.BitShift => BitShift((int)left, (int)right),
                PostScriptOpCode.Cvi => Cvi(left),
                PostScriptOpCode.Cvr => Cvr(left),
                PostScriptOpCode.Neg => Neg(left),
                PostScriptOpCode.Abs => Abs(left),
                PostScriptOpCode.Round => Round(left),
                PostScriptOpCode.Truncate => Truncate(left),
                PostScriptOpCode.Sqrt => Sqrt(left),
                PostScriptOpCode.Sin => Sin(left),
                PostScriptOpCode.Cos => Cos(left),
                PostScriptOpCode.Ceiling => Ceiling(left),
                PostScriptOpCode.Floor => Floor(left),
                PostScriptOpCode.Ln => Ln(left),
                PostScriptOpCode.Log => Log(left),
                _ => throw new Exception("Unexpected instruction " + opCode + " in compiled PostScript expression"),
            };
        }

        /// <summary>
        /// Operators that can mathematically result in an infinite or NaN result with finite input.
        /// Simple arithmetics like multiplication will report false, even if they can technically result in
        /// <see cref="float.PositiveInfinity"/>.
        /// </summary>
        [MethodImpl(MethodInliningOptions.AggressiveInlining)]
        public static bool PotentiallyInfinite(PostScriptOpCode op) =>
            op == PostScriptOpCode.Atan ||
            op == PostScriptOpCode.Div ||
            op == PostScriptOpCode.Idiv ||
            op == PostScriptOpCode.Mod ||
            op == PostScriptOpCode.Sqrt ||
            op == PostScriptOpCode.Log ||
            op == PostScriptOpCode.Ln ||
            op == PostScriptOpCode.Exp;
    }
}
