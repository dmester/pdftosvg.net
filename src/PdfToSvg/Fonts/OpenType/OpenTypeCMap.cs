// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

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

                    if (unicode < 0xFFFF)
                    {
                        return unchecked((char)unicode).ToString();
                    }

                    if (unicode < 0x10FFFF)
                    {
                        // Convert to UTF-16
                        // https://en.wikipedia.org/wiki/UTF-16#U+0000_to_U+D7FF_and_U+E000_to_U+FFFF

                        var u = unicode - 0x10000;
                        var highSurrogate = unchecked((char)(0xD800 | (u >> 10)));
                        var lowSurrogate = unchecked((char)(0xDC00 | (u & 0x3ff)));

                        return new string(new[] { highSurrogate, lowSurrogate });
                    }
                }
            }

            return null;
        }
    }
}
