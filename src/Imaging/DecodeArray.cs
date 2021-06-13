// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Imaging
{
    internal class DecodeArray
    {
        private readonly Range[] ranges;

        public DecodeArray(int bitsPerComponent, float[] values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));

            ranges = new Range[values.Length / 2];

            for (var i = 0; i < ranges.Length; i++)
            {
                ranges[i] = new Range(values[i * 2 + 0], values[i * 2 + 1], bitsPerComponent);
            }
        }

        public DecodeArray(int bitsPerComponent, double[] values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));

            ranges = new Range[values.Length / 2];

            for (var i = 0; i < ranges.Length; i++)
            {
                ranges[i] = new Range((float)values[i * 2 + 0], (float)values[i * 2 + 1], bitsPerComponent);
            }
        }

        struct Range
        {
            private readonly float dmin;
            private readonly float multiplier;

            public Range(float dmin, float dmax, int bitsPerComponent)
            {
                this.dmin = dmin;
                multiplier = (dmax - dmin) / ((1 << bitsPerComponent) - 1);
            }

            public void Decode(ref float value)
            {
                value = dmin + value * multiplier;
            }
        }

        public void Decode(float[] values, int offset, int count)
        {
            var rangeOffset = offset % ranges.Length;

            for (var i = 0; i < count; i++)
            {
                ranges[rangeOffset].Decode(ref values[i + offset]);

                if (++rangeOffset >= ranges.Length)
                {
                    rangeOffset = 0;
                }
            }
        }
    }
}
