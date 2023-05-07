// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Fonts.CharStrings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CompactFonts
{
    internal class CompactFontBuilder
    {
        private readonly CompactFontSet fontSet;
        private readonly CompactFontWriter writer = new CompactFontWriter();
        private readonly bool inlineSubrs;

        private CompactFontBuilder(CompactFontSet fontSet, bool inlineSubrs)
        {
            this.fontSet = fontSet;
            this.inlineSubrs = inlineSubrs;
        }

        public static string SanitizeName(string input)
        {
            const int MaxLength = 63;
            var result = new char[Math.Min(input.Length, MaxLength)];

            for (var i = 0; i < result.Length; i++)
            {
                var ch = input[i];

                // Invalid chars according to CFF spec section 7
                if (ch < 33 ||
                    ch > 126 ||
                    ch == '[' ||
                    ch == ']' ||
                    ch == '(' ||
                    ch == ')' ||
                    ch == '{' ||
                    ch == '}' ||
                    ch == '<' ||
                    ch == '>' ||
                    ch == '/' ||
                    ch == '%' ||
                    ch == ' ' ||
                    ch == '\t' ||
                    ch == '\r' ||
                    ch == '\n' ||
                    ch == '\f')
                {
                    ch = '_';
                }

                result[i] = ch;
            }

            return new string(result);
        }

        private void WriteNameIndex()
        {
            var nameIndex = fontSet.Fonts
                .Select(font => string.IsNullOrEmpty(font.Name) ? "Untitled" : font.Name)
                .Select(name => SanitizeName(name))
                .Select(name => Encoding.ASCII.GetBytes(name))
                .Select(arr => new ArraySegment<byte>(arr))
                .ToList();

            writer.WriteIndex(nameIndex);
        }

        private void WriteStringIndex()
        {
            var stringIndex = fontSet.Strings.CustomStrings
                .Select(str => Encoding.ASCII.GetBytes(str))
                .Select(arr => new ArraySegment<byte>(arr))
                .ToList();

            writer.WriteIndex(stringIndex);
        }

        private void WriteGlobalSubrs()
        {
            IList<ArraySegment<byte>> subrIndex;

            if (inlineSubrs)
            {
                subrIndex = new ArraySegment<byte>[0];
            }
            else
            {
                subrIndex = fontSet.Subrs
                    .Select(subr => subr.Used ? subr.Content : new ArraySegment<byte>(ArrayUtils.Empty<byte>(), 0, 0))
                    .ToList();
            }

            writer.WriteIndex(subrIndex);
        }

        private void MaximizeTopDictOffsets()
        {
            foreach (var font in fontSet.Fonts)
            {
                if (font.TopDict.Charset != 0)
                {
                    font.TopDict.Charset = int.MaxValue;
                }

                if (font.TopDict.Encoding != 0)
                {
                    font.TopDict.Encoding = int.MaxValue;
                }

                font.TopDict.CharStrings = int.MaxValue;

                font.TopDict.Private = new[] { int.MaxValue, int.MaxValue };

                font.TopDict.FDArray = font.IsCIDFont ? int.MaxValue : null;
                font.TopDict.FDSelect = font.IsCIDFont ? int.MaxValue : null;
            }
        }

        private void WriteTopDictIndex()
        {
            var topDictData = new List<ArraySegment<byte>>();

            foreach (var font in fontSet.Fonts)
            {
                var dict = new List<KeyValuePair<int, double[]>>();
                CompactFontDictSerializer.Serialize(dict, font.TopDict, new CompactFontDict(), fontSet.Strings);

                var dictWriter = new CompactFontWriter();
                dictWriter.WriteDict(dict);
                topDictData.Add(dictWriter.GetBuffer());
            }

            writer.WriteIndex(topDictData);
        }

        public void WriteFontSet()
        {
            writer.WriteHeader(new CompactFontHeader
            {
                Major = 1,
                Minor = 0,
                HdrSize = 4,
                OffSize = 2,
            });

            MaximizeTopDictOffsets();

            WriteNameIndex();

            var topDictPosition = writer.Position;
            WriteTopDictIndex();
            WriteStringIndex();
            WriteGlobalSubrs();

            foreach (var font in fontSet.Fonts)
            {
                WriteFont(font);
            }

            writer.Position = topDictPosition;
            WriteTopDictIndex();
            WriteStringIndex();
            WriteGlobalSubrs();
        }

        private class CharSetRange
        {
            public int FirstSID;
            public int Left;
        }

        private bool WritePredefinedCharset(CompactFont font)
        {
            // CID fonts are not allowed to use predefined charsets according to spec chapter 18
            if (font.IsCIDFont)
            {
                return false;
            }

            var predefinedCharsets = CompactFontPredefinedCharsets.Charsets;

            for (var charsetId = 0; charsetId < predefinedCharsets.Length; charsetId++)
            {
                var predefinedCharset = predefinedCharsets[charsetId];
                if (predefinedCharset.Length >= font.Glyphs.Count)
                {
                    var isMatch = true;

                    for (var i = 0; i < font.Glyphs.Count; i++)
                    {
                        if (font.Glyphs[i].SID != predefinedCharset[i])
                        {
                            isMatch = false;
                            break;
                        }
                    }

                    if (isMatch)
                    {
                        font.TopDict.Charset = charsetId;
                        return true;
                    }
                }
            }

            return false;
        }

        private void WriteCustomCharset(CompactFont font)
        {
            font.TopDict.Charset = writer.Position;

            var ranges = new List<CharSetRange>();
            var lastRange = (CharSetRange?)null;

            // The .notdef char is not included in the charset
            foreach (var glyph in font.Glyphs.Skip(1))
            {
                if (lastRange == null || lastRange.FirstSID + lastRange.Left + 1 != glyph.SID)
                {
                    lastRange = new CharSetRange
                    {
                        FirstSID = glyph.SID,
                        Left = 0,
                    };
                    ranges.Add(lastRange);
                }
                else
                {
                    lastRange.Left++;
                }
            }

            if (ranges.Count > font.Glyphs.Count * 2 / 3)
            {
                writer.WriteCard8(0); // format

                foreach (var glyph in font.Glyphs.Skip(1))
                {
                    writer.WriteSID(glyph.SID);
                }
            }
            else if (ranges.All(x => x.Left < 256))
            {
                writer.WriteCard8(1); // format

                foreach (var range in ranges)
                {
                    writer.WriteSID(range.FirstSID);
                    writer.WriteCard8(range.Left);
                }
            }
            else
            {
                writer.WriteCard8(2); // format

                foreach (var range in ranges)
                {
                    writer.WriteSID(range.FirstSID);
                    writer.WriteCard16(range.Left);
                }
            }
        }

        private void WriteCharset(CompactFont font)
        {
            if (!WritePredefinedCharset(font))
            {
                WriteCustomCharset(font);
            }
        }

        private void WriteFont(CompactFont font)
        {
            font.TopDict.Charset = 0;
            font.TopDict.Encoding = 0;
            font.TopDict.FDArray = null;

            WriteCharStrings(font);

            WriteCharset(font);

            if (font.IsCIDFont)
            {
                WriteFDSelect(font);
                WriteFDArray(font);
            }

            WritePrivateDictAndSubrs(font.TopDict, font.PrivateDict, font.Subrs);
        }

        private void WriteCharStrings(CompactFont font)
        {
            Func<CompactFontGlyph, ArraySegment<byte>> mapper;

            if (inlineSubrs)
            {
                mapper = glyph =>
                {
                    var charStringWriter = new Type2CharStringWriter();

                    if (glyph.CharString.Width.HasValue)
                    {
                        charStringWriter.WriteLexeme(CharStringLexeme.Operand(glyph.CharString.Width.Value));
                    }

                    foreach (var lexeme in glyph.CharString.ContentInlinedSubrs)
                    {
                        charStringWriter.WriteLexeme(lexeme);
                    }

                    return charStringWriter.GetBuffer();
                };
            }
            else
            {
                mapper = glyph =>
                {
                    var charStringWriter = new Type2CharStringWriter();

                    foreach (var lexeme in glyph.CharString.Content)
                    {
                        charStringWriter.WriteLexeme(lexeme);
                    }

                    return charStringWriter.GetBuffer();
                };
            }

            var charStringIndex = font.Glyphs.Select(mapper).ToList();

            font.TopDict.CharStrings = writer.Position;
            writer.WriteIndex(charStringIndex);
        }

        private ArraySegment<byte> SerializeDict<T>(T dict) where T : new()
        {
            var dictWriter = new CompactFontWriter();
            WriteDict(dictWriter, dict);
            return dictWriter.GetBuffer();
        }

        private int EstimateDictSize<T>(T dict) where T : new()
        {
            var dictData = new List<KeyValuePair<int, double[]>>();
            CompactFontDictSerializer.Serialize(dictData, dict, new T(), fontSet.Strings);

            var dictWriter = new CompactFontWriter();
            dictWriter.WriteDict(dictData);
            return dictWriter.Length;
        }

        private void WriteDict<T>(CompactFontWriter dictWriter, T dict) where T : new()
        {
            var dictData = new List<KeyValuePair<int, double[]>>();
            CompactFontDictSerializer.Serialize(dictData, dict, new T(), fontSet.Strings);
            dictWriter.WriteDict(dictData);
        }

        private void WritePrivateDictAndSubrs(CompactFontDict fontDict, CompactFontPrivateDict privateDict, IList<CharStringSubRoutine> subrs)
        {
            var startPosition = writer.Position;

            if (subrs.Count > 0 && !inlineSubrs)
            {
                // Private dict
                privateDict.Subrs = ushort.MaxValue;
                privateDict.Subrs = EstimateDictSize(privateDict);

                WriteDict(writer, privateDict);
                fontDict.Private = new int[] { writer.Position - startPosition, startPosition };

                writer.Position = startPosition + privateDict.Subrs.Value;

                // Subrs
                var subrsData = new List<ArraySegment<byte>>(subrs.Count);

                foreach (var subr in subrs)
                {
                    subrsData.Add(subr.Content);
                }

                writer.WriteIndex(subrsData);
            }
            else
            {
                privateDict.Subrs = null;
                WriteDict(writer, privateDict);
                fontDict.Private = new int[] { writer.Position - startPosition, startPosition };
            }
        }

        private void WriteFDArray(CompactFont font)
        {
            var fdArrayData = new List<ArraySegment<byte>>();

            foreach (var subFont in font.FDArray)
            {
                WritePrivateDictAndSubrs(
                    subFont.FontDict,
                    subFont.PrivateDict,
                    font.Subrs == subFont.Subrs ? new CharStringSubRoutine[0] : subFont.Subrs);

                fdArrayData.Add(SerializeDict(subFont.FontDict));
            }

            font.TopDict.FDArray = writer.Position;
            writer.WriteIndex(fdArrayData);
        }

        private class FDSelectRange
        {
            public int FirstGlyphIndex;
            public int LastGlyphIndex;
            public int FDIndex;
        }

        private void WriteFDSelect(CompactFont font)
        {
            font.TopDict.FDSelect = writer.Position;

            var ranges = new List<FDSelectRange>();
            var lastRange = (FDSelectRange?)null;

            for (var glyphIndex = 0; glyphIndex < font.FDSelect.Count; glyphIndex++)
            {
                var fdIndex = font.FDSelect[glyphIndex];

                if (lastRange == null || lastRange.FDIndex != fdIndex)
                {
                    lastRange = new FDSelectRange
                    {
                        FirstGlyphIndex = glyphIndex,
                        LastGlyphIndex = glyphIndex,
                        FDIndex = fdIndex,
                    };
                    ranges.Add(lastRange);
                }
                else
                {
                    lastRange.LastGlyphIndex = glyphIndex;
                }
            }

            const int Format = 3;
            writer.WriteCard8(Format);
            writer.WriteCard16(ranges.Count);

            foreach (var range in ranges)
            {
                writer.WriteCard16(range.FirstGlyphIndex);
                writer.WriteCard8(range.FDIndex);
            }

            var sentinel = (ranges.LastOrDefault()?.LastGlyphIndex ?? 0) + 1;
            writer.WriteCard16(sentinel);
        }

        public static byte[] Build(CompactFontSet fontSet, bool inlineSubrs)
        {
            var serializer = new CompactFontBuilder(fontSet, inlineSubrs);
            serializer.WriteFontSet();
            return serializer.writer.ToArray();
        }
    }
}
