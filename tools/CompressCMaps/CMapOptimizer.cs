// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.CMaps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompressCMaps
{
    internal static class CMapOptimizer
    {
        public static void Optimize(
            List<CMapRange> outputRanges, List<CMapChar> outputChars,
            IEnumerable<CMapRange> inputRanges, IEnumerable<CMapChar> inputChars, bool isBfChars)
        {
            var charsAsRanges = inputChars
                .Where(ch => !isBfChars || ch.Unicode != null && ch.Unicode.Length == 1)
                .Select(ch => new CMapRange(
                    ch.CharCode, ch.CharCode, ch.CharCodeLength,
                    isBfChars ? ch.Unicode![0] : ch.Cid));

            var allRanges = Enumerable.Concat(charsAsRanges, inputRanges)
                .OrderBy(x => x.CharCodeLength)
                .ThenBy(x => x.FromCharCode);

            foreach (var range in CombineRanges(allRanges))
            {
                if (range.FromCharCode == range.ToCharCode)
                {
                    outputChars.Add(new CMapChar(range.FromCharCode, range.CharCodeLength, range.StartValue));
                }
                else
                {
                    outputRanges.Add(range);
                }
            }
        }

        private static IEnumerable<CMapRange> CombineRanges(IEnumerable<CMapRange> input)
        {
            CMapRange previous = default;

            foreach (var range in input)
            {
                if (previous.IsEmpty)
                {
                    previous = range;
                }
                else if (
                    previous.CharCodeLength == range.CharCodeLength &&
                    previous.ToCharCode == range.FromCharCode - 1 &&
                    previous.StartValue + (range.FromCharCode - previous.FromCharCode) == range.StartValue)
                {
                    // Combine
                    previous = new CMapRange(previous.FromCharCode, range.ToCharCode, previous.CharCodeLength, previous.StartValue);
                }
                else
                {
                    yield return previous;
                    previous = range;
                }
            }

            if (!previous.IsEmpty)
            {
                yield return previous;
            }
        }
    }
}
