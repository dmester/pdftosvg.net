// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using PdfToSvg.Fonts.CharStrings;
using PdfToSvg.Fonts.CompactFonts;
using PdfToSvg.Fonts.OpenType;
using PdfToSvg.Fonts.OpenType.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.Type1
{
    internal class Type1Converter
    {
        public static OpenTypeFont ConvertToOpenType(Type1FontInfo info)
        {
            CharString emptyCharString;

            if (info.CharStrings == null || !info.CharStrings.TryGetValue(".notdef", out emptyCharString))
            {
                emptyCharString = CharString.Empty;
            }

            var cffFontSet = new CompactFontSet();
            var cff = new CompactFont(cffFontSet);
            cffFontSet.Fonts.Add(cff);

            var empty = new CompactFontGlyph(emptyCharString, "\0", 0, cffFontSet.Strings.Lookup(".notdef"), ".notdef", 0);
            cff.Glyphs.Add(empty);
            cff.CharSet.Add(0);

            if (info.CharStrings != null)
            {
                foreach (var charString in info.CharStrings)
                {
                    if (charString.Key == ".notdef") continue;

                    var sid = cffFontSet.Strings.AddOrLookup(charString.Key);

                    AdobeGlyphList.TryGetUnicode(charString.Key, out var aglUnicode);

                    var glyph = new CompactFontGlyph(
                        charString.Value,
                        unicode: aglUnicode ?? "",
                        glyphIndex: cff.Glyphs.Count,
                        sid,
                        charString.Key,
                        width: charString.Value.Width ?? 0);

                    cff.Glyphs.Add(glyph);
                    cff.CharSet.Add(sid);
                }
            }

            cff.Name = info.FontName ?? "Untitled";

            cff.TopDict.Notice = info.Notice;
            cff.TopDict.FamilyName = info.FamilyName;
            cff.TopDict.FullName = info.FullName;

            cff.TopDict.UnderlinePosition = info.UnderlinePosition;
            cff.TopDict.UnderlineThickness = info.UnderlineThickness;
            cff.TopDict.Weight = info.Weight;
            cff.TopDict.IsFixedPitch = info.isFixedPitch;
            cff.TopDict.ItalicAngle = info.ItalicAngle;
            cff.TopDict.PaintType = info.PaintType;
            cff.TopDict.FontMatrix = info.FontMatrix ?? new double[] { 0.001, 0, 0, 0.001, 0, 0 };
            cff.TopDict.FontBBox = info.FontBBox ?? new double[] { 0, 0, 0, 0 };

            cff.PrivateDict.StdHW = info.StdHW?.FirstOrDefault();
            cff.PrivateDict.StdVW = info.StdVW?.FirstOrDefault();
            cff.PrivateDict.StemSnapH = info.StemSnapH ?? new double[0];
            cff.PrivateDict.BlueScale = info.BlueScale;
            cff.PrivateDict.BlueValues = info.BlueValues ?? new double[0];

            var openTypeFont = new OpenTypeFont();
            openTypeFont.Tables.Add(new CffTable { Content = cffFontSet });
            return openTypeFont;
        }
    }
}
