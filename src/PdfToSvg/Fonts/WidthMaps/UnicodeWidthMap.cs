// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.CMaps;
using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Fonts.WidthMaps
{
    internal class UnicodeWidthMap : WidthMap
    {
        private readonly Dictionary<char, double> widthMap;
        private readonly double multiplier;

        public UnicodeWidthMap(ushort[] widthData, double multiplier)
        {
            widthMap = new Dictionary<char, double>();

            for (var i = 1; i < widthData.Length; i += 2)
            {
                widthMap[(char)widthData[i - 1]] = widthData[i];
            }

            this.multiplier = multiplier;
        }

        public override double GetWidth(CharInfo ch)
        {
            var width = 0.0;

            if (ch.Unicode != null)
            {
                for (var i = 0; i < ch.Unicode.Length; i++)
                {
                    if (widthMap.TryGetValue(ch.Unicode[i], out var charWidth))
                    {
                        width += charWidth;
                    }
                }
            }

            return width * multiplier;
        }
    }
}
