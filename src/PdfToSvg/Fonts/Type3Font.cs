// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing;
using PdfToSvg.Encodings;
using PdfToSvg.Fonts.CharStrings;
using PdfToSvg.Fonts.CompactFonts;
using PdfToSvg.Fonts.OpenType;
using PdfToSvg.Fonts.OpenType.Tables;
using PdfToSvg.Fonts.Type3;
using PdfToSvg.Fonts.WidthMaps;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg.Fonts
{
    internal sealed class Type3Font : BaseFont
    {
        private const int TargetEmSize = 1000;
        private const uint MaxCharCode = byte.MaxValue;

        private List<CharInfo> charInfos = new();
        private byte[]?[] glyphDefinitions = new byte[]?[MaxCharCode + 1];

        private Type3WidthMap? type3WidthMap;

        public Matrix FontMatrix { get; private set; } = Matrix.Identity;

        public override bool CanBeInlined => true;

        protected override void OnInit(CancellationToken cancellationToken)
        {
            base.OnInit(cancellationToken);

            widthMap = type3WidthMap = new Type3WidthMap(fontDict);

            PopulateOpenType(cancellationToken);
        }

        private static Matrix GetFontMatrix(double[] sourceMatrix)
        {
            return new Matrix(
                sourceMatrix[0],
                sourceMatrix[1],
                sourceMatrix[2],
                sourceMatrix[3],
                sourceMatrix[4],
                sourceMatrix[5]);
        }

        private static double[] GetFontBBox(IEnumerable<CompactFontGlyph> glyphs)
        {
            var minX = double.MaxValue;
            var maxX = double.MinValue;
            var minY = double.MaxValue;
            var maxY = double.MinValue;

            foreach (var glyph in glyphs)
            {
                var charString = glyph.CharString;

                if (minX > charString.MinX) minX = charString.MinX;
                if (minY > charString.MinY) minY = charString.MinY;
                if (maxX < charString.MaxX) maxX = charString.MaxX;
                if (maxY < charString.MaxY) maxY = charString.MaxY;
            }

            if (minX == double.MaxValue ||
                minY == double.MaxValue ||
                maxX == double.MinValue ||
                maxY == double.MinValue)
            {
                return new[] { 0d, 0d, 0d, 0d };
            }

            return new[] { minX, minY, maxX, maxY };
        }

        private void PopulateOpenType(CancellationToken cancellationToken)
        {
            var fontMatrixArray = fontDict.GetArrayOrNull<double>(Names.FontMatrix);
            var fontBBoxArray = fontDict.GetArrayOrNull<double>(Names.FontBBox);

            if (fontMatrixArray?.Length != 6 || fontBBoxArray?.Length != 4)
            {
                return;
            }

            FontMatrix = GetFontMatrix(fontMatrixArray);

            var transform = FontMatrix * Matrix.Scale(TargetEmSize, TargetEmSize);

            var charProcs = fontDict.GetDictionaryOrEmpty(Names.CharProcs);
            var encoding = pdfFontEncoding ?? new StandardEncoding();

            // Handle glyphs
            var glyphs = new List<CompactFontGlyph>();

            var emptyGlyph = new CompactFontGlyph(CharString.Empty, "\0", 0, 0, ".notdef", 0);
            glyphs.Add(emptyGlyph);

            var validFont = true;

            for (var charCode = 0u; charCode <= MaxCharCode; charCode++)
            {
                var glyphName = encoding.GetGlyphName((byte)charCode);
                if (glyphName == null)
                {
                    continue;
                }

                var charInfo = new CharInfo
                {
                    CharCode = charCode,
                    GlyphName = glyphName,
                    Unicode = encoding.GetUnicode((byte)charCode) ?? CharInfo.NotDef,
                };
                charInfos.Add(charInfo);

                var charProc = charProcs.GetDictionaryOrNull(new PdfName(glyphName));
                if (charProc == null || charProc.Stream == null)
                {
                    continue;
                }

                byte[] glyphDefinition;

                using (var contentStream = charProc.Stream.OpenDecoded(cancellationToken))
                {
                    glyphDefinition = contentStream.ToArray();
                    glyphDefinitions[charCode] = glyphDefinition;
                }

                if (validFont)
                {
                    var charString = Type3ToCharStringConverter.Convert(glyphDefinition, transform, cancellationToken);
                    if (charString == null)
                    {
                        validFont = false;
                    }
                    else
                    {
                        var glyphIndex = glyphs.Count;
                        charInfo.GlyphIndex = (uint)glyphIndex;

                        var glyph = new CompactFontGlyph(
                            charString,
                            charInfo.Unicode,
                            glyphIndex,
                            sid: glyphIndex,
                            charName: null,
                            width: charString.Width ?? TargetEmSize);

                        glyphs.Add(glyph);
                    }
                }
            }

            if (validFont)
            {
                var cffFontSet = new CompactFontSet();
                var cff = new CompactFont(cffFontSet);
                cffFontSet.Fonts.Add(cff);

                cff.Name = fontDict.GetNameOrNull(Names.FontDescriptor / Names.FontName)?.Value ?? "Type3 font";
                cff.TopDict.FontMatrix = new[] { 1d / TargetEmSize, 0, 0, 1d / TargetEmSize, 0, 0 };
                cff.TopDict.FontBBox = GetFontBBox(glyphs);

                cff.TopDict.ROS = new[] {
                    cffFontSet.Strings.AddOrLookup("Adobe"),
                    cffFontSet.Strings.AddOrLookup("Identity"),
                    0d
                };
                cff.TopDict.CIDCount = glyphs.Count;

                var fdFont = new CompactSubFont();
                cff.FDArray.Add(fdFont);

                foreach (var glyph in glyphs)
                {
                    cff.Glyphs.Add(glyph);
                    cff.CharSet.Add(glyph.GlyphIndex);
                    cff.FDSelect.Add(0);
                }

                var openTypeFont = new OpenTypeFont();
                openTypeFont.Tables.Add(new CffTable { Content = cffFontSet });

                OpenTypeSanitizer.Sanitize(openTypeFont);

                this.openTypeFont = openTypeFont;
            }
        }

        protected override IEnumerable<CharInfo> GetChars() => charInfos;

        public Type3Char GetChar(byte charCode)
        {
            return new Type3Char(
                width: type3WidthMap?.GetWidth(charCode) ?? 0,
                glyphDefinition: glyphDefinitions[charCode]
            );
        }
    }
}
