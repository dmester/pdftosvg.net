// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Fonts.CharStrings;
using PdfToSvg.Fonts.CompactFonts;
using PdfToSvg.Fonts.OpenType.Conversion;
using PdfToSvg.Fonts.OpenType.Enums;
using PdfToSvg.Fonts.OpenType.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType
{
    internal class OpenTypeSanitizer
    {
        private readonly OpenTypeFont font;

        private OpenTypeSanitizer(OpenTypeFont font)
        {
            this.font = font;
        }

        public static void Sanitize(OpenTypeFont font)
        {
            new OpenTypeSanitizer(font).Sanitize();
        }

        private void Sanitize()
        {
            var cff = GetCff();

            HeadTable head;
            CMapTable cmap;
            HmtxTable hmtx;
            NameTable name;
            HheaTable? hhea;
            PostTable post;
            MaxpTable maxp;
            OS2Table os2;

            if (cff == null)
            {
                // TrueType
                head = GetOrThrow<HeadTable>();
                hmtx = GetOrThrow<HmtxTable>();
                maxp = GetOrThrow<MaxpTable>();
                cmap = GetOrCreate(() => CreateEmptyCMap());
                hhea = GetOrNull<HheaTable>();
            }
            else
            {
                // CFF OpenType
                var glyphs = GetGlyphs(cff);
                head = GetOrCreate(() => CreateHead(cff, glyphs));
                maxp = GetOrCreate(() => CreateMaxp(cff));
                hmtx = GetOrCreate(() => CreateHmtx(cff));
                cmap = GetOrCreate(() => CreateCMap(cff));
                hhea = GetOrCreate(() => CreateHhea(head, cff, glyphs));
            }

            post = GetOrCreate(() => CreatePost(head, cff));
            os2 = GetOrCreate(() => CreateOS2(head, hmtx, cff));
            name = Replace(CreateName(head, cmap, font.Names, cff));

            UpdateOS2(os2, head, cmap, cff);
            UpdateHmtx(hmtx, maxp);

            if (hhea != null)
            {
                UpdateHhea(hhea, hmtx);
            }
        }

        private List<CompactFontGlyph> GetGlyphs(CompactFont cff)
        {
            var result = cff
                .Glyphs
                .Where(x => x.CharString.MinX < x.CharString.MaxX)
                .ToList();

            // Add an empty character if there are no non-zero width characters to simplify logics further down
            if (result.Count == 0)
            {
                result.Add(new CompactFontGlyph(new CharString(), "\ufffd", 0, 0, null, 0));
            }

            return result;
        }

        private T GetOrThrow<T>() where T : IBaseTable
        {
            var table = font.Tables.OfType<T>().FirstOrDefault();
            if (table == null)
            {
                throw new OpenTypeException(nameof(T) + " missing in font and cannot be generated.");
            }

            return table;
        }

        private T GetOrCreate<T>(Func<T> factory) where T : IBaseTable
        {
            var table = font.Tables.OfType<T>().FirstOrDefault();

            if (table == null)
            {
                table = factory();
                font.Tables.Add(table);
            }

            return table;
        }

        private T? GetOrNull<T>() where T : IBaseTable
        {
            return font.Tables.OfType<T>().FirstOrDefault();
        }

        private T Replace<T>(T newTable) where T : IBaseTable
        {
            font.Tables.Remove<NameTable>();
            font.Tables.Add(newTable);
            return newTable;
        }

        private CompactFont? GetCff()
        {
            var cffTable = font.Tables.FirstOrDefault(x => x.Tag == "CFF ");
            if (cffTable is CffTable parsedCffTable)
            {
                return parsedCffTable.Content?.Fonts.FirstOrDefault();
            }
            else if (cffTable is RawTable rawCffTable && rawCffTable.Content != null)
            {
                return CompactFontParser.Parse(rawCffTable.Content, maxFontCount: 1).Fonts.FirstOrDefault();
            }
            else
            {
                return null;
            }
        }

        private HeadTable CreateHead(CompactFont cff, List<CompactFontGlyph> glyphs)
        {
            var unitsPerEm = (ushort)1000;

            var fontMatrix = cff.TopDict.FontMatrix;

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

            var xMin = (short)glyphs.Min(x => x.CharString.MinX);
            var xMax = (short)Math.Ceiling(glyphs.Max(x => x.CharString.MaxX));
            var yMin = (short)glyphs.Min(x => x.CharString.MinY);
            var yMax = (short)Math.Ceiling(glyphs.Max(x => x.CharString.MaxY));

            var isItalic = cff.TopDict.ItalicAngle != 0;
            var fontWeight = ConversionUtils.ParseFontWeight(cff.TopDict.Weight);
            var fontWidth = ConversionUtils.ParseFontWidth(cff.Name);

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

        private HheaTable CreateHhea(HeadTable head, CompactFont cff, List<CompactFontGlyph> glyphs)
        {
            var hhea = new HheaTable();

            hhea.Ascender = head.MaxY;
            hhea.Descender = head.MinY;

            hhea.LineGap = 0;

            hhea.AdvanceWidthMax = (ushort)Math.Ceiling(glyphs.Max(x => x.Width));
            hhea.MinLeftSideBearing = (short)glyphs.Min(x => x.CharString.MinX);
            hhea.MinRightSideBearing = (short)glyphs.Min(x => x.Width - x.CharString.MaxX);
            hhea.MaxXExtent = head.MaxX;

            hhea.CaretSlopeRise = 1;

            hhea.NumberOfHMetrics = (ushort)cff.Glyphs.Count;

            return hhea;
        }

        private NameTable CreateName(HeadTable head, CMapTable cmap, OpenTypeNames names, CompactFont? cff)
        {
            string name;
            string fontFamily;
            string fontSubFamily;
            string copyright;
            string version;

            if (cff == null)
            {
                name = "Untitled exported font";
                fontFamily = "Untitled exported font";
                fontSubFamily = "Regular";
                copyright = "";
                version = "Version 1.0";
            }
            else
            {
                name = cff.Name;
                fontFamily = cff.TopDict.FamilyName ?? cff.Name;
                fontSubFamily = cff.TopDict.Weight ?? "Regular";
                copyright = cff.TopDict.Notice ?? "";
                version = cff.TopDict.Version ?? "Version 1.0";

                if (head.MacStyle.HasFlag(MacStyle.Italic))
                {
                    fontSubFamily = (fontSubFamily + " Italic").TrimStart();
                }
            }

            var backupNames = new[]
            {
                KeyValuePair.Create(OpenTypeNameID.Copyright, copyright),
                KeyValuePair.Create(OpenTypeNameID.FontFamily, fontFamily),
                KeyValuePair.Create(OpenTypeNameID.FontSubfamily, fontSubFamily),
                KeyValuePair.Create(OpenTypeNameID.UniqueId, "PdfToSvg.NET : " + fontFamily + " " + fontSubFamily),
                KeyValuePair.Create(OpenTypeNameID.FullFontName, fontFamily + " " + fontSubFamily),
                KeyValuePair.Create(OpenTypeNameID.Version, version),
                KeyValuePair.Create(OpenTypeNameID.PostScriptName, name),
            };

            var nameRecords = cmap.EncodingRecords
                .SelectMany(cmap => Enumerable
                    .Concat(names, backupNames)
                    .Select(name => new
                    {
                        CMap = cmap,
                        ID = name.Key,
                        name.Value,
                    }))

                .Where(name => !string.IsNullOrEmpty(name.Value))
                .DistinctBy(name => name.ID)

                .Select(name =>
                {
                    var isWindows = name.CMap.PlatformID == OpenTypePlatformID.Windows;
                    var encoding = isWindows ? Encoding.BigEndianUnicode : Encoding.ASCII;
                    var value = name.Value;

                    if (name.ID == OpenTypeNameID.PostScriptName)
                    {
                        value = ConversionUtils.PostScriptName(value);
                    }

                    return new NameRecord
                    {
                        PlatformID = name.CMap.PlatformID,
                        EncodingID = name.CMap.EncodingID,
                        LanguageID = isWindows ? (ushort)0x0409 : (ushort)0,
                        NameID = name.ID,
                        Content = encoding.GetBytes(value)
                    };
                })

                // Order stipulated by spec
                .OrderBy(x => x.PlatformID)
                .ThenBy(x => x.EncodingID)
                .ThenBy(x => x.LanguageID)
                .ThenBy(x => x.NameID)

                .ToArray();

            return new NameTable { NameRecords = nameRecords };
        }

        private OS2Table CreateOS2(HeadTable head, HmtxTable hmtx, CompactFont? cff)
        {
            var os2 = new OS2Table();

            os2.AvgXCharWidth = (short)hmtx.HorMetrics.Average(x => x.AdvanceWidth);

            if (cff == null)
            {
                os2.WeightClass = FontWeight.Regular;
                os2.WidthClass = FontWidth.Medium;
            }
            else
            {
                os2.WeightClass = ConversionUtils.ParseFontWeight(cff.TopDict.Weight);
                os2.WidthClass = ConversionUtils.ParseFontWidth(cff.Name);
            }

            // Subscript and superscript position not available in CFF. Use hard-coded values.
            os2.SubscriptXSize = ConversionUtils.ToFWord(650, head.UnitsPerEm);
            os2.SubscriptYSize = ConversionUtils.ToFWord(600, head.UnitsPerEm);
            os2.SubscriptXOffset = 0;
            os2.SubscriptYOffset = ConversionUtils.ToFWord(75, head.UnitsPerEm);
            os2.SuperscriptXSize = os2.SubscriptXSize;
            os2.SuperscriptYSize = os2.SubscriptYSize;
            os2.SuperscriptXOffset = 0;
            os2.SuperscriptYOffset = ConversionUtils.ToFWord(350, head.UnitsPerEm);

            if (cff == null)
            {
                os2.StrikeoutSize = (short)(head.UnitsPerEm * 15 / 256);
            }
            else
            {
                os2.StrikeoutSize = (short)cff.TopDict.UnderlineThickness;
            }

            // See PANOSE spec: https://monotype.github.io/panose/pan1.htm
            // We can't tell this information from the CFF file, so let's keep it empty.
            os2.Panose = new byte[10];

            os2.Selection =
                (head.MacStyle.HasFlag(MacStyle.Italic) ? SelectionFlags.Italic : 0) |
                (head.MacStyle.HasFlag(MacStyle.Bold) ? SelectionFlags.Bold : SelectionFlags.Regular);

            // These could probably be picked better
            os2.TypoAscender = head.MaxY;
            os2.TypoDescender = head.MinY;
            os2.TypoLineGap = 0;

            // fontbakery says:
            // A font's winAscent and winDescent values should be greater than the head
            // table's yMax, abs(yMin) values. If they are less than these values,
            // clipping can occur on Windows platforms
            // (https://github.com/RedHatBrand/Overpass/issues/33).
            os2.WinAscent = (ushort)head.MaxY;
            os2.WinDescent = (ushort)Math.Abs(head.MinY);

            // According to spec, the following fields should be specified by which code pages that are considered
            // functional. This is very subjective, so let's hard-code only Latin 1.
            os2.CodePageRange1 = 0b1;
            os2.CodePageRange2 = 0;

            os2.DefaultChar = 0;
            os2.BreakChar = 32;

            // Kerning not supported => context of 1 is enough
            os2.MaxContext = 1;

            // Fonts not using multiple optical-size variants should use value 0 - 0xffff
            os2.LowerOpticalPointSize = 0;
            os2.UpperOpticalPointSize = 0xffff;

            return os2;
        }

        private CompactFontGlyph? LookupGlyph(CompactFont? cff, OpenTypeCMap cmap, string unicode)
        {
            if (cff != null)
            {
                var glyphIndex = cmap.ToGlyphIndex(unicode);
                if (glyphIndex != null && glyphIndex.Value < cff.Glyphs.Count)
                {
                    return cff.Glyphs[(int)glyphIndex.Value];
                }
            }

            return null;
        }

        private void UpdateHmtx(HmtxTable hmtxTable, MaxpTable maxpTable)
        {
            if (hmtxTable.HorMetrics.Length > maxpTable.NumGlyphs)
            {
                hmtxTable.HorMetrics = hmtxTable.HorMetrics
                    .Take(maxpTable.NumGlyphs)
                    .ToArray();
            }

            var expectedLeftSideBearings = maxpTable.NumGlyphs - hmtxTable.HorMetrics.Length;

            if (expectedLeftSideBearings != hmtxTable.LeftSideBearings.Length)
            {
                hmtxTable.LeftSideBearings = hmtxTable.LeftSideBearings
                    .Concat(Enumerable.Repeat((short)0, maxpTable.NumGlyphs))
                    .Take(expectedLeftSideBearings)
                    .ToArray();
            }
        }

        private void UpdateHhea(HheaTable hheaTable, HmtxTable hmtxTable)
        {
            if (hmtxTable.HorMetrics.Length > 0)
            {
                hheaTable.AdvanceWidthMax = hmtxTable.HorMetrics.Max(x => x.AdvanceWidth);
            }
        }

        private void UpdateOS2(OS2Table os2, HeadTable head, CMapTable cmapTable, CompactFont? cff)
        {
            var cmap = cmapTable.EncodingRecords
                .Select(encoding => OpenTypeCMapDecoder.GetCMap(encoding))
                .WhereNotNull()
                .OrderByPriority()
                .FirstOrDefault() ??
                new OpenTypeCMap(OpenTypePlatformID.Windows, 0, Enumerable.Empty<OpenTypeCMapRange>());

            var unicodeRanges = ConversionUtils.GetUnicodeRanges(cmap.Chars.Select(x => x.Unicode));

            var x =
                LookupGlyph(cff, cmap, "x") ??
                LookupGlyph(cff, cmap, "o") ??
                LookupGlyph(cff, cmap, "e");

            var H = LookupGlyph(cff, cmap, "H");

            if (os2.StrikeoutPosition == 0)
            {
                if (x != null && x.CharString.MaxY >= 1)
                {
                    os2.StrikeoutPosition = ConversionUtils.ToFWord(500, (int)x.CharString.MaxY);
                }
                else
                {
                    os2.StrikeoutPosition = ConversionUtils.ToFWord(400, head.MaxY);
                }
            }

            os2.UnicodeRange1 = unicodeRanges[0];
            os2.UnicodeRange2 = unicodeRanges[1];
            os2.UnicodeRange3 = unicodeRanges[2];
            os2.UnicodeRange4 = unicodeRanges[3];

            // The CharIndex fields does not support supplementary characters.
            const ushort MaxCharIndex = 0xFFFF;

            os2.FirstCharIndex = ushort.MaxValue;
            os2.LastCharIndex = ushort.MinValue;

            foreach (var ch in cmap.Chars)
            {
                if (ch.Unicode != 0 && ch.Unicode != 0xFFFF)
                {
                    var charIndex = (ushort)Math.Min(MaxCharIndex, ch.Unicode);

                    if (os2.FirstCharIndex > charIndex)
                    {
                        os2.FirstCharIndex = charIndex;
                    }

                    if (os2.LastCharIndex < charIndex)
                    {
                        os2.LastCharIndex = charIndex;
                    }
                }
            }

            if (os2.FirstCharIndex > os2.LastCharIndex)
            {
                os2.FirstCharIndex = 0;
                os2.LastCharIndex = MaxCharIndex;
            }

            os2.XHeight = (short)(x == null ? os2.XHeight : x.CharString.MaxY);
            os2.CapHeight = (short)(H == null ? os2.CapHeight : H.CharString.MaxY);
        }

        private HmtxTable CreateHmtx(CompactFont cff)
        {
            var hmtx = new HmtxTable();

            hmtx.HorMetrics = cff
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

        private CMapTable CreateEmptyCMap()
        {
            var encodings = new List<CMapEncodingRecord>();

            // Encoding 10 (Windows Unicode full)
            encodings.Add(new CMapEncodingRecord
            {
                PlatformID = OpenTypePlatformID.Windows,
                EncodingID = 10,
                Content = new CMapFormat12
                {
                    Language = 0,
                    Groups = new CMapFormat12Group[0],
                }
            });

            return new CMapTable
            {
                EncodingRecords = encodings.ToArray(),
            };
        }

        private CMapTable CreateCMap(CompactFont cff)
        {
            var glyphs = cff.Glyphs
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

        private MaxpTableV05 CreateMaxp(CompactFont cff)
        {
            return new MaxpTableV05
            {
                NumGlyphs = (ushort)cff.Glyphs.Count,
            };
        }

        private PostTable CreatePost(HeadTable head, CompactFont? cff)
        {
            var table = new PostTableV3();

            if (cff == null)
            {
                table.UnderlinePosition = (short)(-head.UnitsPerEm * 13 / 128);
                table.UnderlineThickness = (short)(head.UnitsPerEm * 15 / 256);
            }
            else
            {
                if (!cff.IsCIDFont)
                {
                    table.GlyphNames = cff.Glyphs
                        .Select(glyph => glyph.CharName ?? ".notdef")
                        .ToArray();
                }

                table.UnderlinePosition = (short)cff.TopDict.UnderlinePosition;
                table.UnderlineThickness = (short)cff.TopDict.UnderlineThickness;
            }

            return table;
        }

    }
}
