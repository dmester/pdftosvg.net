// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.ImageModel;
using PdfToSvg.Imaging.Jpx.IO;
using PdfToSvg.Imaging.Jpx.Packets;
using System;
using System.Collections.Generic;
using System.IO;

namespace PdfToSvg.Imaging.Jpx.Codestream
{
    /// <summary>
    /// Reading codestream according to ITU-T T.800 (06/2019) Annex A
    /// <para>
    ///     Phase 1: <see cref="ReadMainHeader"/> parses SOC → SIZ → the main header marker segments,
    ///     stopping at the first SOT without consuming it.
    /// </para>
    /// <para>
    ///     Phase 2: repeated <see cref="ReadTilePart"/> calls walk the tile-parts until EOC or the end
    ///     of the codestream, returning each tile-part's SOT fields and packet data.
    /// </para>
    /// </summary>
    internal sealed class JpxCodestreamReader
    {
        private readonly JpxDataReader reader;
        private readonly JpxHeaderParameters mainParameters = new();
        private readonly JpxPackedPacketHeaderBuffer packedPacketHeaders = new();
        private readonly Dictionary<int, JpxHeaderParameters> tileParameters = new();
        private readonly Dictionary<int, int> tilePartsSeen = new();

        private JpxImageInfo? image;
        private bool endOfCodestream;

        public JpxCodestreamReader(ArraySegment<byte> codestream)
        {
            reader = new JpxDataReader(codestream);
        }

        public JpxHeaderParameters MainHeaderParameters => mainParameters;

        /// <summary>PPM (phase 1) and PPT (phase 2) packed packet header data.</summary>
        public JpxPackedPacketHeaderBuffer PackedPacketHeaders => packedPacketHeaders;

        /// <summary>
        /// Reads the main header: SOC, SIZ (into <paramref name="image"/>) and the following
        /// marker segments, stopping at the first SOT without consuming it.
        /// </summary>
        public void ReadMainHeader(JpxImageInfo image)
        {
            if (this.image != null)
            {
                throw new InvalidOperationException("The main header has already been read");
            }

            this.image = image;

            try
            {
                ReadMainHeaderCore(image);
            }
            catch (EndOfStreamException ex)
            {
                throw new JpxException("Unexpected end of the JPEG 2000 codestream in the main header", ex);
            }
        }

        private void ReadMainHeaderCore(JpxImageInfo image)
        {
            // ITU-T T.800 (06/2019) Section A.4.1: SOC is the first marker in the codestream.
            if (!reader.TryReadMarker(JpxMarker.SOC))
            {
                throw new JpxException("Missing SOC marker at the start of the JPEG 2000 codestream");
            }

            // ITU-T T.800 (06/2019) Section A.5.1: SIZ immediately follows SOC.
            if (!reader.TryReadMarker(JpxMarker.SIZ))
            {
                throw new JpxException("Expected a SIZ marker segment immediately after SOC");
            }

            JpxMarkerSegmentReader.ReadSiz(reader, image);

            var componentCount = image.Components.Length;

            while (!reader.EndOfStream)
            {
                if (reader.PeekMarker(JpxMarker.SOT))
                {
                    // Phase 1 ends here; ReadTilePart consumes the SOT.
                    return;
                }

                var marker = reader.ReadMarker();

                switch (marker)
                {
                    case JpxMarker.COD:
                        mainParameters.CodingStyleDefaults = JpxMarkerSegmentReader.ReadCod(reader);
                        break;

                    case JpxMarker.COC:
                        var coc = JpxMarkerSegmentReader.ReadCoc(reader, componentCount, out var cocComponent);
                        mainParameters.ComponentCodingStyles[cocComponent] = coc;
                        break;

                    case JpxMarker.QCD:
                        mainParameters.QuantizationDefaults = JpxMarkerSegmentReader.ReadQcd(reader);
                        break;

                    case JpxMarker.QCC:
                        var qcc = JpxMarkerSegmentReader.ReadQcc(reader, componentCount, out var qccComponent);
                        mainParameters.ComponentQuantizations[qccComponent] = qcc;
                        break;

                    case JpxMarker.RGN:
                        if (JpxMarkerSegmentReader.ReadRgn(reader, componentCount, out var rgnComponent, out var maxShift))
                        {
                            mainParameters.RegionOfInterestShifts[rgnComponent] = maxShift;
                        }
                        break;

                    case JpxMarker.POC:
                        mainParameters.ProgressionOrderChanges = JpxMarkerSegmentReader.ReadPoc(reader, componentCount);
                        break;

                    case JpxMarker.CAP:
                        JpxMarkerSegmentReader.ReadExtendedCapabilities(reader);
                        break;

                    case JpxMarker.PPM:
                        packedPacketHeaders.AddMainHeaderSegment(JpxMarkerSegmentReader.ReadPackedPacketHeaders(reader));
                        break;

                    // Marker segments carrying no decode-relevant information (ITU-T T.800 (06/2019) referred below)
                    case JpxMarker.PRF: // Section A.5.3 Profile
                    case JpxMarker.TLM: // Section A.7.1 Tile-part lengths
                    case JpxMarker.PLM: // Section A.7.2 Packet length, main header
                    case JpxMarker.CRG: // Section A.9.1 Component registration
                    case JpxMarker.COM: // Section A.9.2 Comment
                        reader.SkipSegmentContent();
                        break;

                    case JpxMarker.MCT:
                    case JpxMarker.MCC:
                    case JpxMarker.MCO:
                    case JpxMarker.NLT:
                        throw new JpxException(
                            "The JPEG 2000 codestream requires the unsupported " + marker +
                            " transformation extension (ITU-T T.801)");

                    // Extension markers
                    case JpxMarker.QPD:
                    case JpxMarker.QPC:
                    case JpxMarker.DCO:
                    case JpxMarker.VMS:
                    case JpxMarker.DFS:
                    case JpxMarker.ADS:
                    case JpxMarker.CBD:
                    case JpxMarker.ATK:
                        reader.SkipSegmentContent();
                        break;

                    case JpxMarker.EOC: // End of codestream
                        // ITU-T T.800 (06/2019) Section A.4: at least one tile-part is required.
                        throw new JpxException("Unexpected EOC marker in the JPEG 2000 main header");

                    case JpxMarker.SOC: // Start of codestream
                    case JpxMarker.SOD: // Start of data
                    // In bit stream markers:
                    case JpxMarker.SOP: // Start of packet
                    case JpxMarker.EPH: // End of packet header
                        throw new JpxException("Unexpected " + marker + " marker in the JPEG 2000 main header");

                    default:
                        SkipUnknownMarker(marker, "main header");
                        break;
                }
            }

            Log.WriteLine("The JPEG 2000 codestream ended without any tile-part");
        }

