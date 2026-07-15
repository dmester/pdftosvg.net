// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.Codestream;
using PdfToSvg.Imaging.Jpx.Coding;
using PdfToSvg.Imaging.Jpx.ImageModel;
using PdfToSvg.Imaging.Jpx.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace PdfToSvg.Imaging.Jpx.Packets
{
    internal sealed class JpxPacketReader
    {
        // Lblock plus the coding pass count factor of Equation B-19 must produce a length that
        // fits a non-negative Int32 (JpxConstraints.MaxFileSize bounds real data far below that)
        private const int MaxSegmentLengthBits = 31;

        // ITU-T T.800 (06/2019) Equation E-2 bounds Mb to 37 (exponent <= 31, guard bits <= 7),
        // and with an ROI the code-blocks are coded with M'b = Mb + s bit-planes (Equation H-3)
        // where s <= JpxConstraints.MaxRegionOfInterestShift (37), so a background code-block can
        // legitimately signal up to M'b - 1 = 73 missing bit-planes. A larger number cannot come
        // from a conforming encoder.
        private const int MaxZeroBitPlanes = 74;

        private readonly JpxTile tile;
        private readonly JpxComponent[] components;
        private readonly JpxResolvedTileParameters parameters;
        private readonly JpxDataReader tilePartReader;
        private readonly JpxDataReader? packedHeaderReader;
        private readonly bool[]? retainedCodeBlockData;

        private bool packetsRead;

        // Codeword segments of the packet currently being decoded, in packet body order.
        private readonly List<BodySlice> bodySlices = new();

        private bool warnedNsopMismatch;
        private bool warnedMissingEph;

        public JpxPacketReader(
            JpxTile tile,
            JpxComponent[] components,
            JpxResolvedTileParameters parameters,
            JpxDataReader tilePartReader,
            JpxDataReader? packedHeaderReader,
            bool[]? retainedCodeBlockData = null)
        {
            if (components.Length != tile.Components.Length)
            {
                throw new JpxException("The JPEG 2000 component list does not match the tile geometry");
            }
            if (retainedCodeBlockData != null && retainedCodeBlockData.Length != components.Length)
            {
                throw new ArgumentException(
                    "Expected one code-block data retention flag per component.", nameof(retainedCodeBlockData));
            }

            this.tile = tile;
            this.components = components;
            this.parameters = parameters;
            this.tilePartReader = tilePartReader;
            this.packedHeaderReader = packedHeaderReader;
            this.retainedCodeBlockData = retainedCodeBlockData;
        }

        public void ReadTilePackets()
        {
            if (packetsRead)
            {
                throw new InvalidOperationException("The packets of this tile have already been read.");
            }
            packetsRead = true;

            var packetIndex = 0;

            foreach (var key in JpxPacketProgressionEnumerator.Enumerate(tile, components, parameters))
            {
                try
                {
                    if (!ReadPacket(packetIndex, key))
                    {
                        break;
                    }
                }
                catch (EndOfStreamException)
                {
                    // ITU-T T.800 (06/2019) section B.11 allows truncated tiles
                    break;
                }

                packetIndex++;
            }
        }

        private bool ReadPacket(int packetIndex, JpxPacketKey key)
        {
            var resolutionLevel = tile.Components[key.Component].ResolutionLevels[key.Resolution];
            var precinct = resolutionLevel.Precincts[key.Precinct];

            // ITU-T T.800 (06/2019) Section A.8.1:
            // An SOP marker segment may precede any packet.
            // With PPM/PPT it appears immediately before the packet body. SOP markers are tolerated whether or not the
            // COD announced them.
            ReadSopMarker(packetIndex);

            // Read header
            if (!ReadPacketHeader(resolutionLevel, precinct, key))
            {
                return false;
            }

            // ITU-T T.800 (06/2019) Section A.8.2:
            // The EPH marker follows the packet header in the same stream as the header
            // (PPM/PPT data or the bit stream)
            ReadEphMarker();

            // Codeword segments of skipped components and of resolution levels above the decoded resolution are
            // never entropy decoded, so their bytes are not retained. The packet header decoding above must still
            // run for them, since it determines the lengths of all subsequent packets.
            var retained =
                (retainedCodeBlockData == null || retainedCodeBlockData[key.Component]) &&
                key.Resolution < tile.Components[key.Component].DecodedResolutionLevelCount;

            // ITU-T T.800 (06/2019) Section B.9:
            // Packet body: the codeword segments appear in the same order as their header entries
            foreach (var slice in bodySlices)
            {
                var length = slice.Length;
                var remainingBytes = tilePartReader.Length - tilePartReader.Cursor;

                // ITU-T T.800 (06/2019) section B.11 allows truncated tiles
                var truncated = length > remainingBytes;
                if (truncated)
                {
                    length = remainingBytes;
                }

                var segmentData = tilePartReader.ReadBytes(length);

                if (retained)
                {
                    AppendSegment(slice, segmentData);
                }

                if (truncated)
                {
                    return false;
                }
            }

            return true;
        }

        private static void AppendSegment(BodySlice slice, ArraySegment<byte> segmentData)
        {
            ref var segments = ref slice.CodeBlock.Segments;
            var last = segments.Count > 0 ? segments[segments.Count - 1] : null;

            if (last != null && !last.Terminated)
            {
                // ITU-T T.800 (06/2019) Section B.10.7.2: a length group that does not end at a
                // terminated coding pass continues in the code-block's next contribution, so the
                // codeword segment spans packets and is decoded as one segment.
                last.Data.Append(segmentData);
                last.CodingPasses += slice.CodingPasses;
                last.Terminated = slice.Terminated;
            }
            else
            {
                segments.Add(new JpxCodeBlockSegment
                {
                    CodingPasses = slice.CodingPasses,
                    Terminated = slice.Terminated,
                    Data = new(segmentData),
                });
            }
        }

        /// <summary>
        /// Decodes one packet header (ITU-T T.800 (06/2019) Sections B.10.3 to B.10.8), updating
        /// the per-code-block inclusion state and collecting the codeword segment lengths of the
        /// packet body into <see cref="bodySlices"/>.
        /// </summary>
        private bool ReadPacketHeader(JpxResolutionLevel resolutionLevel, JpxPrecinct precinct, JpxPacketKey key)
        {
            var byteReader = packedHeaderReader ?? tilePartReader;

            // ITU-T T.800 (06/2019) section B.11 allows truncated tiles
            if (byteReader.EndOfStream)
            {
                return false;
            }

            var reader = new JpxPacketBitReader(byteReader);

            bodySlices.Clear();

            // ITU-T T.800 (06/2019) Section B.10.3: the first bit denotes a zero length packet.
            if (reader.ReadBit() == 0)
            {
                return true;
            }

            var subBands = resolutionLevel.SubBands;

            for (var bandIndex = 0; bandIndex < subBands.Length; bandIndex++)
            {
                var subBand = subBands[bandIndex];
                ref var band = ref precinct.SubBands[bandIndex];

                var countX = band.CodeBlockCountX;
                var countY = band.CodeBlockCountY;
                if (countX == 0 || countY == 0)
                {
                    // No code-blocks of this sub-band fall inside the precinct; the sub-band has
                    // no representation in the packet (ITU-T T.800 (06/2019) Section B.9).
                    continue;
                }

                var inclusionTree = band.InclusionTree ??= new JpxTagTree(countX, countY);
                var zeroBitPlanesTree = band.ZeroBitPlanesTree ??= new JpxTagTree(countX, countY);

                // Code-blocks in raster order confined to the precinct
                // (ITU-T T.800 (06/2019) Section B.10.8).
                for (var y = 0; y < countY; y++)
                {
                    for (var x = 0; x < countX; x++)
                    {
                        var codeBlockIndex =
                            (band.CodeBlockX0 - subBand.CodeBlockGridX0 + x) +
                            (band.CodeBlockY0 - subBand.CodeBlockGridY0 + y) * subBand.CodeBlockCountX;
                        var codeBlock = subBand.CodeBlocks[codeBlockIndex];
                        var leaf = x + y * countX;

                        bool included;
                        var firstInclusion = false;

                        if (codeBlock.Included)
                        {
                            // ITU-T T.800 (06/2019) Section B.10.4: one bit for previously
                            // included code-blocks.
                            included = reader.ReadBit() == 1;
                        }
                        else
                        {
                            // ITU-T T.800 (06/2019) Section B.10.4: the inclusion tag tree values
                            // are the layer number in which the code-block is first included.
                            included = inclusionTree.Decode(ref reader, leaf, key.Layer + 1);
                            firstInclusion = included;
                        }

                        if (!included)
                        {
                            continue;
                        }

                        if (firstInclusion)
                        {
                            // ITU-T T.800 (06/2019) Section B.10.5: the number of missing most
                            // significant bit-planes, coded with a second tag tree per precinct
                            // sub-band.
                            codeBlock.ZeroBitPlanes = DecodeTagTreeValue(ref reader, zeroBitPlanesTree, leaf);
                            codeBlock.Included = true;
                        }

                        var codingPasses = ReadCodingPassCount(ref reader);

                        // ITU-T T.800 (06/2019) Section B.10.7.1: k one bits followed by a zero
                        // increase Lblock by k.
                        while (reader.ReadBit() == 1)
                        {
                            codeBlock.Lblock++;

                            if (codeBlock.Lblock > MaxSegmentLengthBits)
                            {
                                throw new JpxException(
                                    "Invalid JPEG 2000 code-block length indicator (Lblock=" +
                                    codeBlock.Lblock + ").");
                            }
                        }

                        ReadSegmentLengths(ref reader, codeBlock, codingPasses);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Reads the codeword segment lengths of one code-block contribution
        /// (ITU-T T.800 (06/2019) sections B.10.7.1 and B.10.7.2) and queues the corresponding
        /// body slices. One length is signalled per terminated coding pass in the packet
        /// (Tables D.8 and D.9), plus one for the final pass if it is not terminated.
        /// </summary>
        private void ReadSegmentLengths(ref JpxPacketBitReader reader, JpxCodeBlock codeBlock, int codingPasses)
        {
            var style = codeBlock.CodeBlockStyle;

            var firstPass = codeBlock.CodingPasses;
            var lastPass = firstPass + codingPasses - 1;
            var groupStart = firstPass;

            for (var pass = firstPass; pass <= lastPass; pass++)
            {
                var terminated = JpxCodingPass.IsTerminated(style, pass);
                if (!terminated && pass < lastPass)
                {
                    continue;
                }

                var passesInGroup = pass - groupStart + 1;

                // ITU-T T.800 (06/2019) Equation B-19:
                var lengthBits = codeBlock.Lblock + MathUtils.IntLog2(passesInGroup);
                if (lengthBits > MaxSegmentLengthBits)
                {
                    throw new JpxException(
                        "Invalid JPEG 2000 codeword segment length of " + lengthBits +
                        " bits in a packet header.");
                }

                var length = reader.ReadBits(lengthBits);

                bodySlices.Add(new BodySlice
                {
                    CodeBlock = codeBlock,
                    CodingPasses = passesInGroup,
                    Length = length,
                    Terminated = terminated,
                });

                groupStart = pass + 1;
            }

            codeBlock.CodingPasses = lastPass + 1;
        }

        /// <summary>
        /// Decodes the number of coding passes according to ITU-T T.800 (06/2019) Table B.4.
        /// </summary>
        private static int ReadCodingPassCount(ref JpxPacketBitReader reader)
        {
            if (reader.ReadBit() == 0)
            {
                return 1;
            }

            if (reader.ReadBit() == 0)
            {
                return 2;
            }

            var value = reader.ReadBits(2);
            if (value < 3)
            {
                return 3 + value;
            }

            value = reader.ReadBits(5);
            if (value < 31)
            {
                return 6 + value;
            }

            return 37 + reader.ReadBits(7);
        }

        /// <summary>
        /// Fully decodes one tag tree leaf value by raising the threshold until the value is
        /// determined (ITU-T T.800 (06/2019) Section B.10.5: the zero bit-plane count is coded in
        /// the same manner as the inclusion information, but its exact value is needed).
        /// </summary>
        private static int DecodeTagTreeValue(ref JpxPacketBitReader reader, JpxTagTree tree, int leaf)
        {
            var threshold = 1;

            while (!tree.Decode(ref reader, leaf, threshold))
            {
                if (threshold > MaxZeroBitPlanes)
                {
                    throw new JpxException("Invalid JPEG 2000 zero bit-plane count in a packet header");
                }

                threshold++;
            }

            return threshold - 1;
        }

        /// <summary>
        /// Consumes an SOP marker segment at the current position, if present. SOP is optional per packet even when
        /// signalled in the COD, so absence is never an error.
        /// (ITU-T T.800 (06/2019) Section A.8.1)
        /// </summary>
        private void ReadSopMarker(int packetIndex)
        {
            if (tilePartReader.Length - tilePartReader.Cursor < 6 ||
                !tilePartReader.TryReadMarker(JpxMarker.SOP))
            {
                return;
            }

            var segmentReader = tilePartReader.ReadSegmentContent();
            var nsop = segmentReader.ReadUInt16();

            // ITU-T T.800 (06/2019) Table A.40:
            // Nsop counts packets modulo 65536, incremented for every packet whether or not an SOP marker segment is
            // used.
            if (nsop != (packetIndex & 0xFFFF) && !warnedNsopMismatch)
            {
                warnedNsopMismatch = true;
                Log.WriteLine(
                    "JPEG 2000 SOP packet sequence number mismatch (Nsop=" + nsop + ", " +
                    "expected " + (packetIndex & 0xFFFF) + "); decoding best-effort");
            }
        }

        /// <summary>
        /// Consumes an EPH marker after the packet header, if present. A missing EPH is only warned about, so that a
        /// stream with a wrong Scod flag still decodes.
        /// (ITU-T T.800 (06/2019) Section A.8.2)
        /// </summary>
        private void ReadEphMarker()
        {
            // The EPH marker appears in the same stream as the packet header (PPM/PPT data or the bit stream).
            var reader = packedHeaderReader ?? tilePartReader;

            if (!reader.TryReadMarker(JpxMarker.EPH))
            {
                if (parameters.EphMarkerSegments && !warnedMissingEph)
                {
                    warnedMissingEph = true;
                    Log.WriteLine("Missing EPH marker after a JPEG 2000 packet header; decoding best-effort.");
                }
            }
        }

        private struct BodySlice
        {
            public JpxCodeBlock CodeBlock;
            public int CodingPasses;
            public int Length;
            public bool Terminated;
        }

    }
}
