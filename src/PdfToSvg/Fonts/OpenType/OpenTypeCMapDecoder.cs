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
            return cmap switch
            {
                CMapFormat0 format0 => GetRanges(format0),
                CMapFormat4 format4 => GetRanges(format4),
                CMapFormat6 format6 => GetRanges(format6),
                CMapFormat10 format10 => GetRanges(format10),
                CMapFormat12 format12 => GetRanges(format12),
                _ => null,
            };
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

        public static List<OpenTypeCMapRange> GetRanges(CMapFormat6 cmap)
        {
            var ranges = new List<OpenTypeCMapRange>();

            for (var i = 0u; i < cmap.GlyphIdArray.Length; i++)
            {
                var glyphId = cmap.GlyphIdArray[i];
                if (glyphId != 0)
                {
                    ranges.Add(new OpenTypeCMapRange(
                        startUnicode: cmap.FirstCode + i,
                        endUnicode: cmap.FirstCode + i,
                        startGlyphIndex: glyphId
                        ));
                }
            }

            return ranges;
        }

        public static List<OpenTypeCMapRange> GetRanges(CMapFormat10 cmap)
        {
            var ranges = new List<OpenTypeCMapRange>();

            for (var i = 0u; i < cmap.GlyphIdArray.Length; i++)
            {
                var glyphId = cmap.GlyphIdArray[i];
                if (glyphId != 0)
                {
                    ranges.Add(new OpenTypeCMapRange(
                        startUnicode: cmap.StartCharCode + i,
                        endUnicode: cmap.StartCharCode + i,
                        startGlyphIndex: glyphId
                        ));
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