        /// <summary>
        /// Reads the next tile-part (SOT, tile-part header markers up to SOD, and the packet data
        /// sized per Psot), or returns null when the EOC marker or the end of the codestream is
        /// reached. <see cref="ReadMainHeader"/> must have been called first.
        /// </summary>
        public JpxCodestreamTilePart? ReadTilePart()
        {
            if (image == null)
            {
                throw new InvalidOperationException("ReadMainHeader must be called before ReadTilePart");
            }

            if (endOfCodestream || reader.EndOfStream)
            {
                return null;
            }

            try
            {
                return ReadTilePartCore();
            }
            catch (EndOfStreamException ex)
            {
                throw new JpxException("Unexpected end of the JPEG 2000 codestream in a tile-part header", ex);
            }
        }

        private JpxCodestreamTilePart? ReadTilePartCore()
        {
            if (reader.TryReadMarker(JpxMarker.EOC))
            {
                // ITU-T T.800 (06/2019) Section A.4.4:
                // EOC is the last marker in the codestream. Any bytes following it are ignored.
                endOfCodestream = true;
                return null;
            }

            var sotPosition = reader.Cursor;

            // ITU-T T.800 (06/2019) Section A.4.2:
            // SOT is the first marker segment of every tile-part header.
            if (!reader.TryReadMarker(JpxMarker.SOT))
            {
                throw new JpxException("Expected an SOT or EOC marker between JPEG 2000 tile-parts");
            }

            var sot = JpxMarkerSegmentReader.ReadSot(reader);

            var tileCount = image!.NumXTiles * image.NumYTiles;
            if (sot.TileIndex < 0 || sot.TileIndex >= tileCount)
            {
                throw new JpxException(
                    "Invalid JPEG 2000 tile index " + sot.TileIndex + " (the image has " + tileCount + " tiles)");
            }

            // ITU-T T.800 (06/2019) Table A.5: Psot is 0 or at least 14 (SOT segment + SOD marker).
            var psot = (uint)sot.TileLength;
            if (psot != 0 && psot < 14)
            {
                throw new JpxException("Invalid JPEG 2000 tile-part length (Psot=" + psot + ")");
            }

            tilePartsSeen.TryGetValue(sot.TileIndex, out var partsSeen);
            var isFirstTilePart = partsSeen == 0;

            // ITU-T T.800 (06/2019) Section A.4.2:
            // Tile-parts of a tile shall appear in TPsot order. Recover best-effort from out-of-order or duplicate
            // indices.
            if (sot.TilePartIndex != partsSeen)
            {
                Log.WriteLine(
                    "Out-of-order or duplicate JPEG 2000 tile-part index for tile " + sot.TileIndex +
                    " (TPsot=" + sot.TilePartIndex + ", expected " + partsSeen + "); decoding best-effort");
            }

            tilePartsSeen[sot.TileIndex] = partsSeen + 1;
            packedPacketHeaders.RegisterTilePart(sot.TileIndex);

            // ITU-T T.800 (06/2019) Section A.6.6: unlike COD/COC/QCD/QCC/RGN, a POC marker segment may appear in
            // any tile-part header of a tile, not just the first, so the tile's parameters must remain reachable
            // for every tile-part. Fall back to a fresh instance if the first tile-part was missing or out of order.
            JpxHeaderParameters tileParams;

            if (isFirstTilePart)
            {
                tileParams = new JpxHeaderParameters();
                tileParameters[sot.TileIndex] = tileParams;
            }
            else if (!tileParameters.TryGetValue(sot.TileIndex, out tileParams!))
            {
                tileParams = new JpxHeaderParameters();
                tileParameters[sot.TileIndex] = tileParams;
            }

            ReadTilePartHeader(sot.TileIndex, tileParams, isFirstTilePart);

            // ITU-T T.800 (06/2019) Section A.4.2:
            // Psot counts from the first byte of the SOT marker to the end of the tile-part data. Psot = 0 means all
            // data until EOC and is only allowed for the last tile-part.
            int dataLength;

            if (psot == 0)
            {
                dataLength = reader.Length - reader.Cursor;

                var segment = reader.Data;
                if (dataLength >= 2 &&
                    segment.Array![segment.Offset + reader.Length - 2] == 0xFF &&
                    segment.Array[segment.Offset + reader.Length - 1] == 0xD9)
                {
                    Log.WriteLine(
                        "JPEG 2000 tile-part with Psot=0: ending the tile-part data at the trailing EOC marker");
                    dataLength -= 2;
                }
                else
                {
                    Log.WriteLine("JPEG 2000 codestream with Psot=0 does not end with an EOC marker");
                }
            }
            else
            {
                var headerLength = reader.Cursor - sotPosition;
                var declaredLength = (long)psot - headerLength;

                if (declaredLength < 0)
                {
                    throw new JpxException(
                        "Invalid JPEG 2000 tile-part length (Psot=" + psot + " is shorter than the tile-part header)");
                }

                var remaining = reader.Length - reader.Cursor;
                if (declaredLength > remaining)
                {
                    Log.WriteLine(
                        "Truncated JPEG 2000 tile-part (Psot=" + psot + " exceeds the codestream); " +
                        "decoding best-effort");
                    declaredLength = remaining;
                }

                dataLength = (int)declaredLength;
            }

            return new JpxCodestreamTilePart
            {
                TileIndex = sot.TileIndex,
                TilePartIndex = sot.TilePartIndex,
                TilePartCount = sot.TilePartCount,
                IsFirstTilePart = isFirstTilePart,
                Data = reader.ReadBytes(dataLength),
            };
        }

