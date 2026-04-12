// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Fonts.OpenType;
using PdfToSvg.Fonts.OpenType.Glyf;
using PdfToSvg.Fonts.OpenType.Tables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Tests.Fonts.OpenType.Glyf
{
    public class GlyfSanitizerTests
    {
        [Test]
        public void Sanitize_Cyclic_Self()
        {
            var glyphs = new FullGlyfRecord[]
            {
                // Simple
                SimpleGlyph(0),

                // Composite
                CompositeGlyph(1, [0, 1]),
            };

            GlyfSanitizer.Sanitize(glyphs, new GlyfStats());

            AssertOriginal(glyphs, 0);
            AssertEmpty(glyphs, 1);
        }

        [Test]
        public void Sanitize_Cyclic_First()
        {
            var glyphs = new FullGlyfRecord[]
            {
                // Simple
                SimpleGlyph(0),

                // Composite
                CompositeGlyph(1, [2]),
                CompositeGlyph(2, [1]),
                CompositeGlyph(3, [0]),
            };

            GlyfSanitizer.Sanitize(glyphs, new GlyfStats());

            AssertOriginal(glyphs, 0);
            AssertEmpty(glyphs, 1); // Cycle
            AssertEmpty(glyphs, 2); // Empty
            AssertOriginal(glyphs, 3);
        }

        [Test]
        public void Sanitize_Cyclic_Last()
        {
            var glyphs = new FullGlyfRecord[]
            {
                // Simple
                SimpleGlyph(0),

                // Composite
                CompositeGlyph(1, [2]),
                CompositeGlyph(2, [3]),
                CompositeGlyph(3, [2]),
            };

            GlyfSanitizer.Sanitize(glyphs, new GlyfStats());

            AssertOriginal(glyphs, 0);
            AssertEmpty(glyphs, 1); // Empty
            AssertEmpty(glyphs, 2); // Cycle
            AssertEmpty(glyphs, 3); // Empty
        }

        [Test]
        public void Sanitize_Cyclic_Complex()
        {
            // Two cycles:
            // 1 -> 2 -> 3 -> 1
            // 3 -> 4 -> 5 -> 3

            var glyphs = new FullGlyfRecord[]
            {
                // Simple
                SimpleGlyph(0),

                // Composite
                CompositeGlyph(1, [2]),
                CompositeGlyph(2, [3]),
                CompositeGlyph(3, [1, 4]),
                CompositeGlyph(4, [5]),
                CompositeGlyph(5, [3]),
            };

            GlyfSanitizer.Sanitize(glyphs, new GlyfStats());

            AssertOriginal(glyphs, 0);
            AssertEmpty(glyphs, 1); // Cycle
            AssertEmpty(glyphs, 2); // Empty
            AssertEmpty(glyphs, 3); // Cycle
            AssertOriginal(glyphs, 4);
            AssertEmpty(glyphs, 5); // Empty
        }

        [Test]
        public void Sanitize_InvalidReference()
        {
            var glyphs = new FullGlyfRecord[]
            {
                SimpleGlyph(0),
                CompositeGlyph(1, [3]), // Invalid
                CompositeGlyph(2, [0]),
            };

            GlyfSanitizer.Sanitize(glyphs, new GlyfStats());

            AssertOriginal(glyphs, 0);
            AssertEmpty(glyphs, 1);
            AssertOriginal(glyphs, 2);
        }

        [Test]
        public void Stats()
        {
            var fontPath = Path.Combine(TestFiles.ExternalFontsDirectory, "PdfToSvgTTF-Composite.ttf");
            var font = OpenTypeFont.Parse(File.ReadAllBytes(fontPath));

            var glyf = font.Tables.Get<GlyfTable>().Stats;
            var maxp = (MaxpTableV10)font.Tables.Get<MaxpTable>();

            Assert.AreEqual(maxp.MaxPoints, glyf.MaxPoints);
            Assert.AreEqual(maxp.MaxContours, glyf.MaxContours);
            Assert.AreEqual(maxp.MaxComponentDepth, glyf.MaxComponentDepth);
            Assert.AreEqual(maxp.MaxCompositePoints, glyf.MaxCompositePoints);
            Assert.AreEqual(maxp.MaxCompositeContours, glyf.MaxCompositeContours);
            Assert.AreEqual(maxp.MaxComponentElements, glyf.MaxComponentElements);
            Assert.AreEqual(maxp.MaxSizeOfInstructions, glyf.MaxSizeOfInstructions);
        }

        private static FullGlyfRecord SimpleGlyph(byte glyphIndex)
        {
            return new FullGlyfRecord
            {
                SimpleContent = new ArraySegment<byte>([glyphIndex], 0, 1),
                NumberOfContours = 1,
            };
        }

        private static FullGlyfRecord CompositeGlyph(byte glyphIndex, ushort[] referencedGlyphIndexes)
        {
            return new FullGlyfRecord
            {
                SimpleContent = new ArraySegment<byte>([glyphIndex], 0, 1),
                NumberOfContours = -1,
                ReferencedGlyphs = referencedGlyphIndexes
                    .Select(i => new GlyfReference
                    {
                        GlyphIndex = i,
                    })
                    .ToArray(),
            };
        }

        private static void AssertEmpty(FullGlyfRecord[] glyphs, int glyphIndex)
        {
            if (glyphs[glyphIndex].SimpleContent.Count != 0)
            {
                Assert.Fail("Glyph {0} was not empty", glyphIndex);
            }
        }

        private static void AssertOriginal(FullGlyfRecord[] glyphs, int glyphIndex)
        {
            if (glyphs[glyphIndex].SimpleContent.Count != 1 ||
                glyphs[glyphIndex].SimpleContent.Array[0] != glyphIndex)
            {
                Assert.Fail("Glyph {0} was replaced", glyphIndex);
            }
        }
    }
}
