// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg.Fonts.FontResolvers
{
    /// <summary>
    /// A font resolver trying to match font names against commonly available fonts.
    /// </summary>
    internal class LocalFontResolver : FontResolver
    {
        // Information about abbreviated font styles and weights:
        // https://cdn2.hubspot.net/hubfs/1740477/Definitive-Guide-To-Font-Abbreviations.pdf

        private static readonly string[] fontWeights = new string[]
        {
            "demibold", "600",
            "semibold", "600",
            "extrabold", "800",
            "ultrabold", "800",
            "xtbold", "800",
            "utbold", "800",
            "ultralight", "200",
            "extralight", "200",
            "utlight", "200",
            "xtlight", "200",
            "normal", "400",
            "regular", "400",
            "roman", "400",
            "medium", "500",
            "thin", "100",
            "hairline", "100",
            "black", "900",
            "light", "300",
            "bold", "700",
        };

        private static readonly string[] shortFontWeights = new string[]
        {
            "th", "100",
            "lt", "300",
            "md", "500",
            "dm", "600",
            "sm", "600",
            "bd", "700",
            "bl", "900",
        };

        private static readonly string[] fontStyles = new string[]
        {
            "oblique", "Oblique",
            "italic", "Italic",
        };

        private static readonly string[] shortFontStyles = new string[]
        {
            "ob", "Oblique",
            "it", "Italic",
        };

        private static readonly string[] fontFamilies = new string[]
        {
            // Pdf standard fonts
            "Courier", "'Courier New',Courier,monospace",
            "Helvetica", "Helvetica,Arial,sans-serif",
            "Times", "'Times New Roman',serif",
            "Symbol", "Symbol",

            // Default Windows fonts
            // See https://docs.microsoft.com/en-us/typography/fonts/windows_10_font_list

            // Arial Black must come before Arial, since "Arial" also matches "Arial Black".
            "ArialBlack", "'Arial Black',Arial,sans-serif",

            "Arial", "Arial,sans-serif",
            "Bahnschrift", "Bahnschrift,sans-serif",
            "Calibri", "Calibri,sans-serif",
            "Cambria", "Cambria,serif",
            "Candara", "Candara,sans-serif",
            "ComicSans", "'Comic Sans MS','Comic Sans',cursive",
            "Consolas", "Consolas,monospace",
            "Constantia", "Constantia,serif",
            "Corbel", "Corbel,sans-serif",
            "Ebrima", "Ebrima,sans-serif",
            "Franklin", "'Franklin Gothic',sans-serif",
            "Gabriola", "Gabriola,cursive",
            "Gadugi", "Gadugi,sans-serif",
            "Georgia", "Georgia,serif",
            "Impact", "Impact,fantasy",
            "InkFree", "'Ink Free',cursive",
            "LucidaConsole", "'Lucida Console',monospace",
            "LucidaSans", "'Lucida Sans Unicode','Lucida Sans',sans-serif",
            "MalgunGothic", "'Malgun Gothic',sans-serif",
            "Marlett", "Marlett",
            "ftHimalaya", "'Microsoft Himalaya',sans-serif",
            "ftJhengHei", "'Microsoft JhengHei',sans-serif",
            "ftNewTaiLue", "'Microsoft New Tai Lue',sans-serif",
            "ftPhagsPa", "'Microsoft PhagsPa',sans-serif",
            "ftSansSerif", "'Microsoft Sans Serif',sans-serif",
            "ftTaiLe", "'Microsoft Tai Le',sans-serif",
            "ftYaHei", "'Microsoft YaHei',sans-serif",
            "ftYiBaiti", "'Microsoft Yi Baiti',sans-serif",
            "MingLiU", "'MingLiU-ExtB',serif",
            "MongolianBaiti", "'Mongolian Baiti',sans-serif",
            "MSGothic", "'MS Gothic',sans-serif",
            "MSPGothic", "'MS Gothic',sans-serif",
            "MVBoli", "'MV Boli',cursive",
            "Myanmar", "'Myanmar Text','Myanmar Unicode',sans-serif",
            "Nirmala", "'Nirmala UI',sans-serif",
            "Palatino", "'Palatino Linotype',Palatino,serif",
            "SegoePrint", "'Segoe Print',cursive",
            "SegoeScript", "'Segoe Script',cursive",
            "Segoe", "'Segoe UI',sans-serif",
            "SimSun", "SimSun,serif",
            "Sitka", "Sitka,serif",
            "Sylfaen", "Sylfaen,serif",
            "Tahoma", "Tahoma,sans-serif",
            "Trebuchet", "'Trebuchet MS',sans-serif",
            "Verdana", "Verdana,sans-serif",
            "Webdings", "Webdings",
            "Wingdings", "Wingdings",
            "YuGothic", "'Yu Gothic',sans-serif",

            // Wider matchers
            "sans", "sans-serif",
            "roman", "serif",
            "serif", "serif",
            "book", "serif",
            "source", "monospace",
            "code", "monospace",
            "consol", "monospace",
            "mono", "monospace",

            // CJK
            "mincho", "serif",
            "ming", "serif",
            "song", "serif",
        };

        /// <inheritdoc/>
        public override Font ResolveFont(SourceFont sourceFont, CancellationToken cancellationToken)
        {
            var fontName = sourceFont.Name;
            if (fontName == null)
            {
                return new LocalFont("sans-serif");
            }

            var fontFamily = Match(0, fontFamilies, fontName.Replace("-", "").Replace(" ", ""));

            var styleStartIndex = fontName.IndexOfAny(new[] { ',', '-' });

            var rawFontWeight = Match(styleStartIndex, fontWeights, fontName);
            var rawFontStyle = Match(styleStartIndex, fontStyles, fontName);

            // Only match abbreviatons if the remaining part of the font name is short
            if (styleStartIndex > 0 &&
                styleStartIndex + 5 >= fontName.Length &&
                rawFontWeight == null)
            {
                rawFontWeight = Match(styleStartIndex, shortFontWeights, fontName);
            }

            if (styleStartIndex > 0 &&
                styleStartIndex + 20 >= fontName.Length &&
                rawFontStyle == null)
            {
                rawFontStyle = Match(styleStartIndex, shortFontStyles, fontName);
            }

            Enum.TryParse<FontWeight>(rawFontWeight, out var fontWeight);
            Enum.TryParse<FontStyle>(rawFontStyle, out var fontStyle);

            return new LocalFont(fontFamily ?? "sans-serif", fontWeight, fontStyle);
        }

        private static string? Match(int startIndex, string[] propertyMatchers, string fontName)
        {
            if (startIndex < 0)
            {
                startIndex = 0;
            }

            for (var i = 0; i < propertyMatchers.Length; i += 2)
            {
                var matcher = propertyMatchers[i];

                var index = fontName.IndexOf(matcher, startIndex, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    return propertyMatchers[i + 1];
                }
            }

            return null;
        }
    }
}