        private void ReadTilePartHeader(int tileIndex, JpxHeaderParameters tileParams, bool isFirstTilePart)
        {
            var componentCount = image!.Components.Length;

            while (true)
            {
                var marker = reader.ReadMarker();

                switch (marker)
                {
                    case JpxMarker.SOD:
                        // ITU-T T.800 (06/2019) Section A.4.3: SOD is the last marker of a tile-part header.
                        return;

                    case JpxMarker.COD:
                    case JpxMarker.COC:
                    case JpxMarker.QCD:
                    case JpxMarker.QCC:
                    case JpxMarker.RGN:
                        // ITU-T T.800 (06/2019) Sections A.6.1–A.6.5: these functional marker segments may only
                        // appear in the first tile-part (TPsot = 0) of a tile. Late markers are ignored leniently.
                        if (!isFirstTilePart)
                        {
                            Log.WriteLine(
                                "Ignoring " + marker + " marker segment in a non-first JPEG 2000 " +
                                "tile-part of tile " + tileIndex + " (only allowed in the first tile-part)");
                            reader.SkipSegmentContent();
                            break;
                        }

                        ReadTileFunctionalMarker(marker, tileParams, componentCount);
                        break;

                    case JpxMarker.POC:
                        // ITU-T T.800 (06/2019) Section A.6.6 and B.12.3: unlike the other functional marker
                        // segments, a POC marker segment may appear in any tile-part header of a tile (not just the
                        // first), with each occurrence contributing more progression order volumes to the tile's
                        // POC list, in tile-part order.
                        var poc = JpxMarkerSegmentReader.ReadPoc(reader, componentCount);

                        if (tileParams.ProgressionOrderChanges == null)
                        {
                            tileParams.ProgressionOrderChanges = poc;
                        }
                        else
                        {
                            tileParams.ProgressionOrderChanges.AddRange(poc);
                        }
                        break;

                    case JpxMarker.PPT:
                        packedPacketHeaders.AddTileHeaderSegment(
                            tileIndex,
                            JpxMarkerSegmentReader.ReadPackedPacketHeaders(reader));
                        break;

                    // Known marker segments carrying no decode-relevant information
                    case JpxMarker.PLT: // Packet length, tile-part header
                    case JpxMarker.COM: // Comment
                        reader.SkipSegmentContent();
                        break;

                    case JpxMarker.MCT:
                    case JpxMarker.MCC:
                    case JpxMarker.MCO:
                    case JpxMarker.NLT:
                        throw new JpxException(
                            "The JPEG 2000 codestream requires the unsupported " + marker +
                            " transformation extension (ITU-T T.801)");

                    // Extension markers
                    case JpxMarker.QPD:
                    case JpxMarker.QPC:
                    case JpxMarker.DCO:
                    case JpxMarker.VMS:
                    case JpxMarker.DFS:
                    case JpxMarker.ADS:
                    case JpxMarker.CBD:
                    case JpxMarker.ATK:
                    case JpxMarker.RLT:
                        reader.SkipSegmentContent();
                        break;

                    case JpxMarker.SOT:
                    case JpxMarker.EOC:
                    case JpxMarker.SOC:
                    case JpxMarker.SOP:
                    case JpxMarker.EPH:
                        throw new JpxException(
                            "Unexpected " + marker + " marker in a JPEG 2000 tile-part header (expected SOD)");

                    default:
                        SkipUnknownMarker(marker, "tile-part header");
                        break;
                }
            }
        }

