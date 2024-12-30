// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jbig2.Coding
{
    internal abstract class JbigHuffmanRange
    {
        public int PrefixLength { get; protected set; }
        public int RangeLength { get; protected set; }

        public abstract JbigDecodedValue Decode(int value);


        private class LowerRange : JbigHuffmanRange
        {
            public int RangeHigh;

            public override JbigDecodedValue Decode(int value) => new JbigDecodedValue(RangeHigh - value);

            public override string ToString() => "∞ ... " + RangeHigh;
        }

        private class UpperRange : JbigHuffmanRange
        {
            public int RangeLow;

            public override JbigDecodedValue Decode(int value) => new JbigDecodedValue(RangeLow + value);

            public override string ToString() => RangeLow + " ... ∞";
        }

        private class NormalRange : JbigHuffmanRange
        {
            public int RangeLow;

            public override JbigDecodedValue Decode(int value) => new JbigDecodedValue(RangeLow + value);

            public override string ToString()
            {
                if (RangeLength > 0)
                {
                    return RangeLow + " ... " + (RangeLow + (1 << RangeLength) - 1);
                }
                else
                {
                    return RangeLow.ToString();
                }
            }
        }

        private class OutOfBandRange : JbigHuffmanRange
        {
            public override JbigDecodedValue Decode(int value) => JbigDecodedValue.Oob;

            public override string ToString() => "OOB";
        }

        public static JbigHuffmanRange Single(int single, int prefixLength) => new NormalRange
        {
            PrefixLength = prefixLength,
            RangeLength = 0,
            RangeLow = single,
        };

        public static JbigHuffmanRange Range(int rangeLow, int prefixLength, int rangeLength) => new NormalRange
        {
            PrefixLength = prefixLength,
            RangeLength = rangeLength,
            RangeLow = rangeLow,
        };

        public static JbigHuffmanRange Lower(int rangeHigh, int prefixLength) => new LowerRange
        {
            PrefixLength = prefixLength,
            RangeLength = 32,
            RangeHigh = rangeHigh,
        };

        public static JbigHuffmanRange Upper(int rangeLow, int prefixLength) => new UpperRange
        {
            PrefixLength = prefixLength,
            RangeLength = 32,
            RangeLow = rangeLow,
        };

        public static JbigHuffmanRange OutOfBand(int prefixLength) => new OutOfBandRange
        {
            PrefixLength = prefixLength,
            RangeLength = 0,
        };
    }
}
