// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg
{
    /// <summary>
    /// The default font resolver which will try to match font names against commonly available fonts.
    /// </summary>
    public class DefaultFontResolver : IFontResolver
    {
        private static readonly string[] fontWeights = new string[]
        {
            "semibold", "600",
            "bold", "bold",
        };

        private static readonly string[] fontStyles = new string[]
        {
            "oblique", "oblique",
            "italic", "italic",
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
            "Trebuchet MS", "'Trebuchet MS',Trebuchet,sans-serif",
            "Verdana", "Verdana,sans-serif",
            "Webdings", "Webdings",
            "Wingdings", "Wingdings",
            "YuGothic", "'Yu Gothic',sans-serif",

            // Wider matchers
            "sans", "sans-serif",
            "roman", "serif",
            "serif", "serif",
            "source", "monospace",
            "code", "monospace",
            "consol", "monospace",
            "mono", "monospace",
        };

        /// <summary>
        /// Gets an instance of the <see cref="DefaultFontResolver"/>.
        /// </summary>
        public static DefaultFontResolver Instance { get; } = new DefaultFontResolver();

        /// <inheritdoc/>
        public Font ResolveFont(string fontName, CancellationToken cancellationToken)
        {
            var styleStartIndex = fontName.IndexOfAny(new[] { ',', '-' });

            var fontFamily = Match(0, fontFamilies, fontName.Replace("-", "").Replace(" ", ""));
            var fontWeight = Match(styleStartIndex, fontWeights, fontName);
            var fontStyle = Match(styleStartIndex, fontStyles, fontName);

            return new LocalFont(fontFamily ?? "Sans-Serif", fontWeight, fontStyle);
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
