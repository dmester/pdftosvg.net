// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.CMaps;
using PdfToSvg.Common;
using PdfToSvg.Encodings;
using PdfToSvg.Fonts.WidthMaps;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg.Fonts
{
    internal class CharMap : IEnumerable<CharInfo>
    {
        private readonly Dictionary<uint, CharInfo> chars = new();
        private bool charsPopulated;

        public bool TryGetChar(uint charCode, [MaybeNullWhen(false)] out CharInfo foundChar) => chars.TryGetValue(charCode, out foundChar);

        private string ResolveUnicode(CharInfo ch, UnicodeMap toUnicode, SingleByteEncoding? explicitEncoding, bool preferSingleChar)
        {
            // PDF 1.7 section 9.10.2

            var pdfUnicode = toUnicode.GetUnicode(ch.CharCode);

            // Prio 1: Single char ToUnicode
            if (pdfUnicode != null && (!preferSingleChar || IsSingleChar(pdfUnicode)))
            {
                return pdfUnicode;
            }

            // Prio 2: Explicit PDF encoding
            if (explicitEncoding != null && ch.CharCode <= byte.MaxValue)
            {
                var encodingUnicode = explicitEncoding.GetUnicode((byte)ch.CharCode);
                if (encodingUnicode != null)
                {
                    return encodingUnicode;
                }
            }

            // Prio 3: Unicode from font CMap
            if (ch.Unicode.Length != 0 && ch.Unicode != CharInfo.NotDef)
            {
                return ch.Unicode;
            }

            // Prio 4: Multi char ToUnicode
            if (pdfUnicode != null)
            {
                return pdfUnicode;
            }

            // Prio 5: Unicode from glyph name
            if (AdobeGlyphList.TryGetUnicode(ch.GlyphName, out var aglUnicode))
            {
                return aglUnicode;
            }

            return CharInfo.NotDef;
        }

        private static bool IsSingleChar(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return false;
            }

            if (s.Length == 1)
            {
                return true;
            }

            Utf16Encoding.DecodeCodePoint(s, 0, out var codePointLength);

            if (codePointLength == s.Length)
            {
                return true;
            }

            return false;
        }

        private static bool IsValidTargetChar(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return false;
            }

            uint codePoint;
            if (s.Length == 1)
            {
                // Incomplete surrogate pair
                if (char.IsSurrogate(s[0]))
                {
                    return false;
                }

                codePoint = s[0];
            }
            else
            {
                codePoint = Utf16Encoding.DecodeCodePoint(s, 0, out var codePointLength);

                // Multiple characters
                if (codePointLength != s.Length)
                {
                    return false;
                }
            }

            // .notdef
            if (codePoint == 0xfffd)
            {
                return false;
            }

            // Unassigned chars
            // https://www.unicode.org/faq/private_use.html#nonchar1
            // Especially ffff causes problems in SVG, since browsers are replacing it with their own .notdef glyph
            if (codePoint >= 0xfdd0 && codePoint <= 0xfdef ||
                (codePoint & 0xfffe) == 0xfffe) // Captures 0xfffe, 0xffff and corresponding from supplementary planes
            {
                return false;
            }

            // Control characters might break the layout in SVG
            if (char.IsControl(s, 0))
            {
                return false;
            }

            // Bidirectional formatting characters might break the layout in SVG
            if (UnicodeBidi.IsFormattingCharacter(s[0]))
            {
                return false;
            }

            return true;
        }

        private void PopulateForEmbeddedFont(IEnumerable<CharInfo> chars, UnicodeMap toUnicode, SingleByteEncoding? explicitEncoding)
        {
            const char StartPrivateUseArea = '\uE000';
            const char EndPrivateUseArea = '\uF8FF';

            var usedUnicodeToGidMappings = new Dictionary<string, uint>();
            var usedUnicode = new HashSet<string>();
            var nextReplacementChar = StartPrivateUseArea;

            foreach (var ch in chars)
            {
                if (this.chars.ContainsKey(ch.CharCode))
                {
                    continue;
                }

                ch.Unicode = ResolveUnicode(ch, toUnicode, explicitEncoding, preferSingleChar: true);
                ch.Unicode = Ligatures.Lookup(ch.Unicode);

                if (ch.GlyphIndex == null)
                {
                    this.chars[ch.CharCode] = ch;
                }
                else if (
                    IsValidTargetChar(ch.Unicode) &&
                    (
                        !usedUnicodeToGidMappings.TryGetValue(ch.Unicode, out var mappedGid) ||
                        mappedGid == ch.GlyphIndex.Value
                    ))
                {
                    // Valid mapping
                    this.chars[ch.CharCode] = ch;
                    usedUnicodeToGidMappings[ch.Unicode] = ch.GlyphIndex.Value;
                }
                else
                {
                    // Remap
                    var replacement = new string(nextReplacementChar, 1);

                    while (!usedUnicode.Add(replacement))
                    {
                        if (nextReplacementChar < EndPrivateUseArea)
                        {
                            nextReplacementChar++;
                            replacement = new string(nextReplacementChar, 1);
                        }
                        else
                        {
                            replacement = null;
                            break;
                        }
                    }

                    if (replacement != null)
                    {
                        ch.Unicode = replacement;
                        nextReplacementChar++;

                        this.chars[ch.CharCode] = ch;
                        usedUnicodeToGidMappings[ch.Unicode] = ch.GlyphIndex.Value;
                    }
                }
            }
        }

        private void PopulateForTextExtract(IEnumerable<CharInfo> chars, UnicodeMap toUnicode, SingleByteEncoding? explicitEncoding)
        {
            foreach (var ch in chars)
            {
                ch.Unicode = ResolveUnicode(ch, toUnicode, explicitEncoding, preferSingleChar: false);
                this.chars.TryAdd(ch.CharCode, ch);
            }
        }

        private void PopulateWidths(WidthMap widthMap)
        {
            // With mapped glyph
            var charsByGlyphIndex = chars
                    .Values
                    .Where(ch => ch.GlyphIndex != null)
                    .GroupBy(ch => ch.GlyphIndex!.Value);

            foreach (var glyph in charsByGlyphIndex)
            {
                // Prefer:
                // 1. Char codes explicitly mapped using a /Differences array (see issue #33).
                // 2. Width of lower char codes if there are multiple char codes mapping to the same
                //    glyph. It is more likely that the PDF producer mapped used chars to a low char code.

                var width = glyph
                    .OrderBy(ch => ch.IsExplicitlyMapped ? 0 : 1)
                    .ThenBy(ch => ch.CharCode)
                    .Select(ch => widthMap.GetWidth(ch))
                    .Where(w => w > 0)
                    .FirstOrDefault();

                foreach (var ch in glyph)
                {
                    ch.Width = width;
                }
            }

            // Without mapped glyph
            foreach (var ch in chars.Values)
            {
                if (ch.GlyphIndex == null)
                {
                    ch.Width = widthMap.GetWidth(ch);
                }
            }
        }

        public bool TryPopulate(Func<IEnumerable<CharInfo>> charEnumerator, UnicodeMap toUnicode, SingleByteEncoding? explicitEncoding, WidthMap widthMap, bool optimizeForEmbeddedFont)
        {
            if (charsPopulated)
            {
                return false;
            }

            if (!Monitor.TryEnter(chars))
            {
                return false;
            }

            try
            {
                if (charsPopulated)
                {
                    return false;
                }

                var chars = charEnumerator();

                if (optimizeForEmbeddedFont)
                {
                    PopulateForEmbeddedFont(chars, toUnicode, explicitEncoding);
                }
                else
                {
                    PopulateForTextExtract(chars, toUnicode, explicitEncoding);
                }

                PopulateWidths(widthMap);

                charsPopulated = true;
                return true;
            }
            finally
            {
                if (!charsPopulated)
                {
                    // Population failed
                    chars.Clear();
                }

                Monitor.Exit(chars);
            }
        }

        public IEnumerator<CharInfo> GetEnumerator()
        {
            if (charsPopulated)
            {
                return chars.Values.GetEnumerator();
            }
            else
            {
                return Enumerable.Empty<CharInfo>().GetEnumerator();
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
