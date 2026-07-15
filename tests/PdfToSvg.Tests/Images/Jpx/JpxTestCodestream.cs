// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace PdfToSvg.Tests.Images.Jpx
{
    /// <summary>
    /// Helpers for building synthetic JPEG 2000 codestreams byte by byte, following the marker
    /// segment layouts of ITU-T T.800 (06/2019) Annex A.
    /// </summary>
    internal static class JpxTestCodestream
    {
        public static byte[] U16(int value) => [
            (byte)(value >> 8),
            (byte)value,
        ];

        public static byte[] U32(long value) => [
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value,
        ];

        public static byte[] Concat(params byte[][] parts)
        {
            var length = 0;
            foreach (var part in parts)
            {
                length += part.Length;
            }

            var result = new byte[length];
            var cursor = 0;
            foreach (var part in parts)
            {
                part.CopyTo(result, cursor);
                cursor += part.Length;
            }

            return result;
        }

        /// <summary>Marker + auto-computed segment length + payload (ITU-T T.800 Section A.1).</summary>
        public static byte[] Segment(int marker, params byte[][] payloadParts)
        {
            var payload = Concat(payloadParts);
            return Concat(U16(marker), U16(payload.Length + 2), payload);
        }

        public static byte[] Soc() => U16(0xFF4F);

        public static byte[] Eoc() => U16(0xFFD9);

        /// <summary>
        /// SIZ marker segment (ITU-T T.800 Section A.5.1). Each component is a
        /// (Ssiz, XRsiz, YRsiz) triple.
        /// </summary>
        public static byte[] Siz(
            int xsiz, int ysiz, int xosiz, int yosiz,
            int xtsiz, int ytsiz, int xtosiz, int ytosiz,
            params byte[][] components)
        {
            var parts = new List<byte[]>
            {
                U16(0), // Rsiz
                U32(xsiz), U32(ysiz), U32(xosiz), U32(yosiz),
                U32(xtsiz), U32(ytsiz), U32(xtosiz), U32(ytosiz),
                U16(components.Length),
            };
            parts.AddRange(components);
            return Segment(0xFF51, parts.ToArray());
        }

        /// <summary>COD marker segment without user-defined precincts (ITU-T T.800 Section A.6.1).</summary>
        public static byte[] Cod(
            int layers = 1,
            int progressionOrder = 0,
            bool mct = false,
            int decompositionLevels = 1,
            int codeBlockExponent = 4)
        {
            return Segment(0xFF52,
                [0],                             // Scod: no precincts, no SOP/EPH
                [(byte)progressionOrder],
                U16(layers),
                [(byte)(mct ? 1 : 0)],
                [(byte)decompositionLevels],
                [(byte)(codeBlockExponent - 2)], // xcb - 2
                [(byte)(codeBlockExponent - 2)], // ycb - 2
                [0],                             // code-block style
                [1]);                            // transformation: 5/3 reversible
        }

        /// <summary>QCD marker segment, no quantization style (ITU-T T.800 Section A.6.4).</summary>
        public static byte[] Qcd(int guardBits = 2, int subBandCount = 4)
        {
            var payload = new byte[1 + subBandCount];
            payload[0] = (byte)(guardBits << 5);
            for (var i = 0; i < subBandCount; i++)
            {
                payload[1 + i] = 8 << 3; // exponent 8 (Table A.29)
            }
            return Segment(0xFF5C, payload);
        }

        /// <summary>
        /// A full tile-part: SOT (Psot computed unless overridden), header segments, SOD and data
        /// (ITU-T T.800 Sections A.4.2 and A.4.3).
        /// </summary>
        public static byte[] TilePart(
            int tileIndex,
            int tilePartIndex,
            int tilePartCount,
            byte[] headerSegments,
            byte[] data,
            long? psotOverride = null)
        {
            var psot = psotOverride ?? (12 + headerSegments.Length + 2 + data.Length);

            return Concat(
                U16(0xFF90),
                U16(10),
                U16(tileIndex),
                U32(psot),
                [(byte)tilePartIndex, (byte)tilePartCount],
                headerSegments,
                U16(0xFF93),
                data);
        }

        /// <summary>A main header for a single-component, single-tile image.</summary>
        public static byte[] MainHeader(params byte[][] extraSegments)
        {
            var parts = new List<byte[]>
            {
                Soc(),
                Siz(32, 16, 0, 0, 32, 16, 0, 0, [ 7, 1, 1 ]),
                Cod(),
                Qcd(),
            };
            parts.AddRange(extraSegments);
            return Concat(parts.ToArray());
        }

        public static byte[] ToArray(ArraySegment<byte> segment)
        {
            var result = new byte[segment.Count];
            Array.Copy(segment.Array!, segment.Offset, result, 0, segment.Count);
            return result;
        }
    }
}
