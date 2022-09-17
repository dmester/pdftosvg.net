// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.CMaps;
using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Fonts.WidthMaps;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace PdfToSvg.Fonts
{
    internal abstract class Type0Font : BaseFont
    {
        protected override void OnInit(CancellationToken cancellationToken)
        {
            base.OnInit(cancellationToken);

            widthMap = CidFontWidthMap.Parse(fontDict);

            if (fontDict.TryGetValue(Names.Encoding, out var encoding) &&
                (encoding is PdfDictionary || encoding is PdfName))
            {
                cmap = CMap.Create(encoding, cancellationToken) ?? CMap.TwoByteIdentity;
            }
            else
            {
                cmap = CMap.TwoByteIdentity;
            }

            if (cmap.IsUnicodeCMap && toUnicode == UnicodeMap.Empty)
            {
                toUnicode = UnicodeMap.Identity;
            }
        }

        [DebuggerDisplay("{Cid} -> {GlyphIndex} ('{Unicode}')")]
        protected class CidChar
        {
            public uint Cid;
            public uint GlyphIndex;
            public string? Unicode;
        }

        protected override IEnumerable<CharInfo> GetChars()
        {
            return GetCidChars()
                .DistinctBy(ch => ch.Cid)
                .SelectMany(ch => cmap
                    .GetCharCodes(ch.Cid)
                    .Select(charCode => new CharInfo
                    {
                        CharCode = charCode,
                        Cid = ch.Cid,
                        GlyphIndex = ch.GlyphIndex,
                        Unicode = ch.Unicode ?? CharInfo.NotDef,
                    }));
        }

        protected abstract IEnumerable<CidChar> GetCidChars();
    }
}
