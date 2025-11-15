// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Common;
using PdfToSvg.Encodings;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Common
{
    public class UnicodeBidiTests
    {
        private enum BidiType
        {
            StrongLeft,
            StrongRight,
            Weak,
            Neutral,
            Formatting,
        }

        private static BidiType GetType(string bidiClass)
        {
            return bidiClass switch
            {
                // See
                // https://www.unicode.org/reports/tr44/#Bidi_Class_Values

                "L" => BidiType.StrongLeft,
                "R" => BidiType.StrongRight,
                "AL" => BidiType.StrongRight,

                "EN" => BidiType.Weak, 
                "ES" => BidiType.Weak,
                "ET" => BidiType.Weak,
                "AN" => BidiType.Weak,
                "CS" => BidiType.Weak,
                "NSM" => BidiType.Weak,
                "BN" => BidiType.Weak,

                "B" => BidiType.Neutral,
                "S" => BidiType.Neutral,
                "WS" => BidiType.Neutral,
                "ON" => BidiType.Neutral,

                _ => BidiType.Formatting,
            };
        }

        [Test]
        public void MightBeRtl_NoFalsePositivesForAscii()
        {
            var asciiChars = new string(Enumerable.Range(1, 127).Select(x => (char)x).ToArray());
            Assert.IsFalse(UnicodeBidi.MightBeRtl(asciiChars));
        }

        [Test]
        public void MightBeRtl_NoFalseNegatives()
        {
            var unicodeDataPath = Path.Combine("Common", "UnicodeData.txt");

            var codePoints = File
                .ReadAllLines(unicodeDataPath)
                .Select(line => line.Split(';'))
                .Where(fields => fields.Length > 4)
                .Select(fields => new
                {
                    Value = uint.Parse(fields[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    Name = fields[1],
                    BidiClass = fields[4],
                    BidiType = GetType(fields[4]),
                });

            var falsePositiveStrongLeft = 0;
            var falsePositiveWeakOrNeutral = 0;

            foreach (var codePoint in codePoints)
            {
                var s = Utf16Encoding.EncodeCodePoint(codePoint.Value);
                var mightBeRtl = UnicodeBidi.MightBeRtl(s);

                if (mightBeRtl)
                {
                    if (codePoint.BidiType == BidiType.StrongLeft)
                    {
                        falsePositiveStrongLeft++;
                    }
                    if (codePoint.BidiType == BidiType.Weak ||
                        codePoint.BidiType == BidiType.Neutral)
                    {
                        falsePositiveWeakOrNeutral++;
                    }
                }
                else
                {
                    if (codePoint.BidiType == BidiType.StrongRight)
                    {
                        Assert.Fail(
                            "Code point {0:x4} is a strong RTL character, but did not trigger a truthy return value",
                            codePoint.Value);
                    }
                }
            }

            Assert.IsTrue(falsePositiveStrongLeft < 10);
            Assert.IsTrue(falsePositiveWeakOrNeutral < 1000);
        }
    }
}