        private void ReadTileFunctionalMarker(JpxMarker marker, JpxHeaderParameters tileParams, int componentCount)
        {
            switch (marker)
            {
                case JpxMarker.COD:
                    tileParams.CodingStyleDefaults = JpxMarkerSegmentReader.ReadCod(reader);
                    break;

                case JpxMarker.COC:
                    var coc = JpxMarkerSegmentReader.ReadCoc(reader, componentCount, out var cocComponent);
                    tileParams.ComponentCodingStyles[cocComponent] = coc;
                    break;

                case JpxMarker.QCD:
                    tileParams.QuantizationDefaults = JpxMarkerSegmentReader.ReadQcd(reader);
                    break;

                case JpxMarker.QCC:
                    var qcc = JpxMarkerSegmentReader.ReadQcc(reader, componentCount, out var qccComponent);
                    tileParams.ComponentQuantizations[qccComponent] = qcc;
                    break;

                case JpxMarker.RGN:
                    if (JpxMarkerSegmentReader.ReadRgn(reader, componentCount, out var rgnComponent, out var maxShift))
                    {
                        tileParams.RegionOfInterestShifts[rgnComponent] = maxShift;
                    }
                    break;
            }
        }

        /// <summary>
        /// Resolves the currently effective parameters for one tile through <see cref="JpxParameterResolver"/>.
        /// Call again after further tile-parts of the tile have been read, since a tile-part header may add overrides.
        /// </summary>
        public JpxResolvedTileParameters ResolveTileParameters(int tileIndex)
        {
            if (image == null)
            {
                throw new InvalidOperationException("ReadMainHeader must be called before ResolveTileParameters");
            }

            if (!tileParameters.TryGetValue(tileIndex, out var resolvedTileParameters))
            {
                resolvedTileParameters = null;
            }

            return JpxParameterResolver.Resolve(mainParameters, resolvedTileParameters, image.Components.Length);
        }

        private void SkipUnknownMarker(JpxMarker marker, string location)
        {
            var code = (int)marker;

            // ITU-T T.800 (06/2019) Section A.1.3: markers 0xFF30–0xFF3F have no marker segment parameters and shall
            // be skipped.
            if (code >= 0xff30 && code <= 0xff3f)
            {
                return;
            }

            // ITU-T T.800 (06/2019) Table A.1: Refuse markers defined in other standards
            if (code == 0xff00 ||
                code == 0xff01 ||
                code == 0xfffe ||
                code >= 0xffc0 && code <= 0xffdf)
            {
                throw new JpxException(
                    "Unexpected T.81 (JPEG) marker 0x" + code.ToString("x4") + " in the JPEG 2000 " + location);
            }
            if (code >= 0xfff0 && code <= 0xfff6)
            {
                throw new JpxException(
                    "Unexpected T.84 (JPEG extension) marker 0x" + code.ToString("x4") + " in the JPEG 2000 " + location);
            }
            if (code >= 0xfff7 && code <= 0xfff8)
            {
                throw new JpxException(
                    "Unexpected T.87 (JPEG-LS) marker 0x" + code.ToString("x4") + " in the JPEG 2000 " + location);
            }

            Log.WriteLine("Skipping unknown marker segment 0x" + code.ToString("x4") + " in the JPEG 2000 " + location);
            reader.SkipSegmentContent();
        }
    }
}
