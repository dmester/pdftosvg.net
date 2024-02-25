// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging
{
    internal struct DecodeRange : IEquatable<DecodeRange>
    {
        /// <summary>
        /// Smaller differences than this cannot be represented in neither 8-bit nor 16-bit images.
        /// </summary>
        private const float EqualityTolerance = 1f / 65536;

        private readonly float dmin;
        private readonly float multiplier;

        public DecodeRange(float dmin, float dmax, int bitsPerComponent)
        {
            if (bitsPerComponent < 1 || bitsPerComponent > 32)
            {
                throw new ArgumentOutOfRangeException(nameof(bitsPerComponent));
            }

            this.dmin = dmin;

            var maxValue = bitsPerComponent == 32 ? (float)uint.MaxValue : (float)((1u << bitsPerComponent) - 1);
            multiplier = (dmax - dmin) / maxValue;
        }

        public float Decode(float value)
        {
            return dmin + value * multiplier;
        }

        public double Decode(double value)
        {
            return dmin + value * multiplier;
        }

        private static bool ApproxEquals(float a, float b)
        {
            return Math.Abs(a - b) <= EqualityTolerance;
        }

        public override bool Equals(object obj)
        {
            return obj is DecodeRange range && Equals(range);
        }

        public bool Equals(DecodeRange other)
        {
            return ApproxEquals(other.dmin, dmin) && ApproxEquals(other.multiplier, multiplier);
        }

        public override int GetHashCode()
        {
            return dmin.GetHashCode() ^ multiplier.GetHashCode();
        }
    }
}
