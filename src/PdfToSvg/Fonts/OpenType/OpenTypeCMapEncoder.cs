// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Fonts.OpenType.Tables;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType
{
    internal static class OpenTypeCMapEncoder
    {
        internal static List<OpenTypeCMapRange> CombineGlyphRanges(IEnumerable<OpenTypeCMapRange> inputRanges)
        {
            var ranges = inputRanges.ToList();

            ranges.Sort(x => x.StartUnicode, x => x.StartGlyphIndex);

            var readCursor = 0;
            var writeCursor = -1;

            for (; readCursor < ranges.Count; readCursor++)
            {
                if (writeCursor >= 0 &&
                    ranges[writeCursor].EndGlyphIndex + 1 == ranges[readCursor].StartGlyphIndex &&
                    ranges[writeCursor].EndUnicode + 1 == ranges[readCursor].StartUnicode)
                {
                    ranges[writeCursor] = new OpenTypeCMapRange(
                        ranges[writeCursor].StartUnicode,
                        ranges[readCursor].EndUnicode,
                        ranges[writeCursor].StartGlyphIndex
                        );
                }
                else
                {
                    ranges[++writeCursor] = ranges[readCursor];
                }
            }

            ranges.RemoveRange(writeCursor + 1, ranges.Count - writeCursor - 1);

            return ranges;
        }

        public static CMapFormat12 EncodeFormat12(IEnumerable<OpenTypeCMapRange> ranges)
        {
            var optimizedRanges = CombineGlyphRanges(ranges);

            var format12 = new CMapFormat12
            {
                Language = 0,
                Groups = optimizedRanges
                    .Select(range => new CMapFormat12Group
                    {
                        StartCharCode = range.StartUnicode,
                        EndCharCode = range.EndUnicode,
                        StartGlyphID = range.StartGlyphIndex,
                    })
                    .ToArray(),
            };

            return format12;
        }

        public static CMapFormat4 EncodeFormat4(IEnumerable<OpenTypeCMapRange> ranges, out bool wasSubsetted)
        {
            var localWasSubsetted = false;

            var filteredRanges = ranges
                .Where(range =>
                {
                    if (range.StartUnicode > 0xffff ||
                        range.EndUnicode > 0xffff)
                    {
                        localWasSubsetted = true;
                    }

                    return range.StartUnicode < 0xffff;
                });
            var optimizedRanges = CombineGlyphRanges(filteredRanges);

            // The length field of the Format 4 table is limited to 16 bit, so the table must not exceed 2^16 = 64k bytes
            const int TableMaxSize = ushort.MaxValue;
            const int HeaderSize = 8 * 2; // incl "reservedPad"
            const int RangeSize = 4 * 2;
            const int MaxRangeCount = (TableMaxSize - HeaderSize) / RangeSize - 1;

            if (optimizedRanges.Count > MaxRangeCount)
            {
                optimizedRanges.RemoveRange(MaxRangeCount, optimizedRanges.Count - MaxRangeCount);
                localWasSubsetted = true;
            }

            var startCodes = new ushort[optimizedRanges.Count + 1];
            var endCodes = new ushort[optimizedRanges.Count + 1];
            var idDeltas = new short[optimizedRanges.Count + 1];
            var idRangeOffsets = new ushort[optimizedRanges.Count + 1];
            var glyphIdList = new ushort[0];

            var index = 0;

            foreach (var range in optimizedRanges)
            {
                startCodes[index] = (ushort)range.StartUnicode;
                endCodes[index] = (ushort)Math.Min(range.EndUnicode, 0xfffe);
                idDeltas[index] = unchecked((short)(range.StartGlyphIndex - range.StartUnicode));
                index++;
            }

            // According to spec, last range must map [0xffff, 0xffff] to glyph 0
            var last = endCodes.Length - 1;
            startCodes[last] = 0xffff;
            endCodes[last] = 0xffff;
            idDeltas[last] = 1;

            var format4 = new CMapFormat4
            {
                Language = 0,
                StartCode = startCodes,
                EndCode = endCodes,
                IdDelta = idDeltas,
                IdRangeOffsets = idRangeOffsets,
                GlyphIdArray = glyphIdList,
            };

            wasSubsetted = localWasSubsetted;
            return format4;
        }
    }
}
