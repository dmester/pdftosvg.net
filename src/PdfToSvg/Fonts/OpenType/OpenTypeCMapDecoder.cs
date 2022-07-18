// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Fonts.OpenType.Tables;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType
{
    internal static class OpenTypeCMapDecoder
    {
        public static OpenTypeCMap? GetCMap(CMapEncodingRecord encoding)
        {
            var ranges = GetRanges(encoding.Content);

            return ranges != null
                ? new OpenTypeCMap(encoding.PlatformID, encoding.EncodingID, ranges)
                : null;
        }

        public static List<OpenTypeCMapRange>? GetRanges(ICMapFormat? cmap)
        {
            switch (cmap)
            {
                case CMapFormat0 f0: return GetRanges(f0);
                case CMapFormat4 f4: return GetRanges(f4);
                case CMapFormat12 f12: return GetRanges(f12);
                default: return null;
            }
        }

        public static List<OpenTypeCMapRange> GetRanges(CMapFormat0 cmap)
        {
            var ranges = new List<OpenTypeCMapRange>();

            for (var i = 0u; i < cmap.GlyphIdArray.Length; i++)
            {
                var glyphId = cmap.GlyphIdArray[i];
                if (glyphId != 0)
                {
                    ranges.Add(new OpenTypeCMapRange(
                        startUnicode: i,
                        endUnicode: i,
                        startGlyphIndex: glyphId
                        ));
                }
            }

            return ranges;
        }

        public static List<OpenTypeCMapRange> GetRanges(CMapFormat4 cmap)
        {
            var ranges = new List<OpenTypeCMapRange>();

            for (var i = 0; i < cmap.StartCode.Length; i++)
            {
                if (cmap.IdRangeOffsets[i] == 0)
                {
                    ranges.Add(new OpenTypeCMapRange(
                        startUnicode: cmap.StartCode[i],
                        endUnicode: cmap.EndCode[i],
                        startGlyphIndex: (ushort)(cmap.StartCode[i] + cmap.IdDelta[i]) // Modulo 65536
                        ));
                }
                else
                {
                    var startCode = cmap.StartCode[i];
                    var endCode = cmap.EndCode[i];

                    for (var code = startCode; ;)
                    {
                        // Specification:
                        // https://docs.microsoft.com/en-us/typography/opentype/spec/cmap#format-4-segment-mapping-to-delta-values
                        //
                        // Pseudo code in spec:
                        // glyphId = *(idRangeOffset[i]/2 + (c - startCode[i]) + &idRangeOffset[i])

                        var glyphIdIndex =
                            cmap.IdRangeOffsets[i] / 2 - (cmap.IdRangeOffsets.Length - i)
                            + (code - startCode);

                        if (glyphIdIndex >= 0 && glyphIdIndex < cmap.GlyphIdArray.Length)
                        {
                            var glyphId = cmap.GlyphIdArray[glyphIdIndex];

                            ranges.Add(new OpenTypeCMapRange(
                                startUnicode: code,
                                endUnicode: code,
                                startGlyphIndex: (ushort)(glyphId + cmap.IdDelta[i]) // Modulo 65536
                                ));
                        }

                        // Prevent infinity loop on overflow
                        if (code < endCode)
                        {
                            code++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            return ranges;
        }

        public static List<OpenTypeCMapRange> GetRanges(CMapFormat12 cmap)
        {
            var ranges = new List<OpenTypeCMapRange>();

            foreach (var group in cmap.Groups)
            {
                ranges.Add(new OpenTypeCMapRange(
                    startUnicode: group.StartCharCode,
                    endUnicode: group.EndCharCode,
                    startGlyphIndex: group.StartGlyphID
                    ));
            }

            return ranges;
        }

    }
}
