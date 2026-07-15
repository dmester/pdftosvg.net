// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.IO;
using System;
using System.Collections.Generic;

namespace PdfToSvg.Imaging.Jpx.Packets
{
    /// <summary>
    /// Accumulates packed packet header data from PPM marker segments
    /// (ITU-T T.800 (06/2019) Section A.7.4, collected during the phase-1 main header parse) and PPT marker segments
    /// (Section A.7.5, collected during the phase-2 tile-part walk), and exposes the packed header bytes per tile to
    /// the Tier-2 packet reader.
    /// <para>
    ///    Write side (called by <see cref="Codestream.JpxCodestreamReader"/>): <see cref="AddMainHeaderSegment"/> for
    ///    each PPM segment, then <see cref="RegisterTilePart"/> once per tile-part in codestream order, and
    ///    <see cref="AddTileHeaderSegment"/> for each PPT segment of that tile-part.
    /// </para>
    /// <para>
    ///    Read side: <see cref="GetTileHeaderReader"/> returns a cached, stateful reader over the concatenated packet
    ///    headers of one tile, or null when the packet headers are interleaved in the bit stream. The reader instance
    ///    is cached so its cursor advances sequentially as consecutive packets consume header bits.
    /// </para>
    /// </summary>
    internal sealed class JpxPackedPacketHeaderBuffer
    {
        private readonly List<JpxPackedPacketHeaderSegment> mainSegments = new();
        private readonly Dictionary<int, TileBuffer> tiles = new();

        // Concatenation of all PPM segment data in Zppm order, split into one (Nppm, Ippm) entry per tile-part in
        // codestream order (ITU-T T.800 (06/2019) Section A.7.4).
        private List<ArraySegment<byte>>? mainEntries;
        private int nextMainEntry;

        /// <summary>
        /// Whether the main header contained PPM marker segments. When true, all packet headers live in the main header
        /// (ITU-T T.800 (06/2019) Section A.7.4) and the tile bit streams contain packet bodies only.
        /// </summary>
        public bool HasMainHeaderSegments => mainSegments.Count > 0;

        public void AddMainHeaderSegment(JpxPackedPacketHeaderSegment segment)
        {
            mainSegments.Add(segment);
        }

        /// <summary>
        /// Registers the next tile-part in codestream order. When PPM segments are present, this assigns the next
        /// (Nppm, Ippm) entry to <paramref name="tileIndex"/>, since the kth entry belongs to the kth tile-part
        /// appearing in the codestream (ITU-T T.800 (06/2019) Section A.7.4)
        /// </summary>
        public void RegisterTilePart(int tileIndex)
        {
            var tile = GetTile(tileIndex);
            tile.TilePartCount++;

            if (mainSegments.Count == 0)
            {
                return;
            }

            mainEntries ??= ParseMainEntries();

            if (nextMainEntry < mainEntries.Count)
            {
                tile.MainEntries.Add(mainEntries[nextMainEntry++]);
            }
            else
            {
                Log.WriteLine(
                    "JPEG 2000 PPM data contains fewer (Nppm, Ippm) entries than there are " +
                    "tile-parts; decoding best-effort");
            }
        }

        /// <summary>
        /// Adds one PPT marker segment encountered in the current tile-part header of the given tile. Must be called
        /// after <see cref="RegisterTilePart"/> for that tile-part, since Zppt indices are scoped to a single tile-part
        /// header (ITU-T T.800 (06/2019) Section A.7.5)
        /// </summary>
        public void AddTileHeaderSegment(int tileIndex, JpxPackedPacketHeaderSegment segment)
        {
            var tile = GetTile(tileIndex);
            tile.TileSegments.Add(new PptSegment
            {
                TilePartOrdinal = tile.TilePartCount,
                Segment = segment,
            });
        }

        /// <summary>
        /// Returns a reader over the concatenated packed packet headers of one tile, or null when this tile's packet
        /// headers are found in the bit stream. PPT data takes precedence over PPM data
        /// (ITU-T T.800 (06/2019) Section A.7.5 disallows the combination; decoding is best-effort if both appear).
        /// The returned instance is cached, so repeated calls observe the same cursor.
        /// </summary>
        public JpxDataReader? GetTileHeaderReader(int tileIndex)
        {
            var tile = GetTile(tileIndex);

            if (tile.Reader == null)
            {
                if (tile.TileSegments.Count > 0)
                {
                    if (mainSegments.Count > 0)
                    {
                        Log.WriteLine(
                            "JPEG 2000 codestream contains both PPM and PPT marker segments, which is disallowed by " +
                            "ITU-T T.800 Section A.7.4; using the PPT data");
                    }

                    tile.Reader = new JpxDataReader(ConcatTileSegments(tile));
                }
                else if (mainSegments.Count > 0)
                {
                    mainEntries ??= ParseMainEntries();

                    var totalLength = 0;
                    foreach (var range in tile.MainEntries)
                    {
                        totalLength += range.Count;
                    }

                    var data = new byte[totalLength];
                    var cursor = 0;
                    foreach (var range in tile.MainEntries)
                    {
                        range.CopyTo(data, cursor);
                        cursor += range.Count;
                    }

                    tile.Reader = new JpxDataReader(data);
                }
            }

            return tile.Reader;
        }

