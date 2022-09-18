// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts
{
    internal static class Ligatures
    {
        private static readonly Dictionary<string, string> ligatureLookup = new(StringComparer.Ordinal);

        static Ligatures()
        {
            var ligatures = new string[]
            {
                "ﬀ", "ff",
                "ﬃ", "ffi",
                "ﬄ", "ffl",
                "ﬁ", "fi",
                "ﬂ", "fl",
                "ﬅ", "ft",
                "Ĳ", "IJ",
                "ĳ", "ij",
                "ﬆ", "st",
                "Ꜩ", "TZ",
                "ꜩ", "tz",
                "ᵫ", "ue",
                "ꭣ", "uo",
            };

            for (var i = 0; i < ligatures.Length; i += 2)
            {
                ligatureLookup[ligatures[i + 1]] = ligatures[i];
            }
        }

        public static string Lookup(string nonLigature)
        {
            return ligatureLookup.TryGetValue(nonLigature, out var ligature) ? ligature : nonLigature;
        }
    }
}
