// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType.Tables
{
    [DebuggerDisplay("post")]
    internal abstract class PostTable : IBaseTable
    {
        public string Tag => "post";

        public decimal ItalicAngle;
        public short UnderlinePosition;
        public short UnderlineThickness;
        public uint IsFixedPitch;
        public uint MinMemType42;
        public uint MaxMemType42;
        public uint MinMemType1;
        public uint MaxMemType1;

        public string[] GlyphNames = ArrayUtils.Empty<string>();

        // Specification:
        // https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6post.html

        protected static readonly string[] MacintoshNames = new[]
        {
            ".notdef",
            ".null",
            "nonmarkingreturn",
            "space",
            "exclam",
            "quotedbl",
            "numbersign",
            "dollar",
            "percent",
            "ampersand",
            "quotesingle",
            "parenleft",
            "parenright",
            "asterisk",
            "plus",
            "comma",
            "hyphen",
            "period",
            "slash",
            "zero",
            "one",
            "two",
            "three",
            "four",
            "five",
            "six",
            "seven",
            "eight",
            "nine",
            "colon",
            "semicolon",
            "less",
            "equal",
            "greater",
            "question",
            "at",
            "A",
            "B",
            "C",
            "D",
            "E",
            "F",
            "G",
            "H",
            "I",
            "J",
            "K",
            "L",
            "M",
            "N",
            "O",
            "P",
            "Q",
            "R",
            "S",
            "T",
            "U",
            "V",
            "W",
            "X",
            "Y",
            "Z",
            "bracketleft",
            "backslash",
            "bracketright",
            "asciicircum",
            "underscore",
            "grave",
            "a",
            "b",
            "c",
            "d",
            "e",
            "f",
            "g",
            "h",
            "i",
            "j",
            "k",
            "l",
            "m",
            "n",
            "o",
            "p",
            "q",
            "r",
            "s",
            "t",
            "u",
            "v",
            "w",
            "x",
            "y",
            "z",
            "braceleft",
            "bar",
            "braceright",
            "asciitilde",
            "Adieresis",
            "Aring",
            "Ccedilla",
            "Eacute",
            "Ntilde",
            "Odieresis",
            "Udieresis",
            "aacute",
            "agrave",
            "acircumflex",
            "adieresis",
            "atilde",
            "aring",
            "ccedilla",
            "eacute",
            "egrave",
            "ecircumflex",
            "edieresis",
            "iacute",
            "igrave",
            "icircumflex",
            "idieresis",
            "ntilde",
            "oacute",
            "ograve",
            "ocircumflex",
            "odieresis",
            "otilde",
            "uacute",
            "ugrave",
            "ucircumflex",
            "udieresis",
            "dagger",
            "degree",
            "cent",
            "sterling",
            "section",
            "bullet",
            "paragraph",
            "germandbls",
            "registered",
            "copyright",
            "trademark",
            "acute",
            "dieresis",
            "notequal",
            "AE",
            "Oslash",
            "infinity",
            "plusminus",
            "lessequal",
            "greaterequal",
            "yen",
            "mu",
            "partialdiff",
            "summation",
            "product",
            "pi",
            "integral",
            "ordfeminine",
            "ordmasculine",
            "Omega",
            "ae",
            "oslash",
            "questiondown",
            "exclamdown",
            "logicalnot",
            "radical",
            "florin",
            "approxequal",
            "Delta",
            "guillemotleft",
            "guillemotright",
            "ellipsis",
            "nonbreakingspace",
            "Agrave",
            "Atilde",
            "Otilde",
            "OE",
            "oe",
            "endash",
            "emdash",
            "quotedblleft",
            "quotedblright",
            "quoteleft",
            "quoteright",
            "divide",
            "lozenge",
            "ydieresis",
            "Ydieresis",
            "fraction",
            "currency",
            "guilsinglleft",
            "guilsinglright",
            "fi",
            "fl",
            "daggerdbl",
            "periodcentered",
            "quotesinglbase",
            "quotedblbase",
            "perthousand",
            "Acircumflex",
            "Ecircumflex",
            "Aacute",
            "Edieresis",
            "Egrave",
            "Iacute",
            "Icircumflex",
            "Idieresis",
            "Igrave",
            "Oacute",
            "Ocircumflex",
            "apple",
            "Ograve",
            "Uacute",
            "Ucircumflex",
            "Ugrave",
            "dotlessi",
            "circumflex",
            "tilde",
            "macron",
            "breve",
            "dotaccent",
            "ring",
            "cedilla",
            "hungarumlaut",
            "ogonek",
            "caron",
            "Lslash",
            "lslash",
            "Scaron",
            "scaron",
            "Zcaron",
            "zcaron",
            "brokenbar",
            "Eth",
            "eth",
            "Yacute",
            "yacute",
            "Thorn",
            "thorn",
            "minus",
            "multiply",
            "onesuperior",
            "twosuperior",
            "threesuperior",
            "onehalf",
            "onequarter",
            "threequarters",
            "franc",
            "Gbreve",
            "gbreve",
            "Idotaccent",
            "Scedilla",
            "scedilla",
            "Cacute",
            "cacute",
            "Ccaron",
            "ccaron",
            "dcroat",
        };

        void IBaseTable.Write(OpenTypeWriter writer, IList<IBaseTable> tables)
        {
            Write(writer, tables);
        }

        protected abstract void Write(OpenTypeWriter writer, IList<IBaseTable> tables);

        protected void WriteHeader(OpenTypeWriter writer)
        {
            writer.WriteFixed(ItalicAngle);
            writer.WriteInt16(UnderlinePosition);
            writer.WriteInt16(UnderlineThickness);
            writer.WriteUInt32(IsFixedPitch);
            writer.WriteUInt32(MinMemType42);
            writer.WriteUInt32(MaxMemType42);
            writer.WriteUInt32(MinMemType1);
            writer.WriteUInt32(MaxMemType1);
        }

        protected void ReadHeader(OpenTypeReader reader)
        {
            ItalicAngle = reader.ReadFixed();
            UnderlinePosition = reader.ReadInt16();
            UnderlineThickness = reader.ReadInt16();
            IsFixedPitch = reader.ReadUInt32();
            MinMemType42 = reader.ReadUInt32();
            MaxMemType42 = reader.ReadUInt32();
            MinMemType1 = reader.ReadUInt32();
            MaxMemType1 = reader.ReadUInt32();
        }
    }
}
