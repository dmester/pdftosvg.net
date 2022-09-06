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
        private const int GlyphArrayGroupThreshold = 5;

        internal static List<List<OpenTypeCMapRange>> GroupRanges(IEnumerable<OpenTypeCMapRange> inputRanges)
        {
            var ranges = inputRanges.ToList();

            ranges.Sort(x => x.StartUnicode, x => x.StartGlyphIndex);

            CombineGlyphRanges(ranges);
            return GroupUnicodeRanges(ranges);
        }

        private static void CombineGlyphRanges(List<OpenTypeCMapRange> ranges)
        {
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
        }

        private static List<List<OpenTypeCMapRange>> GroupUnicodeRanges(List<OpenTypeCMapRange> ranges)
        {
            var groups = new List<List<OpenTypeCMapRange>>();

            List<OpenTypeCMapRange>? group = null;

            foreach (var range in ranges.OrderBy(x => x.StartUnicode))
            {
                var forceOwnGroup = range.EndGlyphIndex - range.StartGlyphIndex > GlyphArrayGroupThreshold;
                if (forceOwnGroup)
                {
                    groups.Add(new List<OpenTypeCMapRange> { range });
                    group = null;
                }
                else
                {
                    if (group == null ||
                        group.Last().EndUnicode + 1 != range.StartUnicode)
                    {
                        group = new List<OpenTypeCMapRange>();
                        groups.Add(group);
                    }

                    group.Add(range);
                }
            }

            return groups;
        }

        public static CMapFormat4 EncodeFormat4(IEnumerable<OpenTypeCMapRange> ranges)
        {
            var groups = GroupRanges(ranges
                .Where(range => range.StartUnicode < 0xffff));

            var startCodes = new ushort[groups.Count + 1];
            var endCodes = new ushort[groups.Count + 1];
            var idDeltas = new short[groups.Count + 1];
            var idRangeOffsets = new ushort[groups.Count + 1];
            var glyphIdList = new List<ushort>();

            var index = 0;

            foreach (var group in groups)
            {
                if (group.Count == 1)
                {
                    var range = group[0];
                    startCodes[index] = (ushort)range.StartUnicode;
                    endCodes[index] = (ushort)Math.Min(range.EndUnicode, 0xfffe);
                    idDeltas[index] = unchecked((short)(range.StartGlyphIndex - range.StartUnicode));
                }
                else
                {
                    startCodes[index] = (ushort)group.First().StartUnicode;
                    endCodes[index] = (ushort)Math.Min(group.Last().EndUnicode, 0xfffe);
                    idRangeOffsets[index] = (ushort)((idRangeOffsets.Length - index + glyphIdList.Count) * 2);

                    foreach (var range in group)
                    {
                        for (var glyph = range.StartGlyphIndex; ;)
                        {
                            glyphIdList.Add((ushort)glyph);

                            // Prevent infinity loop on overflow
                            if (glyph < range.EndGlyphIndex)
                            {
                                glyph++;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }

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
                GlyphIdArray = glyphIdList.ToArray(),
            };

            return format4;
        }
    }
}
