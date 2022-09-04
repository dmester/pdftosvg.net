// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Fonts.CharStrings;
using PdfToSvg.Fonts.CompactFonts;
using PdfToSvg.Fonts.OpenType.Enums;
using PdfToSvg.Fonts.OpenType.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType.Conversion
{
    internal class CffToOtfConverter
    {
        private CompactFont font;

        private List<CompactFontGlyph> nonZeroWidthGlyphs;
        private bool isItalic;
        private FontWeight fontWeight;
        private FontWidth fontWidth;
        private ushort unitsPerEm;

        private short xMin;
        private short xMax;
        private short yMin;
        private short yMax;

        private CffToOtfConverter(CompactFont font)
        {
            this.font = font;

            nonZeroWidthGlyphs = font
                .Glyphs
                .Where(x => x.CharString.MinX < x.CharString.MaxX)
                .ToList();

            // Add an empty character if there are no non-zero width characters to simplify logics further down
            if (nonZeroWidthGlyphs.Count == 0)
            {
                nonZeroWidthGlyphs.Add(new CompactFontGlyph(CharString.Empty, "\ufffd", 0, 0, null, 0));
            }

            isItalic = font.TopDict.ItalicAngle != 0;
            fontWeight = ConversionUtils.ParseFontWeight(font.TopDict.Weight);
            fontWidth = ConversionUtils.ParseFontWidth(font.Name);

            unitsPerEm = (ushort)1000;

            var fontMatrix = font.TopDict.FontMatrix;

            if (fontMatrix.Length == 6)
            {
                if (fontMatrix[0] != fontMatrix[3] ||
                    Math.Abs(fontMatrix[1]) > 0.001 ||
                    Math.Abs(fontMatrix[2]) > 0.001 ||
                    Math.Abs(fontMatrix[4]) > 0.001 ||
                    Math.Abs(fontMatrix[5]) > 0.001
                    )
                {
                    throw new OpenTypeException(
                        "This CFF font contains a font matrix with rotation, skew or non-proportional scaling. " +
                        "Matrix: [" + string.Join(" ", fontMatrix) + "]");
                }

                if (fontMatrix[0] != 0)
                {
                    unitsPerEm = (ushort)(1 / fontMatrix[0]);
                }
            }

            xMin = (short)nonZeroWidthGlyphs.Min(x => x.CharString.MinX);
            xMax = (short)Math.Ceiling(nonZeroWidthGlyphs.Max(x => x.CharString.MaxX));
            yMin = (short)nonZeroWidthGlyphs.Min(x => x.CharString.MinY);
            yMax = (short)Math.Ceiling(nonZeroWidthGlyphs.Max(x => x.CharString.MaxY));
        }

        private HeadTable CreateHead()
        {
            var head = new HeadTable();

            // The dates are hard-coded to ensure deterministic output.
            // This ensures fonts can be reused between pages.
            head.Created = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            head.Modified = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            head.Flags = 0;
            head.UnitsPerEm = unitsPerEm;

            head.MinX = xMin;
            head.MaxX = xMax;
            head.MinY = yMin;
            head.MaxY = yMax;

            head.MacStyle =
                (fontWeight > FontWeight.Normal ? MacStyle.Bold : 0) |
                (isItalic ? MacStyle.Italic : 0) |
                (fontWidth < FontWidth.Medium ? MacStyle.Condensed : 0) |
                (fontWidth > FontWidth.Medium ? MacStyle.Extended : 0);

            head.LowestRecPPEM = 8;

            head.FontDirectionHint = 2;

            head.FontRevision = 1;

            return head;
        }

        private HheaTable CreateHhea()
        {
            var hhea = new HheaTable();

            hhea.Ascender = yMax;
            hhea.Descender = yMin;

            hhea.LineGap = 0;

            hhea.AdvanceWidthMax = (ushort)Math.Ceiling(nonZeroWidthGlyphs.Max(x => x.Width));
            hhea.MinLeftSideBearing = (short)nonZeroWidthGlyphs.Min(x => x.CharString.MinX);
            hhea.MinRightSideBearing = (short)nonZeroWidthGlyphs.Min(x => x.Width - x.CharString.MaxX);
            hhea.MaxXExtent = xMax;

            hhea.CaretSlopeRise = 1;

            hhea.NumberOfHMetrics = (ushort)font.Glyphs.Count;

            return hhea;
        }

        private NameTable CreateName(CMapTable cmap)
        {
            var fontFamily = font.TopDict.FamilyName ?? font.Name;
            var fontSubFamily = font.TopDict.Weight ?? "Regular";

            if (isItalic)
            {
                fontSubFamily = (fontSubFamily + " Italic").TrimStart();
            }

            var nameValues = new[]
            {
                new { ID = OpenTypeNameID.Copyright, Value = font.TopDict.Notice ?? "" },
                new { ID = OpenTypeNameID.FontFamily, Value = fontFamily},
                new { ID = OpenTypeNameID.FontSubfamily, Value = fontSubFamily },
                new { ID = OpenTypeNameID.UniqueId, Value = "PdfToSvg.NET : " + fontFamily + " " + fontSubFamily },
                new { ID = OpenTypeNameID.FullFontName, Value = fontFamily + " " + fontSubFamily },
                new { ID = OpenTypeNameID.Version, Value = "Version 1.0" },
                new { ID = OpenTypeNameID.PostScriptName, Value = ConversionUtils.PostScriptName(font.Name) },
            };

            var nameRecords = cmap.EncodingRecords
                .SelectMany(cmap => nameValues
                    .Select(x =>
                    {
                        var isWindows = cmap.PlatformID == OpenTypePlatformID.Windows;
                        var encoding = isWindows ? Encoding.BigEndianUnicode : Encoding.ASCII;

                        return new NameRecord
                        {
                            PlatformID = cmap.PlatformID,
                            EncodingID = cmap.EncodingID,
                            LanguageID = isWindows ? (ushort)0x0409 : (ushort)0,
                            NameID = x.ID,
                            Content = encoding.GetBytes(x.Value ?? "")
                        };
                    }))
                .Where(x => x.Content.Length > 0)

                // Order stipulated by spec
                .OrderBy(x => x.PlatformID)
                .ThenBy(x => x.EncodingID)
                .ThenBy(x => x.LanguageID)
                .ThenBy(x => x.NameID)

                .ToArray();

            return new NameTable { NameRecords = nameRecords };
        }

        private OS2Table CreateOS2()
        {
            var os2 = new OS2Table();

            var x = nonZeroWidthGlyphs
                .Where(x => x.UnicodeCodePoint == 'x' || x.UnicodeCodePoint == 'e' || x.UnicodeCodePoint == 'o')
                .OrderByDescending(x => x.UnicodeCodePoint)
                .FirstOrDefault();

            var H = nonZeroWidthGlyphs
                .FirstOrDefault(x => x.UnicodeCodePoint == 'H');

            var unicodeRanges = ConversionUtils.GetUnicodeRanges(font.Glyphs.Select(x => x.UnicodeCodePoint));

            os2.AvgXCharWidth = (short)nonZeroWidthGlyphs.Average(x => x.Width);

            os2.WeightClass = fontWeight;
            os2.WidthClass = fontWidth;

            // Subscript and superscript position not available in CFF. Use hard-coded values.
            os2.SubscriptXSize = ConversionUtils.ToFWord(650, unitsPerEm);
            os2.SubscriptYSize = ConversionUtils.ToFWord(600, unitsPerEm);
            os2.SubscriptXOffset = 0;
            os2.SubscriptYOffset = ConversionUtils.ToFWord(75, unitsPerEm);
            os2.SuperscriptXSize = os2.SubscriptXSize;
            os2.SuperscriptYSize = os2.SubscriptYSize;
            os2.SuperscriptXOffset = 0;
            os2.SuperscriptYOffset = ConversionUtils.ToFWord(350, unitsPerEm);

            os2.StrikeoutSize = (short)font.TopDict.UnderlineThickness;

            if (x != null && x.CharString.MaxY >= 1)
            {
                os2.StrikeoutPosition = ConversionUtils.ToFWord(500, (int)x.CharString.MaxY);
            }
            else
            {
                os2.StrikeoutPosition = ConversionUtils.ToFWord(400, yMax);
            }

            // See PANOSE spec: https://monotype.github.io/panose/pan1.htm
            // We can't tell this information from the CFF file, so let's keep it empty.
            os2.Panose = new byte[10];

            os2.UnicodeRange1 = unicodeRanges[0];
            os2.UnicodeRange2 = unicodeRanges[1];
            os2.UnicodeRange3 = unicodeRanges[2];
            os2.UnicodeRange4 = unicodeRanges[3];

            os2.Selection =
                (isItalic ? SelectionFlags.Italic : 0) |
                (fontWeight > FontWeight.Regular ? SelectionFlags.Bold : 0) |
                (fontWeight == FontWeight.Regular ? SelectionFlags.Regular : 0);

            // The CharIndex fields does not support supplementary characters.
            const uint MaxCharIndex = 0xFFFF;
            var charIndex = font.Glyphs.Select(x => Math.Min(MaxCharIndex, x.UnicodeCodePoint));

            os2.FirstCharIndex = (ushort)charIndex.DefaultIfEmpty().Min();
            os2.LastCharIndex = (ushort)charIndex.DefaultIfEmpty(MaxCharIndex).Max();

            // These could probably be picked better
            os2.TypoAscender = yMax;
            os2.TypoDescender = yMin;
            os2.TypoLineGap = 0;

            // fontbakery says:
            // A font's winAscent and winDescent values should be greater than the head
            // table's yMax, abs(yMin) values. If they are less than these values,
            // clipping can occur on Windows platforms
            // (https://github.com/RedHatBrand/Overpass/issues/33).
            os2.WinAscent = (ushort)yMax;
            os2.WinDescent = (ushort)Math.Abs(yMin);

            // According to spec, the following fields should be specified by which code pages that are considered
            // functional. This is very subjective, so let's hard-code only Latin 1.
            os2.CodePageRange1 = 0b1;
            os2.CodePageRange2 = 0;

            os2.XHeight = (short)(x == null ? 0 : x.CharString.MaxY);
            os2.CapHeight = (short)(H == null ? 0 : H.CharString.MaxY);

            os2.DefaultChar = 0;
            os2.BreakChar = 32;

            // Kerning not supported => context of 1 is enough
            os2.MaxContext = 1;

            // Fonts not using multiple optical-size variants should use value 0 - 0xffff
            os2.LowerOpticalPointSize = 0;
            os2.UpperOpticalPointSize = 0xffff;

            return os2;
        }

        private HmtxTable CreateHmtx()
        {
            var hmtx = new HmtxTable();

            hmtx.HorMetrics = font
                .Glyphs
                .Select(x => new LongHorMetricRecord
                {
                    AdvanceWidth = (ushort)x.Width,
                    LeftSideBearing = 0,
                })
                .ToArray();

            hmtx.LeftSideBearings = new short[0];

            return hmtx;
        }

        private CMapTable CreateCMap()
        {
            var glyphs = font.Glyphs
                .Select((ch, index) => new
                {
                    CodePoint = ch.UnicodeCodePoint,
                    GlyphID = (uint)index,
                })
                .Where(x => x.CodePoint != 0xFFFD || x.GlyphID == 0)
                .OrderBy(x => x.CodePoint);

            CMapFormat12Group? previous = null;
            var groups = new List<CMapFormat12Group>();

            foreach (var glyph in glyphs)
            {
                if (previous != null)
                {
                    if (previous.EndCharCode >= glyph.CodePoint)
                    {
                        // Overlapping, skip
                        continue;
                    }
                    else if (
                        previous.EndCharCode + 1 == glyph.CodePoint &&
                        previous.StartGlyphID + (previous.EndCharCode - previous.StartCharCode) + 1 == glyph.GlyphID)
                    {
                        // Merge with previous
                        previous.EndCharCode = glyph.CodePoint;
                        continue;
                    }
                }

                previous = new CMapFormat12Group
                {
                    StartCharCode = glyph.CodePoint,
                    EndCharCode = glyph.CodePoint,
                    StartGlyphID = glyph.GlyphID,
                };

                groups.Add(previous);
            }

            var encodings = new List<CMapEncodingRecord>();

            // Encoding 4 (Windows Unicode BMP)
            var bmpRanges = groups
                .Where(x => x.StartCharCode < 0xffff)
                .ToList();

            if (bmpRanges.Count > 0)
            {
                var format4 = new CMapFormat4
                {
                    Language = 0,
                    StartCode = new ushort[bmpRanges.Count + 1],
                    EndCode = new ushort[bmpRanges.Count + 1],
                    IdDelta = new short[bmpRanges.Count + 1],
                    IdRangeOffsets = new ushort[bmpRanges.Count + 1],
                    GlyphIdArray = new ushort[0],
                };

                for (var i = 0; i < bmpRanges.Count; i++)
                {
                    var range = bmpRanges[i];

                    format4.StartCode[i] = (ushort)range.StartCharCode;
                    format4.EndCode[i] = (ushort)Math.Min(range.EndCharCode, 0xfffe);
                    format4.IdDelta[i] = unchecked((short)(range.StartGlyphID - range.StartCharCode));
                    format4.IdRangeOffsets[i] = 0;
                }

                // According to spec, last range must map [0xffff, 0xffff] to glyph 0
                var last = format4.EndCode.Length - 1;
                format4.StartCode[last] = 0xffff;
                format4.EndCode[last] = 0xffff;
                format4.IdDelta[last] = 1;

                encodings.Add(new CMapEncodingRecord
                {
                    PlatformID = OpenTypePlatformID.Windows,
                    EncodingID = 1,
                    Content = format4,
                });
            }

            // Encoding 10 (Windows Unicode full)
            encodings.Add(new CMapEncodingRecord
            {
                PlatformID = OpenTypePlatformID.Windows,
                EncodingID = 10,
                Content = new CMapFormat12
                {
                    Language = 0,
                    Groups = groups.ToArray(),
                }
            });

            return new CMapTable
            {
                EncodingRecords = encodings.ToArray(),
            };
        }

        private MaxpTableV05 CreateMaxp()
        {
            return new MaxpTableV05
            {
                NumGlyphs = (ushort)font.Glyphs.Count,
            };
        }

        private PostTable CreatePost()
        {
            var table = new PostTableV3();

            if (!font.IsCIDFont)
            {
                table.GlyphNames = font.Glyphs
                    .Select(glyph => glyph.CharName ?? ".notdef")
                    .ToArray();
            }

            table.UnderlinePosition = (short)font.TopDict.UnderlinePosition;
            table.UnderlineThickness = (short)font.TopDict.UnderlineThickness;

            return table;
        }

        private RawTable CreateCff()
        {
            return new RawTable
            {
                Tag = "CFF ",
                Content = CompactFontBuilder.Build(font.FontSet, inlineSubrs: true),
            };
        }

        private OpenTypeFont ToOpenType()
        {
            var otf = new OpenTypeFont();

            var cmap = CreateCMap();

            otf.Tables.Add(CreateHead());
            otf.Tables.Add(CreateHhea());
            otf.Tables.Add(CreateCff());
            otf.Tables.Add(CreateMaxp());
            otf.Tables.Add(cmap);
            otf.Tables.Add(CreatePost());
            otf.Tables.Add(CreateHmtx());
            otf.Tables.Add(CreateOS2());
            otf.Tables.Add(CreateName(cmap));

            return otf;
        }

        public static OpenTypeFont Convert(CompactFont font)
        {
            return new CffToOtfConverter(font).ToOpenType();
        }
    }
}
