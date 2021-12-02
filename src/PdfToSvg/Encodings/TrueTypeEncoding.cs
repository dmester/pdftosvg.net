// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Fonts.OpenType;
using PdfToSvg.Fonts.OpenType.Enums;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfToSvg.Encodings
{
    /// <summary>
    /// Extracts unicode information from a TrueType/OpenType font.
    /// </summary>
    internal class TrueTypeEncoding : ITextDecoder
    {
        private static readonly Dictionary<Tuple<OpenTypePlatformID, int>, int> cmapPriority = new()
        {
            { Tuple.Create(OpenTypePlatformID.Windows, 10), 3 },
            { Tuple.Create(OpenTypePlatformID.Unicode, 4), 1 },
            { Tuple.Create(OpenTypePlatformID.Windows, 1), 4 },
            { Tuple.Create(OpenTypePlatformID.Unicode, 3), 2 },
            { Tuple.Create(OpenTypePlatformID.Windows, 0), 5 },
        };

        private readonly OpenTypeCMap cmap;
        private readonly Dictionary<uint, uint> cidToGidMap = new Dictionary<uint, uint>();

        private TrueTypeEncoding(OpenTypeCMap cmap, Dictionary<uint, uint> cidToGidMap)
        {
            this.cmap = cmap;
            this.cidToGidMap = cidToGidMap;
        }

        public static TrueTypeEncoding? Create(OpenTypeFont font, Stream cidToGidMapStream)
        {
            var cmap = font.CMaps
                .Select(cmap => cmapPriority.TryGetValue(
                    Tuple.Create(cmap.PlatformID, cmap.EncodingID), out var priority) ?
                    new { Priority = priority, CMap = cmap } : null)
                .Where(cmap => cmap != null)
                .OrderBy(cmap => cmap!.Priority)
                .Select(cmap => cmap!.CMap)
                .FirstOrDefault();

            if (cmap == null)
            {
                return null;
            }

            var cidToGidMap = new Dictionary<uint, uint>();

            var buffer = new byte[1024];
            int read;
            var nextCid = 0u;

            do
            {
                read = cidToGidMapStream.ReadAll(buffer, 0, buffer.Length);

                for (var i = 0; i + 1 < read; i += 2)
                {
                    var gid = unchecked((uint)((buffer[i] << 8) | buffer[i + 1]));
                    cidToGidMap[nextCid++] = gid;
                }
            }
            while (read == buffer.Length);

            return new TrueTypeEncoding(cmap, cidToGidMap);
        }

        public CharacterCode GetCharacter(PdfString value, int index)
        {
            if (index + 1 < value.Length)
            {
                var cid = unchecked((uint)((value[index] << 8) | value[index + 1]));

                if (!cidToGidMap.TryGetValue(cid, out var gid))
                {
                    gid = cid;
                }

                var str = cmap.ToUnicode(gid);
                if (str != null)
                {
                    return new CharacterCode(cid, 2, str);
                }
            }

            return default;
        }
    }
}
