// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType
{
    internal class OpenTypeCMap
    {
        public OpenTypeCMap(OpenTypePlatformID platformID, int encodingID, IList<OpenTypeCMapRange> ranges)
        {
            PlatformID = platformID;
            EncodingID = encodingID;
            Ranges = ranges;
        }

        public OpenTypePlatformID PlatformID { get; }

        public int EncodingID { get; }

        public IList<OpenTypeCMapRange> Ranges { get; }

        public string? ToUnicode(uint glyphIndex)
        {
            for (var i = 0; i < Ranges.Count; i++)
            {
                var range = Ranges[i];

                if (glyphIndex >= range.StartGlyphIndex &&
                    glyphIndex <= range.EndGlyphIndex)
                {
                    var unicode = range.StartUnicode + (glyphIndex - range.StartGlyphIndex);

                    return Utf16Encoding.EncodeCodePoint(unicode);
                }
            }

            return null;
        }
    }
}
