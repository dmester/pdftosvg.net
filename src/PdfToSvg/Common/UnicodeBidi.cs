// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Common
{
    internal static class UnicodeBidi
    {
        // Unicode bidirectional algorithm:
        // http://www.unicode.org/reports/tr9/

        public const char LeftToRightEmbedding = '\u202A';
        public const char RightToLeftEmbedding = '\u202B';
        public const char PopDirectionalFormatting = '\u202C';
        public const char LeftToRightOverride = '\u202D';
        public const char RightToLeftOverride = '\u202E';

        public const char LeftToRightIsolate = '\u2066';
        public const char RightToLeftIsolate = '\u2067';
        public const char FirstStrongIsolate = '\u2068';
        public const char PopDirectionalIsolate = '\u2069';

        public const char LeftToRightMark = '\u200E';
        public const char RightToLeftMark = '\u200F';
        public const char ArabicLetterMark = '\u061C';

        public static bool IsFormattingCharacter(char ch)
        {
            return
                ch >= LeftToRightEmbedding && ch <= RightToLeftOverride ||
                ch >= LeftToRightIsolate && ch <= PopDirectionalIsolate ||
                ch == LeftToRightMark ||
                ch == RightToLeftMark ||
                ch == ArabicLetterMark;
        }

        /// <summary>
        /// Determines whether the specified string might contain Right-to-Left text. The method is optimistic and
        /// might return true even if the string is not RTL, but not the opposite.
        /// </summary>
        public static bool MightBeRtl(string input)
        {
            // Try to find a strong RTL character in the string.
            // To avoid having to embed the entire Unicode database, we will look for broad Unicode blocks known to
            // contain strong RTL characters.
            //
            // The ranges were manually picked from the Unicode database:
            // https://www.unicode.org/Public/UCD/latest/ucd/UnicodeData.txt

            for (var i = 0; i < input.Length; i++)
            {
                var ch = input[i];

                if (// U+0590..U+05FF Hebrew
                    // U+0600..U+06FF Arabic
                    // U+0700..U+074F Syriac
                    // U+0750..U+077F Arabic Supplement
                    // U+0780..U+07BF Thaana
                    // U+07C0..U+07FF NKo
                    // U+0800..U+083F Samaritan
                    // U+0840..U+085F Mandaic
                    // U+0860..U+086F Syriac Supplement
                    // U+0870..U+089F Arabic Extended-B
                    // U+08A0..U+08FF Arabic Extended-A
                    ch >= 0x0590 && (ch <= 0x08FF ||

                    // RIGHT-TO-LEFT MARK
                    ch == 0x200f ||

                    // U+10800..U+1083F Cypriot Syllabary
                    // U+10840..U+1085F Imperial Aramaic
                    // U+10860..U+1087F Palmyrene
                    // U+10880..U+108AF Nabataean
                    // U+108E0..U+108FF Hatran
                    // U+10900..U+1091F Phoenician
                    // U+10920..U+1093F Lydian
                    // U+10940..U+1095F Sidetic
                    // U+10980..U+1099F Meroitic Hieroglyphs
                    // U+109A0..U+109FF Meroitic Cursive
                    // U+10A00..U+10A5F Kharoshthi
                    // U+10A60..U+10A7F Old South Arabian
                    // U+10A80..U+10A9F Old North Arabian
                    // U+10AC0..U+10AFF Manichaean
                    // U+10B00..U+10B3F Avestan
                    // U+10B40..U+10B5F Inscriptional Parthian
                    // U+10B60..U+10B7F Inscriptional Pahlavi
                    // U+10B80..U+10BAF Psalter Pahlavi
                    // U+10C00..U+10C4F Old Turkic
                    // U+10C80..U+10CFF Old Hungarian
                    // U+10D00..U+10D3F Hanifi Rohingya
                    // U+10D40..U+10D8F Garay
                    // U+10E60..U+10E7F Rumi Numeral Symbols
                    // U+10E80..U+10EBF Yezidi
                    // U+10EC0..U+10EFF Arabic Extended-C
                    // U+10F00..U+10F2F Old Sogdian
                    // U+10F30..U+10F6F Sogdian
                    // U+10F70..U+10FAF Old Uyghur
                    // U+10FB0..U+10FDF Chorasmian
                    // U+10FE0..U+10FFF Elymaic
                    // UTF16: U+D802 U+DC00 - U+D803 U+DFFF
                    ch >= 0xD802 && (ch <= 0xD803 ||

                    // U+1E800..U+1E8DF Mende Kikakui
                    // U+1E900..U+1E95F Adlam
                    // U+1EC70..U+1ECBF Indic Siyaq Numbers
                    // U+1ED00..U+1ED4F Ottoman Siyaq Numbers
                    // U+1EE00..U+1EEFF Arabic Mathematical Alphabetic Symbols
                    // UTF16: U+D83A U+DC00 - U+D83B U+DEFF
                    ch >= 0xD83A && (ch <= 0xD83B ||

                    // U+FB00..U+FB4F Alphabetic Presentation Forms (excl U+FB0x)
                    // U+FB50..U+FDFF Arabic Presentation Forms-A
                    ch >= 0xFB10 && (ch <= 0xFDFF ||

                    // U+FE70..U+FEFF Arabic Presentation Forms-B (excl BOM U+FEFF)
                    ch >= 0xFE70 && (ch <= 0xFEFE))))))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
