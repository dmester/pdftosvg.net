// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Encodings;
using PdfToSvg.Fonts.OpenType.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg.Fonts.OpenType
{
    internal class OpenTypeCMap
    {
        private readonly List<OpenTypeCMapRange> rangesByUnicode;
        private ReadOnlyCollection<OpenTypeCMapRange>? readOnlyRanges;
        private readonly Lazy<uint[]> unicodeLookup;

        public OpenTypeCMap(OpenTypePlatformID platformID, int encodingID, IEnumerable<OpenTypeCMapRange> ranges)
        {
            rangesByUnicode = ranges.ToList();
            rangesByUnicode.Sort(x => x.StartUnicode, x => x.StartGlyphIndex);

            PlatformID = platformID;
            EncodingID = encodingID;

            unicodeLookup = new(() =>
            {
                if (rangesByUnicode.Count > 0)
                {
                    var maxGlyphIndex = rangesByUnicode.Max(x => x.EndGlyphIndex);
                    var unicode = new uint[maxGlyphIndex + 1];

                    foreach (var ch in Chars)
                    {
                        unicode[ch.GlyphIndex] = ch.Unicode;
                    }

                    return unicode;
                }
                else
                {
                    return new uint[0];
                }
            }, LazyThreadSafetyMode.PublicationOnly);
        }

        public OpenTypePlatformID PlatformID { get; }

        public int EncodingID { get; }

        public ReadOnlyCollection<OpenTypeCMapRange> Ranges
        {
            get => readOnlyRanges ??= new ReadOnlyCollection<OpenTypeCMapRange>(rangesByUnicode);
        }

        public IEnumerable<OpenTypeCMapChar> Chars
        {
            get
            {
                foreach (var range in rangesByUnicode)
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

        public uint? ToGlyphIndex(string unicode)
        {
            if (unicode == null) throw new ArgumentNullException(nameof(unicode));
            if (unicode.Length == 0) throw new ArgumentException("Unicode string cannot be empty.", nameof(unicode));

            var codePoint = Utf16Encoding.DecodeCodePoint(unicode, 0, out var length);
            if (unicode.Length != length)
            {
                // Contains several characters
                return null;
            }

            return ToGlyphIndex(codePoint);
        }

        public uint? ToGlyphIndex(uint unicodeCodePoint)
        {
            var min = 0;
            var max = rangesByUnicode.Count - 1;

            while (min <= max)
            {
                var mid = min + ((max - min) >> 1);
                var range = rangesByUnicode[mid];

                if (unicodeCodePoint < range.StartUnicode)
                {
                    max = mid - 1;
                }
                else if (unicodeCodePoint > range.EndUnicode)
                {
                    min = mid + 1;
                }
                else
                {
                    var glyphIndex = range.StartGlyphIndex + (unicodeCodePoint - range.StartUnicode);
                    return glyphIndex;
                }
            }

            return null;
        }

        public string? ToUnicode(uint glyphIndex)
        {
            var lookup = unicodeLookup.Value;
            if (glyphIndex < lookup.Length)
            {
                return Utf16Encoding.EncodeCodePoint(lookup[glyphIndex]);
            }

            return null;
        }
    }
}
