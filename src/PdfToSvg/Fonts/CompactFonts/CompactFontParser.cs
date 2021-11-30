// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Encodings;
using PdfToSvg.Fonts.CharStrings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CompactFonts
{
    internal class CompactFontParser
    {
        private readonly CompactFontReader reader;
        private readonly byte[] data;
        private readonly IDictionary<uint, string>? customCMap;

        private readonly List<CompactFont> fonts = new List<CompactFont>();

        private CompactFontStringTable strings;
        private int[] globalSubrIndex = ArrayUtils.Empty<int>();

        private CompactFontParser(byte[] data, IDictionary<uint, string>? customCMap)
        {
            reader = new CompactFontReader(data);
            this.data = data;
            this.customCMap = customCMap;
        }

        private void ReadDict<T>(T dict, int position, int length)
        {
            reader.Position = position;

            var dictData = reader.ReadDict(length);
            CompactFontDictSerializer.Deserialize(dict, dictData, strings);
        }

        private void ReadFDSelect(IList<int> fdSelect, int nGlyphs)
        {
            var format = reader.ReadCard8();

            switch (format)
            {
                case 0:
                    for (var i = 0; i < nGlyphs - 1; i++)
                    {
                        var sid = reader.ReadSID();

                        fdSelect.Add(sid);
                    }
                    break;

                case 3:
                    var nRanges = reader.ReadCard16();

                    var first = 0;
                    var fd = 0;

                    for (var i = 0; i <= nRanges; i++)
                    {
                        var nextFirst = reader.ReadCard16();

                        if (i > 0)
                        {
                            for (var j = first; j < nextFirst; j++)
                            {
                                fdSelect.Add(fd);
                            }
                        }

                        if (i < nRanges)
                        {
                            fd = reader.ReadCard8();
                            first = nextFirst;
                        }
                    }
                    break;

                default:
                    throw new CompactFontException("Invalid FDSelect format " + format + ".");
            }
        }

        private void ReadCharset(IList<int> charset, int nGlyphs)
        {
            var format = reader.ReadCard8();

            charset.Add(0);

            switch (format)
            {
                case 0:
                    for (var i = 0; i < nGlyphs - 1; i++)
                    {
                        var sid = reader.ReadSID();

                        charset.Add(sid);
                    }
                    break;

                case 1:
                    while (charset.Count < nGlyphs)
                    {
                        var sid = reader.ReadSID();
                        var nLeft = reader.ReadCard8();

                        for (var i = 0; i <= nLeft; i++)
                        {
                            charset.Add(sid + i);
                        }
                    }
                    break;

                case 2:
                    while (charset.Count < nGlyphs)
                    {
                        var sid = reader.ReadSID();
                        var nLeft = reader.ReadCard16();

                        for (var i = 0; i <= nLeft; i++)
                        {
                            charset.Add(sid + i);
                        }
                    }
                    break;

                default:
                    throw new CompactFontException("Invalid CFF charset format " + format + ".");
            }
        }

        private void ReadFont(CompactFont font)
        {
            var localSubrIndex = ArrayUtils.Empty<int>();

            var charset = new List<int>();
            var isCidFont = font.TopDict.FDArray != null;

            var fdSelect = new List<int>();
            var fdArray = new List<CompactFontDict>();
            var fdArrayLocalSubrIndex = new List<int[]>();

            // Ensure supported char string type
            if (font.TopDict.CharstringType != 2)
            {
                throw new CompactFontException("Char strings of type " + font.TopDict.CharstringType + " currently not supported.");
            }

            // Private DICT
            if (font.TopDict.Private.Length == 2)
            {
                var privateDictStart = font.TopDict.Private[1];
                ReadDict(font.PrivateDict, privateDictStart, font.TopDict.Private[0]);

                if (font.PrivateDict.Subrs != null)
                {
                    reader.Position = font.PrivateDict.Subrs.Value + privateDictStart;
                    localSubrIndex = reader.ReadIndex();
                }
            }

            // Charstrings index
            reader.Position = font.TopDict.CharStrings;
            var charStringsIndex = reader.ReadIndex();
            var nGlyphs = charStringsIndex.Length - 1;

            // Charset
            if (font.TopDict.Charset > 0)
            {
                reader.Position = font.TopDict.Charset;
                ReadCharset(charset, nGlyphs);
            }

            // FDSelect
            if (font.TopDict.FDSelect != null)
            {
                reader.Position = font.TopDict.FDSelect.Value;
                ReadFDSelect(fdSelect, nGlyphs);
            }

            // FDArray
            if (font.TopDict.FDArray != null)
            {
                reader.Position = font.TopDict.FDArray.Value;

                var fdArrayIndex = reader.ReadIndex();

                for (var j = 0; j + 1 < fdArrayIndex.Length; j++)
                {
                    reader.Position = fdArrayIndex[j];
                    var fdDictData = reader.ReadDict(fdArrayIndex[j + 1] - fdArrayIndex[j]);

                    var fdDict = new CompactFontDict();
                    CompactFontDictSerializer.Deserialize(fdDict, fdDictData, strings);

                    var fdLocalSubrIndex = localSubrIndex;

                    if (fdDict.Private.Length == 2)
                    {
                        var privateDictStart = fdDict.Private[1];
                        var fdPrivateDict = new CompactFontPrivateDict();

                        ReadDict(fdPrivateDict, privateDictStart, fdDict.Private[0]);

                        if (fdPrivateDict.Subrs != null)
                        {
                            reader.Position = fdPrivateDict.Subrs.Value + privateDictStart;
                            fdLocalSubrIndex = reader.ReadIndex();
                        }
                    }

                    fdArray.Add(fdDict);
                    fdArrayLocalSubrIndex.Add(fdLocalSubrIndex);
                }
            }

            // Glyphs
            for (var glyphIndex = 0; glyphIndex < charset.Count; glyphIndex++)
            {
                var charLocalSubrIndex = localSubrIndex;

                if (glyphIndex < fdSelect.Count)
                {
                    var fdIndex = fdSelect[glyphIndex];
                    if (fdIndex < fdArrayLocalSubrIndex.Count)
                    {
                        charLocalSubrIndex = fdArrayLocalSubrIndex[fdIndex];
                    }
                }

                var startIndex = charStringsIndex[glyphIndex];
                var endIndex = glyphIndex + 1 < charStringsIndex.Length ? charStringsIndex[glyphIndex + 1] : data.Length;
                var cidOrSid = charset[glyphIndex];

                var glyph = ReadGlyph(font, glyphIndex, startIndex, endIndex, charLocalSubrIndex, cidOrSid, isCidFont);
                font.Glyphs.Add(glyph);
            }
        }

        private CompactFontGlyph ReadGlyph(CompactFont font, int glyphIndex, int startIndex, int endIndex, int[] charLocalSubrIndex, int cidOrSid, bool isCidFont)
        {
            string unicode;
            var charString = (CharString?)null;
            double width, minX, maxX, minY, maxY;

            if (isCidFont)
            {
                if (customCMap == null ||
                    customCMap.TryGetValue((uint)cidOrSid, out unicode) == false)
                {
                    unicode = Utf16Encoding.GetPrivateUseChar(cidOrSid);
                }
            }
            else
            {
                var charName = strings.Lookup(cidOrSid);

                AdobeGlyphList.TryMap(charName, out unicode!);

                if (unicode == null)
                {
                    unicode = "";
                }
            }

            try
            {
                charString = Type2CharStringParser.Parse(data, startIndex, endIndex, globalSubrIndex, charLocalSubrIndex);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Failed to parse char '" + unicode + "'. " + ex);
            }

            if (charString == null || charString.Width == null)
            {
                width = font.PrivateDict.DefaultWidthX;
            }
            else
            {
                width = font.PrivateDict.NominalWidthX + charString.Width.Value;
            }

            if (charString == null ||
                charString.MinX >= charString.MaxX ||
                charString.MinY >= charString.MaxY)
            {
                minX = 0;
                maxX = 0;
                minY = 0;
                maxY = 0;
            }
            else
            {
                minX = charString.MinX;
                maxX = charString.MaxX;
                minY = charString.MinY;
                maxY = charString.MaxY;
            }

            return new CompactFontGlyph(unicode, glyphIndex, width, minX, maxX, minY, maxY);
        }

        private CompactFontSet Read()
        {
            var header = reader.ReadHeader();

            if (header.Major != 1)
            {
                throw new CompactFontException("Unsupported CFF version " + header.Major + "." + header.Minor + ".");
            }

            reader.Position = header.HdrSize;

            var nameIndex = reader.ReadIndex();
            var topDictIndex = reader.ReadIndex();
            var stringIndex = reader.ReadIndex();
            globalSubrIndex = reader.ReadIndex();

            var names = reader.ReadStrings(nameIndex);
            strings = new CompactFontStringTable(reader.ReadStrings(stringIndex));

            for (var i = 0; i + 1 < topDictIndex.Length; i++)
            {
                var font = new CompactFont();
                ReadDict(font.TopDict, topDictIndex[i], topDictIndex[i + 1] - topDictIndex[i]);

                font.Content = data;

                if (i < names.Length)
                {
                    font.Name = names[i];
                }
                else if (0 < names.Length)
                {
                    font.Name = names[0];
                }

                ReadFont(font);

                fonts.Add(font);
            }

            return new CompactFontSet(fonts);
        }

        public static CompactFontSet Parse(byte[] data, IDictionary<uint, string>? customCMap)
        {
            return new CompactFontParser(data, customCMap).Read();
        }
    }
}
