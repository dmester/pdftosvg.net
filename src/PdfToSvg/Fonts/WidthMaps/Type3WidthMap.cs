// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.CMaps;
using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing;
using PdfToSvg.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Fonts.WidthMaps
{
    internal class Type3WidthMap : WidthMap
    {
        private readonly Dictionary<int, double> widthMap = new Dictionary<int, double>();

        public Type3WidthMap(PdfDictionary font)
        {
            // PDF Specification 1.7, Table 112, page 266
            var fontMatrixArray = font.GetArrayOrNull<double>(Names.FontMatrix);
            var firstChar = font.GetValueOrDefault(Names.FirstChar, 0);

            if (fontMatrixArray != null &&
                fontMatrixArray.Length == 6 &&
                font.TryGetArray<double>(Names.Widths, out var widths))
            {
                var fontMatrix = new Matrix(
                    fontMatrixArray[0],
                    fontMatrixArray[1],
                    fontMatrixArray[2],
                    fontMatrixArray[3],
                    fontMatrixArray[4],
                    fontMatrixArray[5]);

                fontMatrix.DecomposeScaleX(out var scaleX);

                for (var i = 0; i < widths.Length; i++)
                {
                    widthMap[firstChar + i] = widths[i] * scaleX;
                }
            }
        }

        public override double GetWidth(CharInfo ch) => GetWidth((int)ch.CharCode);

        public double GetWidth(int charCode)
        {
            if (widthMap.TryGetValue(charCode, out var width))
            {
                return width;
            }

            return 0;
        }
    }
}
