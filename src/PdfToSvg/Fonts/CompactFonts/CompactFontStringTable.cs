﻿// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.CompactFonts
{
    internal class CompactFontStringTable
    {
        // CFF spec Appendix A
        private static readonly string[] sids = new string[]
        {
            ".notdef",
            "space",
            "exclam",
            "quotedbl",
            "numbersign",
            "dollar",
            "percent",
            "ampersand",
            "quoteright",
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
            "quoteleft",
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
            "exclamdown",
            "cent",
            "sterling",
            "fraction",
            "yen",
            "florin",
            "section",
            "currency",
            "quotesingle",
            "quotedblleft",
            "guillemotleft",
            "guilsinglleft",
            "guilsinglright",
            "fi",
            "fl",
            "endash",
            "dagger",
            "daggerdbl",
            "periodcentered",
            "paragraph",
            "bullet",
            "quotesinglbase",
            "quotedblbase",
            "quotedblright",
            "guillemotright",
            "ellipsis",
            "perthousand",
            "questiondown",
            "grave",
            "acute",
            "circumflex",
            "tilde",
            "macron",
            "breve",
            "dotaccent",
            "dieresis",
            "ring",
            "cedilla",
            "hungarumlaut",
            "ogonek",
            "caron",
            "emdash",
            "AE",
            "ordfeminine",
            "Lslash",
            "Oslash",
            "OE",
            "ordmasculine",
            "ae",
            "dotlessi",
            "lslash",
            "oslash",
            "oe",
            "germandbls",
            "onesuperior",
            "logicalnot",
            "mu",
            "trademark",
            "Eth",
            "onehalf",
            "plusminus",
            "Thorn",
            "onequarter",
            "divide",
            "brokenbar",
            "degree",
            "thorn",
            "threequarters",
            "twosuperior",
            "registered",
            "minus",
            "eth",
            "multiply",
            "threesuperior",
            "copyright",
            "Aacute",
            "Acircumflex",
            "Adieresis",
            "Agrave",
            "Aring",
            "Atilde",
            "Ccedilla",
            "Eacute",
            "Ecircumflex",
            "Edieresis",
            "Egrave",
            "Iacute",
            "Icircumflex",
            "Idieresis",
            "Igrave",
            "Ntilde",
            "Oacute",
            "Ocircumflex",
            "Odieresis",
            "Ograve",
            "Otilde",
            "Scaron",
            "Uacute",
            "Ucircumflex",
            "Udieresis",
            "Ugrave",
            "Yacute",
            "Ydieresis",
            "Zcaron",
            "aacute",
            "acircumflex",
            "adieresis",
            "agrave",
            "aring",
            "atilde",
            "ccedilla",
            "eacute",
            "ecircumflex",
            "edieresis",
            "egrave",
            "iacute",
            "icircumflex",
            "idieresis",
            "igrave",
            "ntilde",
            "oacute",
            "ocircumflex",
            "odieresis",
            "ograve",
            "otilde",
            "scaron",
            "uacute",
            "ucircumflex",
            "udieresis",
            "ugrave",
            "yacute",
            "ydieresis",
            "zcaron",
            "exclamsmall",
            "Hungarumlautsmall",
            "dollaroldstyle",
            "dollarsuperior",
            "ampersandsmall",
            "Acutesmall",
            "parenleftsuperior",
            "parenrightsuperior",
            "twodotenleader",
            "onedotenleader",
            "zerooldstyle",
            "oneoldstyle",
            "twooldstyle",
            "threeoldstyle",
            "fouroldstyle",
            "fiveoldstyle",
            "sixoldstyle",
            "sevenoldstyle",
            "eightoldstyle",
            "nineoldstyle",
            "commasuperior",
            "threequartersemdash",
            "periodsuperior",
            "questionsmall",
            "asuperior",
            "bsuperior",
            "centsuperior",
            "dsuperior",
            "esuperior",
            "isuperior",
            "lsuperior",
            "msuperior",
            "nsuperior",
            "osuperior",
            "rsuperior",
            "ssuperior",
            "tsuperior",
            "ff",
            "ffi",
            "ffl",
            "parenleftinferior",
            "parenrightinferior",
            "Circumflexsmall",
            "hyphensuperior",
            "Gravesmall",
            "Asmall",
            "Bsmall",
            "Csmall",
            "Dsmall",
            "Esmall",
            "Fsmall",
            "Gsmall",
            "Hsmall",
            "Ismall",
            "Jsmall",
            "Ksmall",
            "Lsmall",
            "Msmall",
            "Nsmall",
            "Osmall",
            "Psmall",
            "Qsmall",
            "Rsmall",
            "Ssmall",
            "Tsmall",
            "Usmall",
            "Vsmall",
            "Wsmall",
            "Xsmall",
            "Ysmall",
            "Zsmall",
            "colonmonetary",
            "onefitted",
            "rupiah",
            "Tildesmall",
            "exclamdownsmall",
            "centoldstyle",
            "Lslashsmall",
            "Scaronsmall",
            "Zcaronsmall",
            "Dieresissmall",
            "Brevesmall",
            "Caronsmall",
            "Dotaccentsmall",
            "Macronsmall",
            "figuredash",
            "hypheninferior",
            "Ogoneksmall",
            "Ringsmall",
            "Cedillasmall",
            "questiondownsmall",
            "oneeighth",
            "threeeighths",
            "fiveeighths",
            "seveneighths",
            "onethird",
            "twothirds",
            "zerosuperior",
            "foursuperior",
            "fivesuperior",
            "sixsuperior",
            "sevensuperior",
            "eightsuperior",
            "ninesuperior",
            "zeroinferior",
            "oneinferior",
            "twoinferior",
            "threeinferior",
            "fourinferior",
            "fiveinferior",
            "sixinferior",
            "seveninferior",
            "eightinferior",
            "nineinferior",
            "centinferior",
            "dollarinferior",
            "periodinferior",
            "commainferior",
            "Agravesmall",
            "Aacutesmall",
            "Acircumflexsmall",
            "Atildesmall",
            "Adieresissmall",
            "Aringsmall",
            "AEsmall",
            "Ccedillasmall",
            "Egravesmall",
            "Eacutesmall",
            "Ecircumflexsmall",
            "Edieresissmall",
            "Igravesmall",
            "Iacutesmall",
            "Icircumflexsmall",
            "Idieresissmall",
            "Ethsmall",
            "Ntildesmall",
            "Ogravesmall",
            "Oacutesmall",
            "Ocircumflexsmall",
            "Otildesmall",
            "Odieresissmall",
            "OEsmall",
            "Oslashsmall",
            "Ugravesmall",
            "Uacutesmall",
            "Ucircumflexsmall",
            "Udieresissmall",
            "Yacutesmall",
            "Thornsmall",
            "Ydieresissmall",
            "001.000",
            "001.001",
            "001.002",
            "001.003",
            "Black",
            "Bold",
            "Book",
            "Light",
            "Medium",
            "Regular",
            "Roman",
            "Semibold",
        };
        private static readonly Dictionary<string, int> sidLookup = new(StringComparer.Ordinal);

        private List<string> customStrings;
        private Dictionary<string, int> customStringLookup = new(StringComparer.Ordinal);

        static CompactFontStringTable()
        {
            for (var i = 0; i < sids.Length; i++)
            {
                sidLookup[sids[i]] = i;
            }
        }

        public CompactFontStringTable()
        {
            customStrings = new List<string>();
        }

        public CompactFontStringTable(string[] customStrings)
        {
            this.customStrings = customStrings.ToList();

            for (var i = 0; i < customStrings.Length; i++)
            {
                customStringLookup[customStrings[i]] = i + sids.Length;
            }
        }

        public IList<string> GetCustomStrings()
        {
            return customStrings;
        }

        public string? Lookup(int index)
        {
            if (index >= 0)
            {
                if (index < sids.Length)
                {
                    return sids[index];
                }

                index -= sids.Length;

                if (index < customStrings.Count)
                {
                    return customStrings[index];
                }
            }

            return null;
        }

        public int Lookup(string str)
        {
            int sid;

            if (sidLookup.TryGetValue(str, out sid) ||
                customStringLookup.TryGetValue(str, out sid))
            {
                return sid;
            }

            return -1;
        }

        public int AddOrLookup(string str)
        {
            var sid = Lookup(str);

            if (sid < 0)
            {
                sid = customStrings.Count + sids.Length;
                customStrings.Add(str);
                customStringLookup[str] = sid;
            }

            return sid;
        }
    }
}
