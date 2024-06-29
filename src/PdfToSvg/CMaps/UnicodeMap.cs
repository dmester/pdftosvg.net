// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg.CMaps
{
    internal abstract class UnicodeMap
    {
        public abstract string? GetUnicode(uint charCode);

        public static UnicodeMap Empty { get; } = new UnicodeEmptyMap();
        public static UnicodeMap Identity { get; } = new UnicodeIdentityMap();

        public static UnicodeMap Link(CMap encoding, UnicodeMap unicodeMap)
        {
            return new LinkedUnicodeCMap(encoding, unicodeMap);
        }

        public static UnicodeMap Create(CMapData cmapData)
        {
            return new UnicodeCMap(cmapData);
        }

        public static UnicodeMap Create(PdfStream stream, CancellationToken cancellationToken)
        {
            var cmapData = CMapParser.Parse(stream, cancellationToken);
            return new UnicodeCMap(cmapData);
        }

        private class LinkedUnicodeCMap : UnicodeMap
        {
            private readonly CMap encoding;
            private readonly UnicodeMap unicodeMap;

            public LinkedUnicodeCMap(CMap encoding, UnicodeMap unicodeMap)
            {
                this.encoding = encoding;
                this.unicodeMap = unicodeMap;
            }

            public override string? GetUnicode(uint charCode)
            {
                var cid = encoding.GetCid(charCode);
                if (cid == null)
                {
                    return null;
                }
                else
                {
                    return unicodeMap.GetUnicode(cid.Value);
                }
            }
        }

        private class UnicodeCMap : UnicodeMap
        {
            private const int ExpandRangesThreshold = 100;

            private readonly Dictionary<uint, string> bfChars;
            private readonly List<CMapRange> bfRanges;

            public UnicodeCMap(CMapData data)
            {
                bfRanges = new(data.BfRanges.Count);
                bfChars = new(data.BfChars.Count);

                foreach (var range in data.BfRanges)
                {
                    var length = range.ToCharCode - range.FromCharCode;
                    if (length < ExpandRangesThreshold)
                    {
                        var startCodePoint = range.StartValue;

                        for (var i = 0u; i <= length; i++)
                        {
                            var codePoint = startCodePoint + i;
                            var unicode = Utf16Encoding.EncodeCodePoint(codePoint);

                            if (unicode != null)
                            {
                                bfChars[range.FromCharCode + i] = unicode;
                            }
                        }
                    }
                    else
                    {
                        bfRanges.Add(range);
                    }
                }

                foreach (var ch in data.BfChars)
                {
                    if (ch.Unicode != null)
                    {
                        bfChars[ch.CharCode] = ch.Unicode;
                    }
                }

                bfRanges.Sort(CMapRangeComparer.Instance);
            }

            public override string? GetUnicode(uint charCode)
            {
                // Bf char
                if (bfChars.TryGetValue(charCode, out var unicode))
                {
                    return unicode;
                }

                // Bf range
                var searchBfRange = new CMapRange(charCode, charCode, 1, 0);
                var bfRangeIndex = bfRanges.BinarySearch(searchBfRange, CMapRangeComparer.Instance);
                if (bfRangeIndex >= 0)
                {
                    var bfRange = bfRanges[bfRangeIndex];

                    var offset = charCode - bfRange.FromCharCode;
                    var codePoint = bfRange.StartValue + offset;

                    return Utf16Encoding.EncodeCodePoint(codePoint);
                }

                return null;
            }

        }

        private class UnicodeIdentityMap : UnicodeMap
        {
            public override string? GetUnicode(uint charCode) => new string((char)charCode, 1);
        }

        private class UnicodeEmptyMap : UnicodeMap
        {
            public override string? GetUnicode(uint charCode) => null;
        }

    }
}
