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
    internal class CidFontWidthMap : WidthMap
    {
        private readonly Dictionary<uint, double> widthMap;
        private const double WidthMultiplier = 0.001;

        private CidFontWidthMap(Dictionary<uint, double> widthMap)
        {
            this.widthMap = widthMap;
        }

        public static CidFontWidthMap Parse(PdfDictionary font)
        {
            var widthMap = new Dictionary<uint, double>();

            if (font.TryGetArray(Names.DescendantFonts / Indexes.First / Names.W, out var w))
            {
                uint? cfirst = null;
                uint? clast = null;

                foreach (var item in w)
                {
                    if (item is object[] rangeArray)
                    {
                        if (cfirst != null)
                        {
                            var offset = 0u;

                            foreach (var width in rangeArray)
                            {
                                var charCode = cfirst.Value + offset;

                                if (width is int intWidth)
                                {
                                    widthMap[charCode] = intWidth * WidthMultiplier;
                                }
                                else if (width is double realWidth)
                                {
                                    widthMap[charCode] = realWidth * WidthMultiplier;
                                }

                                offset++;
                            }
                        }

                        cfirst = null;
                        clast = null;
                    }
                    else if (item is int integer)
                    {
                        if (cfirst == null)
                        {
                            cfirst = unchecked((uint)integer);
                        }
                        else if (clast == null)
                        {
                            clast = unchecked((uint)integer);
                        }
                        else
                        {
                            for (var i = cfirst.Value; i <= clast.Value; i++)
                            {
                                widthMap[i] = integer * WidthMultiplier;
                            }

                            cfirst = null;
                            clast = null;
                        }
                    }
                    else if (item is double real)
                    {
                        if (cfirst != null && clast != null)
                        {
                            for (var i = cfirst.Value; i <= clast.Value; i++)
                            {
                                widthMap[i] = real * WidthMultiplier;
                            }
                        }

                        cfirst = null;
                        clast = null;
                    }
                }
            }

            return new CidFontWidthMap(widthMap);
        }

        public override double GetWidth(CharInfo ch)
        {
            if (widthMap.TryGetValue(ch.CharCode, out var width))
            {
                return width;
            }

            return 0;
        }
    }
}