        private TileBuffer GetTile(int tileIndex)
        {
            if (!tiles.TryGetValue(tileIndex, out var tile))
            {
                tile = new TileBuffer();
                tiles[tileIndex] = tile;
            }
            return tile;
        }

        private static int CompareSegmentOrder(JpxPackedPacketHeaderSegment a, JpxPackedPacketHeaderSegment b)
        {
            return a.Index - b.Index;
        }

        private static int CompareSegmentOrder(PptSegment a, PptSegment b)
        {
            if (a.TilePartOrdinal != b.TilePartOrdinal)
            {
                return a.TilePartOrdinal - b.TilePartOrdinal;
            }

            return CompareSegmentOrder(a.Segment, b.Segment);
        }

        // ITU-T T.800 (06/2019) Section A.7.5:
        // Within one tile-part header, PPT data is concatenated in order of increasing Zppt. Across tile-part headers,
        // the data follows the tile-part order in the codestream.
        private static byte[] ConcatTileSegments(TileBuffer tile)
        {
            var segments = tile.TileSegments;

            segments.Sort(CompareSegmentOrder);

            for (var i = 1; i < segments.Count; i++)
            {
                if (segments[i].TilePartOrdinal == segments[i - 1].TilePartOrdinal &&
                    segments[i].Segment.Index == segments[i - 1].Segment.Index)
                {
                    throw new JpxException(
                        "Duplicate JPEG 2000 PPT marker segment index Zppt=" + segments[i].Segment.Index);
                }
            }

            var totalLength = 0;
            foreach (var segment in segments)
            {
                totalLength += segment.Segment.Data.Count;
            }

            var data = new byte[totalLength];
            var cursor = 0;
            foreach (var segment in segments)
            {
                segment.Segment.Data.CopyTo(data, cursor);
                cursor += segment.Segment.Data.Count;
            }

            return data;
        }

        // ITU-T T.800 (06/2019) Section A.7.4:
        // PPM segment data is concatenated in order of increasing Zppm and then split into (Nppm, Ippm) blocks, one per
        // tile-part. Nppm blocks may span PPM segment boundaries.
        private List<ArraySegment<byte>> ParseMainEntries()
        {
            // Sort segments
            mainSegments.Sort(CompareSegmentOrder);

            for (var i = 1; i < mainSegments.Count; i++)
            {
                if (mainSegments[i].Index == mainSegments[i - 1].Index)
                {
                    throw new JpxException(
                        "Duplicate JPEG 2000 PPM marker segment index Zppm=" + mainSegments[i].Index);
                }
            }

            // Combine segments
            var totalLength = 0;
            foreach (var segment in mainSegments)
            {
                totalLength += segment.Data.Count;
            }

            var data = new byte[totalLength];
            var cursor = 0;
            foreach (var segment in mainSegments)
            {
                segment.Data.CopyTo(data, cursor);
                cursor += segment.Data.Count;
            }

            // Parse segments
            var dataReader = new JpxDataReader(data);
            var entries = new List<ArraySegment<byte>>();

            while (!dataReader.EndOfStream)
            {
                if (dataReader.Cursor + 4 > dataReader.Length)
                {
                    Log.WriteLine(
                        "Truncated Nppm length field at the end of the JPEG 2000 PPM data; " +
                        "ignoring the trailing " + (dataReader.Length - dataReader.Cursor) + " byte(s)");
                    break;
                }

                var nppm = dataReader.ReadUInt32();
                var remainingBytes = (uint)(data.Length - dataReader.Cursor);

                var bytesToRead = nppm;

                if (remainingBytes < nppm)
                {
                    bytesToRead = remainingBytes;
                    Log.WriteLine(
                        "Truncated JPEG 2000 PPM data: Nppm=" + nppm + " but only " + remainingBytes +
                        " bytes remain; decoding best-effort");
                }

                entries.Add(dataReader.ReadBytes((int)bytesToRead));
            }

            return entries;
        }

        private struct PptSegment
        {
            public int TilePartOrdinal;
            public JpxPackedPacketHeaderSegment Segment;
        }

        private sealed class TileBuffer
        {
            public readonly List<PptSegment> TileSegments = new();
            public readonly List<ArraySegment<byte>> MainEntries = new();
            public int TilePartCount;
            public JpxDataReader? Reader;
        }
    }
}
