// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using PdfToSvg.Fonts.OpenType;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg.Fonts
{
    internal sealed class CidType2Font : Type0Font
    {
        private struct CidMapping
        {
            public uint Cid;
            public uint Gid;
        }

        private ILookup<uint, uint>? gidToCidMap;

        protected override void OnInit(CancellationToken cancellationToken)
        {
            base.OnInit(cancellationToken);

            gidToCidMap = ReadCidToGidMap(cancellationToken).ToLookup(x => x.Gid, x => x.Cid);
        }

        private IEnumerable<CidMapping> ReadCidToGidMap(CancellationToken cancellationToken)
        {
            if (fontDict.TryGetStream(Names.DescendantFonts / Indexes.First / Names.CIDToGIDMap, out var cidToGidMapStream))
            {
                var buffer = new byte[1024];
                int read;
                var nextCid = 0u;

                using var stream = cidToGidMapStream.OpenDecoded(cancellationToken);
                do
                {
                    read = stream.ReadAll(buffer, 0, buffer.Length);

                    for (var i = 0; i + 1 < read; i += 2)
                    {
                        var gid = unchecked((uint)((buffer[i] << 8) | buffer[i + 1]));

                        yield return new CidMapping
                        {
                            Cid = nextCid++,
                            Gid = gid,
                        };
                    }
                }
                while (read == buffer.Length);
            }
        }

        protected override IEnumerable<CidChar> GetCidChars()
        {
            if (openTypeFont == null || gidToCidMap == null)
            {
                yield break;
            }

            // ISO 32000-2 section 9.7.4.2
            var numGlyphs = openTypeFont.NumGlyphs;
            var cmap = openTypeFont.CMaps.OrderByPriority().FirstOrDefault();

            if (cmap != null)
            {
                foreach (var ch in cmap.Chars)
                {
                    foreach (var cid in gidToCidMap[ch.GlyphIndex])
                    {
                        yield return new CidChar
                        {
                            Cid = cid,
                            GlyphIndex = ch.GlyphIndex,
                            Unicode = Utf16Encoding.EncodeCodePoint(ch.Unicode),
                        };
                    }
                }

                foreach (var ch in cmap.Chars)
                {
                    if (!gidToCidMap.Contains(ch.GlyphIndex))
                    {
                        yield return new CidChar
                        {
                            Cid = ch.GlyphIndex,
                            GlyphIndex = ch.GlyphIndex,
                            Unicode = Utf16Encoding.EncodeCodePoint(ch.Unicode),
                        };
                    }
                }
            }

            for (var glyphIndex = 0u; glyphIndex < numGlyphs; glyphIndex++)
            {
                foreach (var cid in gidToCidMap[glyphIndex])
                {
                    yield return new CidChar { Cid = cid, GlyphIndex = glyphIndex };
                }
            }

            for (var glyphIndex = 0u; glyphIndex < numGlyphs; glyphIndex++)
            {
                if (!gidToCidMap.Contains(glyphIndex))
                {
                    yield return new CidChar { Cid = glyphIndex, GlyphIndex = glyphIndex };
                }
            }
        }
    }
}
