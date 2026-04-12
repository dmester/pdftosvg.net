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
    internal static class GlyfParser
    {
        private const int SizeInt8 = 1;
        private const int SizeInt16 = 2;


        public static void Read(ref FullGlyfRecord record, OpenTypeReader reader)
        {
            record.NumberOfContours = reader.ReadInt16();
            record.XMin = reader.ReadInt16();
            record.YMin = reader.ReadInt16();
            record.XMax = reader.ReadInt16();
            record.YMax = reader.ReadInt16();

            if (record.XMin > record.XMax ||
                record.YMin > record.YMax)
            {
                throw new OpenTypeException("Invalid BBOX");
            }

            if (record.NumberOfContours > 0)
            {
                ReadSimpleGlyph(ref record, reader);
            }
            else if (record.NumberOfContours < 0)
            {
                record.NumberOfContours = -1;
                ReadCompositeGlyph(ref record, reader);
            }
        }

        public static FullGlyfRecord[] Read(OpenTypeReader reader, uint[] offsets)
        {
            var fullGlyphs = new FullGlyfRecord[offsets.Length - 1];
            var endIndex = (int)offsets[0];

            for (var i = 1; i < offsets.Length; i++)
            {
                var startIndex = endIndex;
                endIndex = (int)offsets[i];

                // PdfToSvg.NET does not need the parsed glyphs, but since fonts included in PDF's can be
                // severely malformed, we need to validate the glyph data to prevent the font from being
                // rejected by OTS Sanitizer when the resulting SVG is shown in a browser.
                if (startIndex >= endIndex)
                {
                    // Empty glyph
                }
                else
                {
                    try
                    {
                        var glyphReader = reader.Slice(startIndex, endIndex - startIndex);
                        Read(ref fullGlyphs[i - 1], glyphReader);
                    }
                    catch
                    {
                        // Invalid glyph, substitute with an empty glyph
                        fullGlyphs[i - 1] = default;
                    }
                }
            }

            return fullGlyphs;
        }

        private static void ReadSimpleGlyph(ref FullGlyfRecord record, OpenTypeReader reader)
        {
            // Spec:
            // https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#simple-glyph-description

            // endPtsOfContours
            var endPtsOfContours = new ushort[record.NumberOfContours];
            for (var i = 0; i < endPtsOfContours.Length; i++)
            {
                var endPt = reader.ReadUInt16();
                if (i > 0 && endPt < endPtsOfContours[i - 1])
                {
                    throw new OpenTypeException("Point indices were not in increasing numeric order");
                }

                endPtsOfContours[i] = endPt;
            }

            var numberOfPoints = endPtsOfContours[endPtsOfContours.Length - 1] + 1;
            record.NumberOfPoints = checked((ushort)numberOfPoints);

            // instructionLength
            var instructionLength = reader.ReadUInt16();
            record.NumInstructions = instructionLength;

            // instructions
            reader.ConsumeBytes(instructionLength);

            // flags
            var flagsStartPosition = reader.Position;
            int totalCoordinateLength = 0;

            for (var i = 0; i < numberOfPoints; i++)
            {
                var flag = (SimpleGlyphFlags)reader.ReadUInt8();

                var xCoordinateLength =
                    flag.HasFlag(SimpleGlyphFlags.XShortVector) ? SizeInt8 :
                    flag.HasFlag(SimpleGlyphFlags.XIsSameOrPositiveXShortVector) ? 0 :
                    SizeInt16;

                var yCoordinateLength =
                    flag.HasFlag(SimpleGlyphFlags.YShortVector) ? SizeInt8 :
                    flag.HasFlag(SimpleGlyphFlags.YIsSameOrPositiveYShortVector) ? 0 :
                    SizeInt16;

                var coordinateLength = xCoordinateLength + yCoordinateLength;

                totalCoordinateLength += coordinateLength;

                var repeat = flag.HasFlag(SimpleGlyphFlags.RepeatFlag);
                if (repeat)
                {
                    int repeatTimes = reader.ReadUInt8();

                    if (repeatTimes == 0)
                    {
                        throw new OpenTypeException("Repeat count must not be 0");
                    }
                    if (i + repeatTimes >= numberOfPoints)
                    {
                        throw new OpenTypeException("Repeat count exceeded remaining glyph buffer");
                    }

                    i += repeatTimes;

                    totalCoordinateLength += coordinateLength * repeatTimes;
                }
            }

            // xCoordinates and yCoordinates
            reader.ConsumeBytes(totalCoordinateLength);

            var endPosition = reader.Position;
            reader.Position = 0;
            record.SimpleContent = reader.ReadByteSegment(endPosition);
        }

        private static void ReadCompositeGlyph(ref FullGlyfRecord record, OpenTypeReader reader)
        {
            // Spec:
            // https://learn.microsoft.com/en-us/typography/opentype/spec/glyf#composite-glyph-description

            var referencedGlyphs = new List<GlyfReference>();
            var flags = default(ComponentGlyphFlags);

            do
            {
                // uint16 flags
                flags = (ComponentGlyphFlags)reader.ReadUInt16();

                // uint16 glyphIndex
                var glyphIndex = reader.ReadUInt16();

                // -- Arguments
                var argumentsAreWords = flags.HasFlag(ComponentGlyphFlags.Arg1And2AreWords);
                // int16/int8 argument1
                // int16/int8 argument2
                var argumentByteCount = 2 * (argumentsAreWords ? SizeInt16 : SizeInt8);

                // -- Scale
                var scaleByteCount = 0;
                if (flags.HasFlag(ComponentGlyphFlags.WeHaveAScale))
                {
                    // F2DOT14 scale
                    scaleByteCount = 1 * SizeInt16;
                }
                else if (flags.HasFlag(ComponentGlyphFlags.WeHaveAnXAndYScale))
                {
                    // F2DOT14 xScale
                    // F2DOT14 yScale
                    scaleByteCount = 2 * SizeInt16;
                }
                else if (flags.HasFlag(ComponentGlyphFlags.WeHaveATwoByTwo))
                {
                    // F2DOT14 xScale
                    // F2DOT14 scale01
                    // F2DOT14 scale10
                    // F2DOT14 yScale
                    scaleByteCount = 4 * SizeInt16;
                }

                // ---
                referencedGlyphs.Add(new GlyfReference
                {
                    GlyphIndex = glyphIndex,
                    Flags = flags,
                    Data = reader.ReadByteSegment(argumentByteCount + scaleByteCount),
                });
            }
            while (flags.HasFlag(ComponentGlyphFlags.MoreComponents));

            // Validate instructions, otherwise remove them
            //
            // Some producer seems to omit the `numInstructions` field, but still include the instructions data,
            // which causes OTS Sanitizer to reject the font.
            // https://github.com/dmester/pdftosvg.net/issues/61
            //
            if (flags.HasFlag(ComponentGlyphFlags.WeHaveInstructions) && reader.Position + SizeInt16 <= reader.Length)
            {
                var numInstructions = reader.ReadUInt16();

                if (reader.Position + numInstructions <= reader.Length)
                {
                    record.CompositeInstructions = reader.ReadByteSegment(numInstructions);
                    record.NumInstructions = numInstructions;
                }
            }

            record.ReferencedGlyphs = referencedGlyphs.ToArray();
        }

    }
}
