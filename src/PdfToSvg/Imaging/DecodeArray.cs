// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Imaging
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class DecodeArray : IEquatable<DecodeArray?>
    {
        private readonly DecodeRange[] ranges;
        private readonly Array values;

        public DecodeArray(int bitsPerComponent, float[] values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));

            ranges = new DecodeRange[values.Length / 2];
            this.values = values;

            for (var i = 0; i < ranges.Length; i++)
            {
                ranges[i] = new DecodeRange(values[i * 2 + 0], values[i * 2 + 1], bitsPerComponent);
            }
        }

        public DecodeArray(int bitsPerComponent, double[] values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));

            ranges = new DecodeRange[values.Length / 2];
            this.values = values;

            for (var i = 0; i < ranges.Length; i++)
            {
                ranges[i] = new DecodeRange((float)values[i * 2 + 0], (float)values[i * 2 + 1], bitsPerComponent);
            }
        }

        public void Decode(float[] values) => Decode(values, 0, values.Length);

        public void Decode(float[] values, int offset, int count)
        {
            var rangeOffset = offset % ranges.Length;

            for (var i = 0; i < count; i++)
            {
                values[i + offset] = ranges[rangeOffset].Decode(values[i + offset]);

                if (++rangeOffset >= ranges.Length)
                {
                    rangeOffset = 0;
                }
            }
        }

        public override int GetHashCode()
        {
            // A decode array will probably never be used as key, so let's be ok with this bad hash code
            return ranges.Length;
        }

        public bool Equals(DecodeArray? other)
        {
            if (other == null || other.ranges.Length != ranges.Length)
            {
                return false;
            }

            for (var i = 0; i < ranges.Length; i++)
            {
                if (!ranges[i].Equals(other.ranges[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as DecodeArray);
        }

        private string DebuggerDisplay
        {
            get
            {
                string GetFormattedValue(int index)
                {
                    return ((IFormattable)values.GetValue(index)).ToString("0.###", CultureInfo.InvariantCulture);
                }

                var formattedRanges = Enumerable
                    .Range(0, values.Length / 2)
                    .Select(i => GetFormattedValue(i * 2 + 0) + " " + GetFormattedValue(i * 2 + 1));

                return "[ " + string.Join(", ", formattedRanges) + " ]";
            }
        }
    }
}
