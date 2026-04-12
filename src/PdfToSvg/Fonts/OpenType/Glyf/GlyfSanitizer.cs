// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Fonts.OpenType.Tables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts.OpenType.Glyf
{
    internal static class GlyfSanitizer
    {
        public static void Sanitize(FullGlyfRecord[] glyphs, GlyfStats stats)
        {
            RemoveInvalidCompositeGlyphs(glyphs);
            RemoveEmptyGlyphReferences(glyphs);

            var maxPoints = 0;
            var maxContours = 0;
            var maxCompositeDepth = 0;
            var maxCompositePoints = 0;
            var maxCompositeContours = 0;
            var maxComponentElements = 0;
            var maxSizeOfInstructions = 0;

            for (var i = 0; i < glyphs.Length; i++)
            {
                ref var glyph = ref glyphs[i];
                var referencedGlyphs = glyph.ReferencedGlyphs;

                if (referencedGlyphs != null && referencedGlyphs.Length > 0)
                {
                    var totalGlyphPoints = 0;
                    var totalGlyphContours = 0;

                    var stack = new Stack<IEnumerator>();
                    stack.Push(referencedGlyphs.GetEnumerator());

                    if (maxCompositeDepth < stack.Count)
                    {
                        maxCompositeDepth = stack.Count;
                    }

                    do
                    {
                        var enumerator = stack.Peek();
                        if (enumerator.MoveNext())
                        {
                            var referencedGlyph = glyphs[((GlyfReference)enumerator.Current).GlyphIndex];
                            totalGlyphPoints += referencedGlyph.NumberOfPoints;

                            if (referencedGlyph.IsSimpleGlyph)
                            {
                                totalGlyphContours += referencedGlyph.NumberOfContours;
                            }
                            else if (
                                referencedGlyph.ReferencedGlyphs != null &&
                                referencedGlyph.ReferencedGlyphs.Length > 0)
                            {
                                stack.Push(referencedGlyph.ReferencedGlyphs.GetEnumerator());

                                if (maxCompositeDepth < stack.Count)
                                {
                                    maxCompositeDepth = stack.Count;
                                }
                            }
                        }
                        else
                        {
                            stack.Pop();
                        }
                    }
                    while (stack.Count > 0);

                    if (maxComponentElements < referencedGlyphs.Length)
                    {
                        maxComponentElements = referencedGlyphs.Length;
                    }
                    if (maxCompositePoints < totalGlyphPoints)
                    {
                        maxCompositePoints = totalGlyphPoints;
                    }
                    if (maxCompositeContours < totalGlyphContours)
                    {
                        maxCompositeContours = totalGlyphContours;
                    }
                }

                if (maxSizeOfInstructions < glyph.NumInstructions)
                {
                    maxSizeOfInstructions = glyph.NumInstructions;
                }
                if (maxContours < glyph.NumberOfContours)
                {
                    maxContours = glyph.NumberOfContours;
                }
                if (maxPoints < glyph.NumberOfPoints)
                {
                    maxPoints = glyph.NumberOfPoints;
                }
            }

            stats.MaxPoints = (ushort)maxPoints;
            stats.MaxContours = (ushort)maxContours;
            stats.MaxComponentDepth = (ushort)maxCompositeDepth;
            stats.MaxCompositePoints = (ushort)maxCompositePoints;
            stats.MaxCompositeContours = (ushort)maxCompositeContours;
            stats.MaxComponentElements = (ushort)maxComponentElements;
            stats.MaxSizeOfInstructions = (ushort)maxSizeOfInstructions;
        }

        private static void RemoveInvalidCompositeGlyphs(FullGlyfRecord[] glyphs)
        {
            for (var i = 0; i < glyphs.Length; i++)
            {
                ref var glyph = ref glyphs[i];

                if (glyph.ReferencedGlyphs == null ||
                    glyph.ReferencedGlyphs.Length == 0)
                {
                    continue;
                }

                var stack = new Stack<IEnumerator>();
                var glyphPath = new Stack<int>();
                glyphPath.Push(i);
                stack.Push(glyph.ReferencedGlyphs.GetEnumerator());

                do
                {
                    var enumerator = stack.Peek();
                    if (enumerator.MoveNext())
                    {
                        var glyphReference = (GlyfReference)enumerator.Current;
                        if (glyphPath.Contains(glyphReference.GlyphIndex))
                        {
                            glyphs[glyphReference.GlyphIndex] = default;

                            // Rewind up to the first time the cyclic glyph was referenced
                            while (glyphPath.Count > 0 && glyphPath.Pop() != glyphReference.GlyphIndex) ;

                            // Synchronize stack with the path
                            while (stack.Count > glyphPath.Count)
                            {
                                stack.Pop();
                            }
                        }
                        else if (glyphReference.GlyphIndex >= glyphs.Length)
                        {
                            // Invalid reference
                            // Will be removed by RemoveEmptyGlyphReferences()
                        }
                        else
                        {
                            // Check child glyphs
                            ref var referencedGlyph = ref glyphs[glyphReference.GlyphIndex];
                            if (referencedGlyph.ReferencedGlyphs != null && referencedGlyph.ReferencedGlyphs.Length > 0)
                            {
                                stack.Push(referencedGlyph.ReferencedGlyphs.GetEnumerator());
                                glyphPath.Push(glyphReference.GlyphIndex);
                            }
                        }
                    }
                    else
                    {
                        stack.Pop();
                        glyphPath.Pop();
                    }
                }
                while (stack.Count > 0);
            }
        }

        private static void RemoveEmptyGlyphReferences(FullGlyfRecord[] glyphs)
        {
            for (var glyphIndex = 0; glyphIndex < glyphs.Length; glyphIndex++)
            {
                ref var glyph = ref glyphs[glyphIndex];
                if (glyph.ReferencedGlyphs == null)
                {
                    continue;
                }

                for (var refIndex = glyph.ReferencedGlyphs.Length - 1; refIndex >= 0; refIndex--)
                {
                    var glyphRef = glyph.ReferencedGlyphs[refIndex];

                    var invalidRef =
                        // Non-existing glyph referenced
                        glyphRef.GlyphIndex >= glyphs.Length ||

                        // DirectWrite on Windows does not like references to empty simple glyphs.
                        // Newer versions of OTS sanitizer, e.g. the one used by Firefox, will fix this by removing the
                        // reference to the empty glyph, but older versions will not, so we will do it here to be sure.
                        //
                        // More info here:
                        // https://github.com/khaledhosny/ots/pull/289
                        glyphs[glyphRef.GlyphIndex].NumberOfContours == 0;

                    if (invalidRef)
                    {
                        glyph.ReferencedGlyphs = ArrayUtils.Remove(glyph.ReferencedGlyphs, refIndex);

                        // References left?
                        if (glyph.ReferencedGlyphs.Length == 0)
                        {
                            glyphs[glyphIndex] = default;
                        }
                        else
                        {
                            // If we remove glyph references, the instructions might be invalid.
                            // We could try to patch them, but since the glyph is already damaged, we will make minimal
                            // effort repairing it.
                            glyph.CompositeInstructions = default;
                        }
                    }
                }
            }
        }
    }
}
