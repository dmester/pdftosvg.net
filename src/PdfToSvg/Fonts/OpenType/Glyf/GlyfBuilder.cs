// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Fonts.OpenType.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType.Glyf
{
    internal static class GlyfBuilder
    {
        public static GlyfRecord[] Write(FullGlyfRecord[] fullGlyphs, int estimatedCapacity)
        {
            var newOffsets = new int[fullGlyphs.Length];
            var writer = new OpenTypeWriter(estimatedCapacity);

            for (var glyphIndex = 0; glyphIndex < fullGlyphs.Length; glyphIndex++)
            {
                Write(writer, ref fullGlyphs[glyphIndex]);
                newOffsets[glyphIndex] = writer.Position;
            }

            var binaryData = writer.ToArray();

            // Finish GlyfTable
            var glyphs = new GlyfRecord[fullGlyphs.Length];
            var cursor = 0;

            for (var glyphIndex = 0; glyphIndex < glyphs.Length; glyphIndex++)
            {
                ref var fullGlyph = ref fullGlyphs[glyphIndex];

                var startIndex = cursor;
                cursor = newOffsets[glyphIndex];

                glyphs[glyphIndex] = new GlyfRecord
                {
                    Data = new ArraySegment<byte>(binaryData, startIndex, cursor - startIndex),
                    XMin = fullGlyph.XMin,
                    XMax = fullGlyph.XMax,
                    YMin = fullGlyph.YMin,
                    YMax = fullGlyph.YMax,
                };
            }

            return glyphs;
        }

        private static void Write(OpenTypeWriter writer, ref FullGlyfRecord record)
        {
            var startPosition = writer.Position;

            if (record.IsSimpleGlyph)
            {
                writer.WriteBytes(record.SimpleContent);
            }
            else
            {
                writer.WriteInt16(record.NumberOfContours);
                writer.WriteInt16(record.XMin);
                writer.WriteInt16(record.YMin);
                writer.WriteInt16(record.XMax);
                writer.WriteInt16(record.YMax);

                var refGlyphs = record.ReferencedGlyphs;
                if (refGlyphs != null)
                {
                    for (var refGlyphIndex = 0; refGlyphIndex < refGlyphs.Length; refGlyphIndex++)
                    {
                        var refGlyph = refGlyphs[refGlyphIndex];

                        var flags = (ComponentGlyphFlags.All & ~ComponentGlyphFlags.MoreComponents & ~ComponentGlyphFlags.WeHaveInstructions) & refGlyph.Flags;

                        if (refGlyphIndex + 1 < refGlyphs.Length)
                        {
                            flags |= ComponentGlyphFlags.MoreComponents;
                        }
                        else if (record.CompositeInstructions.Count > 0)
                        {
                            flags |= ComponentGlyphFlags.WeHaveInstructions;
                        }

                        writer.WriteUInt16((ushort)flags);
                        writer.WriteUInt16(refGlyph.GlyphIndex);
                        writer.WriteBytes(refGlyph.Data);
                    }
                }

                if (record.CompositeInstructions.Count > 0)
                {
                    writer.WriteUInt16((ushort)record.CompositeInstructions.Count);
                    writer.WriteBytes(record.CompositeInstructions);
                }
            }

            // Byte alignment
            //
            // Word alignment is required if `indexToLocFormat` in `head` table is 0.
            //
            // It is always required according to the Apple spec:
            // https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6loca.html#:~:text=The%20glyph%20data%20is%20always%20word%20aligned
            //
            // It is recommended according to the Microsoft spec:
            // https://learn.microsoft.com/en-us/typography/opentype/otspec150/recom#loca-table
            //
            if (((writer.Position - startPosition) & 1) == 1)
            {
                writer.WriteUInt8(0);
            }
        }
    }
}
