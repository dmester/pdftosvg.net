// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.CMaps
{
    internal class CustomCMap : CMap
    {
        private const int ExpandRangesThreshold = 100;

        private readonly CMap? parentCMap;
        private readonly bool isUnicodeCMap;

        private readonly List<CMapCodeSpaceRange> codeSpaceRanges;

        private readonly CharCodeLookup charCodes;
        private readonly CharCodeLookup notDefCharCodes;

        private readonly CidLookup cids;
        private readonly CidLookup notDefCids;

        public override bool IsUnicodeCMap => isUnicodeCMap;

        public CustomCMap(CMapData data, CMap? parentCMap = null)
        {
            this.parentCMap = parentCMap;
            isUnicodeCMap = data.IsUnicodeCMap;

            charCodes = new CharCodeLookup(data.CidRanges, data.CidChars, false);
            notDefCharCodes = new CharCodeLookup(data.NotDefRanges, data.NotDefChars, true);

            cids = new CidLookup(data.CidRanges, data.CidChars, false);
            notDefCids = new CidLookup(data.NotDefRanges, data.NotDefChars, true);

            codeSpaceRanges = data.CodeSpaceRanges.ToList();
        }

        private class CidLookup
        {
            private readonly Dictionary<uint, uint> chars;
            private readonly List<CMapRange> ranges;
            private readonly bool isNotDef;

            public CidLookup(List<CMapRange> ranges, List<CMapChar> chars, bool isNotDef)
            {
                this.ranges = new(ranges.Count);
                this.chars = new(chars.Count);

                foreach (var range in ranges)
                {
                    var length = range.ToCharCode - range.FromCharCode;
                    if (length < ExpandRangesThreshold)
                    {
                        for (var i = 0u; i <= length; i++)
                        {
                            var charCode = range.FromCharCode + i;
                            var cid = range.StartValue + (isNotDef ? 0 : i);

                            this.chars[charCode] = cid;
                        }
                    }
                    else
                    {
                        this.ranges.Add(range);
                    }
                }

                foreach (var ch in chars)
                {
                    this.chars[ch.CharCode] = ch.Cid;
                }

                this.isNotDef = isNotDef;
                this.ranges.Sort(CMapRangeComparer.Instance);
            }

            public uint? GetCid(uint charCode)
            {
                // Char
                if (chars.TryGetValue(charCode, out var cid))
                {
                    return cid;
                }

                // Range
                var searchRange = new CMapRange(charCode, charCode, 1, 0);
                var rangeIndex = ranges.BinarySearch(searchRange, CMapRangeComparer.Instance);
                if (rangeIndex >= 0)
                {
                    var range = ranges[rangeIndex];
                    var offset = isNotDef ? 0 : (charCode - range.FromCharCode);
                    return range.StartValue + offset;
                }

                return null;
            }
        }

        private class CharCodeLookup
        {
            private const uint MaxRangeCharCodes = 500;

            private readonly ILookup<uint, uint> chars;
            private readonly List<CMapRange> ranges;
            private readonly bool isNotDef;

            public CharCodeLookup(List<CMapRange> ranges, List<CMapChar> chars, bool isNotDef)
            {
                var expandedRanges = ranges
                    .Where(range => range.ToCharCode - range.FromCharCode < ExpandRangesThreshold)
                    .SelectMany(range => Enumerable
                        .Range(0, (int)(range.ToCharCode - range.FromCharCode + 1))
                        .Select(i => (uint)i)
                        .Select(i => new CMapChar(
                            range.FromCharCode + i,
                            range.CharCodeLength,
                            range.StartValue + (isNotDef ? 0 : i)))
                        );

                this.chars = Enumerable.Concat(chars, expandedRanges)
                    .ToLookup(ch => ch.Cid, ch => ch.CharCode);

                this.ranges = ranges
                    .Where(range => range.ToCharCode - range.FromCharCode >= ExpandRangesThreshold)
                    .OrderBy(range => range.StartValue)
                    .ToList();

                this.isNotDef = isNotDef;
            }

            public IEnumerable<uint> GetCharCodes(uint cid)
            {
                foreach (var charCode in chars[cid])
                {
                    yield return charCode;
                }

                for (var i = 0; i < ranges.Count; i++)
                {
                    var range = ranges[i];

                    if (cid < range.StartValue)
                    {
                        break;
                    }

                    if (isNotDef)
                    {
                        if (cid == range.StartValue)
                        {
                            var length = Math.Min(MaxRangeCharCodes, range.ToCharCode - range.FromCharCode) + 1;

                            for (var j = 0u; j < length; j++)
                            {
                                yield return range.FromCharCode + j;
                            }
                        }
                    }
                    else
                    {
                        if (cid - range.StartValue <= range.ToCharCode - range.FromCharCode)
                        {
                            yield return range.FromCharCode + (cid - range.StartValue);
                        }
                    }
                }
            }
        }

        public override CMapCharCode GetCharCode(PdfString str, int offset)
        {
            var charCode = 0u;

            for (var codeLength = 1; codeLength <= 4 && offset + codeLength <= str.Length; codeLength++)
            {
                charCode = (charCode << 8) | str[offset + codeLength - 1];

                for (var codeSpaceRangeIndex = 0; codeSpaceRangeIndex < codeSpaceRanges.Count; codeSpaceRangeIndex++)
                {
                    var codeSpace = codeSpaceRanges[codeSpaceRangeIndex];

                    if (codeSpace.CharCodeLength == codeLength)
                    {
                        for (var byteIndex = 0; byteIndex < 4; byteIndex++)
                        {
                            var shift = byteIndex * 8;

                            var lo = (codeSpace.FromCharCode >> shift) & 0xff;
                            var hi = (codeSpace.ToCharCode >> shift) & 0xff;
                            var cur = (charCode >> shift) & 0xff;

                            if (cur < lo || cur > hi)
                            {
                                goto OutsideRange;
                            }
                        }

                        return new CMapCharCode(charCode, codeLength);

                    OutsideRange:;
                    }
                }
            }

            if (parentCMap != null)
            {
                return parentCMap.GetCharCode(str, offset);
            }

            return default;
        }

        public override uint? GetCid(uint charCode)
        {
            var cid = cids.GetCid(charCode);

            if (cid == null && parentCMap != null)
            {
                cid = parentCMap.GetCid(charCode);
            }

            return cid;
        }

        public override uint? GetNotDef(uint charCode)
        {
            var cid = notDefCids.GetCid(charCode);

            if (cid == null && parentCMap != null)
            {
                cid = parentCMap.GetNotDef(charCode);
            }

            return cid;
        }

        private IEnumerable<uint> GetUnfilteredCharCodes(uint cid)
        {
            var lookups = new[] { charCodes, notDefCharCodes };
            foreach (var lookup in lookups)
            {
                foreach (var charCode in lookup.GetCharCodes(cid))
                {
                    yield return charCode;
                }
            }

            if (parentCMap != null)
            {
                var parentCharCodes = parentCMap is CustomCMap customCMap
                    ? customCMap.GetUnfilteredCharCodes(cid)
                    : parentCMap.GetCharCodes(cid);

                foreach (var charCode in parentCharCodes)
                {
                    yield return charCode;
                }
            }
        }

        public override IEnumerable<uint> GetCharCodes(uint cid)
        {
            var charCodes = new HashSet<uint>();

            var lookups = new[] { this.charCodes, notDefCharCodes };
            foreach (var lookup in lookups)
            {
                foreach (var charCode in lookup.GetCharCodes(cid))
                {
                    if (charCodes.Add(charCode))
                    {
                        yield return charCode;
                    }
                }
            }

            if (parentCMap != null)
            {
                var parentCharCodes = parentCMap is CustomCMap customCMap
                    ? customCMap.GetUnfilteredCharCodes(cid)
                    : parentCMap.GetCharCodes(cid);

                foreach (var charCode in parentCharCodes)
                {
                    // Ensure mapping is not overridden by a descendant CMap
                    var backCid = GetCid(charCode) ?? GetNotDef(charCode);
                    if (backCid == cid)
                    {
                        if (charCodes.Add(charCode))
                        {
                            yield return charCode;
                        }
                    }
                }
            }
        }
    }
}
