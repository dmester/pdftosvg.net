// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Encodings;
using PdfToSvg.Fonts.OpenType.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType
{
    internal class OpenTypeCMap
    {
        private readonly List<OpenTypeCMapRange> rangesByGlyphIndex;
        private ReadOnlyCollection<OpenTypeCMapRange>? readOnlyRanges;

        public OpenTypeCMap(OpenTypePlatformID platformID, int encodingID, IEnumerable<OpenTypeCMapRange> ranges)
        {
            rangesByGlyphIndex = ranges.ToList();
            rangesByGlyphIndex.Sort((a, b) => Comparer<uint>.Default.Compare(a.StartGlyphIndex, b.StartGlyphIndex));

            PlatformID = platformID;
            EncodingID = encodingID;
        }

        public OpenTypePlatformID PlatformID { get; }

        public int EncodingID { get; }

        public ReadOnlyCollection<OpenTypeCMapRange> Ranges
        {
            get => readOnlyRanges ??= new ReadOnlyCollection<OpenTypeCMapRange>(rangesByGlyphIndex);
        }

        public IEnumerable<OpenTypeCMapChar> Chars
        {
            get
            {
                foreach (var range in rangesByGlyphIndex)
                {
                    for (var glyphIndex = range.StartGlyphIndex; ; glyphIndex++)
                    {
                        yield return new OpenTypeCMapChar(
                            range.StartUnicode + (glyphIndex - range.StartGlyphIndex), glyphIndex);

                        if (glyphIndex == range.EndGlyphIndex)
                        {
                            break;
                        }
                    }
                }
            }
        }

        public string? ToUnicode(uint glyphIndex)
        {
            var min = 0;
            var max = rangesByGlyphIndex.Count - 1;

            while (min <= max)
            {
                var mid = min + ((max - min) >> 1);
                var range = rangesByGlyphIndex[mid];

                if (glyphIndex < range.StartGlyphIndex)
                {
                    max = mid - 1;
                }
                else if (glyphIndex > range.EndGlyphIndex)
                {
                    min = mid + 1;
                }
                else
                {
                    var unicode = range.StartUnicode + (glyphIndex - range.StartGlyphIndex);
                    return Utf16Encoding.EncodeCodePoint(unicode);
                }
            }

            return null;
        }
    }
}
