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
        private readonly double defaultWidth;
        private readonly Dictionary<uint, double> widthMap;
        private const double WidthMultiplier = 0.001;
        // ISO 32000-1, section 9.7.3: CIDs are limited to two bytes.
        private const int MaxCid = ushort.MaxValue;
        private const int MaxWidthAssignments = 1000000;

        private CidFontWidthMap(Dictionary<uint, double> widthMap, double defaultWidth)
        {
            this.widthMap = widthMap;
            this.defaultWidth = defaultWidth;
        }

        public static CidFontWidthMap Parse(PdfDictionary font)
        {
            var widthMap = new Dictionary<uint, double>();
            // Include overwrites in the budget: repeated ranges must not multiply the work without limit.
            var remainingWidths = MaxWidthAssignments;
            var defaultWidth = 1.0; // Default value for default width is 1000

            if (font.TryGetNumber(Names.DescendantFonts / Indexes.First / Names.DW, out var dw))
            {
                defaultWidth = dw * WidthMultiplier;
            }

            if (font.TryGetArray(Names.DescendantFonts / Indexes.First / Names.W, out var w))
            {
                int? cfirst = null;
                int? clast = null;

                foreach (var item in w)
                {
                    if (remainingWidths <= 0)
                    {
                        // Preserve parsed widths and use the default width for missing CIDs.
                        break;
                    }

                    if (item is object[] rangeArray)
                    {
                        if (cfirst != null)
                        {
                            for (var offset = 0; offset < rangeArray.Length && remainingWidths > 0;
                                offset++, remainingWidths--)
                            {
                                var charCode = (long)cfirst.Value + offset;
                                if (charCode > MaxCid)
                                {
                                    break;
                                }
                                if (charCode < 0)
                                {
                                    continue;
                                }

                                var width = rangeArray[offset];

                                if (width is int intWidth)
                                {
                                    widthMap[(uint)charCode] = intWidth * WidthMultiplier;
                                }
                                else if (width is double realWidth)
                                {
                                    widthMap[(uint)charCode] = realWidth * WidthMultiplier;
                                }
                            }
                        }

                        cfirst = null;
                        clast = null;
                    }
                    else if (item is int integer)
                    {
                        if (cfirst == null)
                        {
                            cfirst = integer;
                        }
                        else if (clast == null)
                        {
                            clast = integer;
                        }
                        else
                        {
                            var last = Math.Min(MaxCid, clast.Value);
                            for (var i = Math.Max(0, cfirst.Value); i <= last && remainingWidths > 0;
                                i++, remainingWidths--)
                            {
                                widthMap[(uint)i] = integer * WidthMultiplier;
                            }

                            cfirst = null;
                            clast = null;
                        }
                    }
                    else if (item is double real)
                    {
                        if (cfirst != null && clast != null)
                        {
                            var last = Math.Min(MaxCid, clast.Value);
                            for (var i = Math.Max(0, cfirst.Value); i <= last && remainingWidths > 0;
                                i++, remainingWidths--)
                            {
                                widthMap[(uint)i] = real * WidthMultiplier;
                            }
                        }

                        cfirst = null;
                        clast = null;
                    }
                }
            }

            return new CidFontWidthMap(widthMap, defaultWidth);
        }

        public override double GetWidth(CharInfo ch)
        {
            if (ch.Cid.HasValue && widthMap.TryGetValue(ch.Cid.Value, out var width))
            {
                return width;
            }

            return defaultWidth;
        }
    }
}
