// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.Codestream;
using System;

namespace PdfToSvg.Imaging.Jpx.ImageModel
{
    /// <summary>
    /// Builds the tile layout of the image from resolved per-tile coding parameters:
    /// <list type="bullet">
    ///    <item>tiles and tile-components (ITU-T T.800 (06/2019) Section B.3)</item>
    ///    <item>resolution levels, sub-bands, precincts and code-blocks (Sections B.5 to B.7)</item>
    /// </list>
    /// </summary>
    internal static class JpxTileLayoutBuilder
    {
        /// <summary>
        /// Builds all tiles of the image using the same resolved parameters. Tiles whose parameters are later
        /// overridden by a tile-part header are rebuilt individually with <see cref="BuildTile"/>.
        /// </summary>
        public static JpxTile[] Build(
            JpxImageInfo image,
            JpxResolvedTileParameters parameters,
            int resolutionReduction = 0)
        {
            // ITU-T T.800 (06/2019) Section B.3 Equation B-5
            var tileCount = (long)image.NumXTiles * image.NumYTiles;

            // Normally already enforced when parsing the SIZ marker segment, but kept here so the
            // builder cannot be tricked into unbounded allocation.
            if (tileCount < 1 || tileCount > JpxConstraints.MaxTileCount)
            {
                throw new JpxException(
                    "Number of tiles (" + tileCount + ") in this JPEG 2000 image exceeds the " +
                    "maximum allowed tiles (" + JpxConstraints.MaxTileCount + ")");
            }

            var tiles = new JpxTile[tileCount];

            for (var tileIndex = 0; tileIndex < tiles.Length; tileIndex++)
            {
                tiles[tileIndex] = BuildTile(image, tileIndex, parameters, resolutionReduction);
            }

            return tiles;
        }

        /// <summary>
        /// Builds the geometry of a single tile from resolved per-tile parameters.
        /// <paramref name="resolutionReduction"/> is the number of highest resolution levels that are discarded by
        /// the decode (0 decodes the full resolution).
        /// </summary>
        public static JpxTile BuildTile(
            JpxImageInfo image,
            int tileIndex,
            JpxResolvedTileParameters parameters,
            int resolutionReduction = 0)
        {
            var components = image.Components;
            if (components == null || components.Length < 1)
            {
                throw new JpxException("JPEG 2000 image contains no components");
            }

            if (parameters.ComponentCodingStyles.Length != components.Length ||
                parameters.ComponentQuantizations.Length != components.Length ||
                parameters.RegionOfInterestShifts.Length != components.Length)
            {
                throw new JpxException("Resolved JPEG 2000 tile parameters do not match the component count.");
            }

            var numXTiles = image.NumXTiles;
            var numYTiles = image.NumYTiles;
            if (tileIndex < 0 || tileIndex >= numXTiles * numYTiles)
            {
                throw new JpxException("Invalid JPEG 2000 tile index (" + tileIndex + ").");
            }

            // ITU-T T.800 (06/2019) Section B.3 Equation B-6
            var p = tileIndex % numXTiles;
            var q = tileIndex / numXTiles;

            // ITU-T T.800 (06/2019) Section B.3 Equations B-7 to B-10. 64-bit arithmetic so a
            // large XTsiz/YTsiz cannot overflow before being clamped to the (bounded) image area.
            var tile = new JpxTile
            {
                Index = tileIndex,
                TX0 = (int)Math.Max((long)image.XTOsiz + (long)p * image.XTsiz, image.XOsiz),
                TY0 = (int)Math.Max((long)image.YTOsiz + (long)q * image.YTsiz, image.YOsiz),
                TX1 = (int)Math.Min((long)image.XTOsiz + (p + 1L) * image.XTsiz, image.Xsiz),
                TY1 = (int)Math.Min((long)image.YTOsiz + (q + 1L) * image.YTsiz, image.Ysiz),
            };

            var tileComponents = new JpxTileComponent[components.Length];

            for (var c = 0; c < components.Length; c++)
            {
                tileComponents[c] = BuildTileComponent(
                    tile,
                    c,
                    components[c],
                    parameters.ComponentCodingStyles[c],
                    parameters.ComponentQuantizations[c],
                    parameters.RegionOfInterestShifts[c],
                    resolutionReduction);
            }

            tile.Components = tileComponents;
            return tile;
        }

        private static JpxTileComponent BuildTileComponent(
            JpxTile tile,
            int componentIndex,
            JpxComponent component,
            JpxCodingStyleDefaults codingStyle,
            JpxQuantizationDefaults quantization,
            int regionOfInterestShift,
            int resolutionReduction)
        {
            // ITU-T T.800 (06/2019) Section B.3 Equation B-12
            var tileComponent = new JpxTileComponent
            {
                ComponentIndex = componentIndex,
                TcX0 = MathUtils.CeilDiv(tile.TX0, component.XRsizi),
                TcY0 = MathUtils.CeilDiv(tile.TY0, component.YRsizi),
                TcX1 = MathUtils.CeilDiv(tile.TX1, component.XRsizi),
                TcY1 = MathUtils.CeilDiv(tile.TY1, component.YRsizi),
                CodingStyle = codingStyle,
                Quantization = quantization,
                RegionOfInterestShift = regionOfInterestShift,
            };

            // The reduction is computed from the main header decomposition levels, so this can only be hit by a
            // tile-part COD/COC lowering the levels below the main header minimum. A reconstruction coarser than
            // resolution level 0 does not exist (ITU-T T.800 (06/2019) Section B.5).
            if (resolutionReduction < 0 || resolutionReduction > codingStyle.NumberOfDecompositionLevels)
            {
                throw new JpxException(
                    "The JPEG 2000 tile-component has too few decomposition levels (" +
                    codingStyle.NumberOfDecompositionLevels + ") for a decode with resolution reduction " +
                    resolutionReduction);
            }

            tileComponent.DecodedResolutionLevelCount =
                codingStyle.NumberOfDecompositionLevels - resolutionReduction + 1;

            tileComponent.ResolutionLevels = BuildResolutionLevels(
                tileComponent.TcX0,
                tileComponent.TcY0,
                tileComponent.TcX1,
                tileComponent.TcY1,
                codingStyle,
                quantization,
                regionOfInterestShift);

            return tileComponent;
        }

        /// <summary>
        /// Builds the resolution levels r = 0..NL of a tile-component whose coordinates are given by Equation B-12,
        /// using the resolved coding style and quantization of the tile-component.
        /// <paramref name="regionOfInterestShift"/> is the resolved RGN max-shift value s of the component
        /// (0 when no ROI applies).
        /// </summary>
        public static JpxResolutionLevel[] BuildResolutionLevels(
            int tcX0,
            int tcY0,
            int tcX1,
            int tcY1,
            JpxCodingStyleDefaults codingStyle,
            JpxQuantizationDefaults quantization,
            int regionOfInterestShift = 0)
        {
            var decompositionLevels = codingStyle.NumberOfDecompositionLevels;

            // Normally already enforced when parsing the RGN marker segment, but kept here so a
            // malformed shift cannot inflate the bit-plane counts below.
            if (regionOfInterestShift < 0 || regionOfInterestShift > JpxConstraints.MaxRegionOfInterestShift)
            {
                throw new JpxException("Invalid JPEG 2000 region of interest shift (" + regionOfInterestShift + ")");
            }

            if (codingStyle.PrecinctSize.Length < decompositionLevels + 1)
            {
                throw new JpxException("Missing JPEG 2000 precinct size for resolution level");
            }

            ValidateQuantization(decompositionLevels, quantization);

            var resolutions = new JpxResolutionLevel[decompositionLevels + 1];

            for (var level = 0; level <= decompositionLevels; level++)
            {
                var resolution = new JpxResolutionLevel
                {
                    Level = level,

                    // ITU-T T.800 (06/2019) Equation B-14
                    TrX0 = MathUtils.CeilDivPow2(tcX0, decompositionLevels - level),
                    TrY0 = MathUtils.CeilDivPow2(tcY0, decompositionLevels - level),
                    TrX1 = MathUtils.CeilDivPow2(tcX1, decompositionLevels - level),
                    TrY1 = MathUtils.CeilDivPow2(tcY1, decompositionLevels - level),
                };

                if (level == 0)
                {
                    // ITU-T T.800 (06/2019) Section B.5:
                    // Resolution level 0 is the NL-LL band, whose coordinates per Equation B-15 (xo = yo = 0) equal the
                    // Equation B-14 coordinates of resolution level 0
                    resolution.SubBands = new[]
                    {
                        BuildSubBand(
                            resolution,
                            codingStyle,
                            quantization,
                            regionOfInterestShift,
                            JpxSubBandType.LL,
                            decompositionLevel: decompositionLevels,
                            quantizationIndex: 0,
                            resolution.TrX0, resolution.TrY0, resolution.TrX1, resolution.TrY1,
                            localX0: 0,
                            localY0: 0),
                    };
                }
                else
                {
                    // ITU-T T.800 (06/2019) Section B.5:
                    // Resolution level r > 0 holds the HL, LH and HH bands of decomposition level nb = NL - r + 1
                    // (Equation B-15 with the (xo, yo) values from Table B.1)
                    var nb = decompositionLevels - level + 1;

                    var lowX0 = MathUtils.CeilDivPow2(tcX0, nb);
                    var lowY0 = MathUtils.CeilDivPow2(tcY0, nb);
                    var lowX1 = MathUtils.CeilDivPow2(tcX1, nb);
                    var lowY1 = MathUtils.CeilDivPow2(tcY1, nb);

                    var highOffset = 1L << (nb - 1);
                    var highX0 = MathUtils.CeilDivPow2(tcX0 - highOffset, nb);
                    var highY0 = MathUtils.CeilDivPow2(tcY0 - highOffset, nb);
                    var highX1 = MathUtils.CeilDivPow2(tcX1 - highOffset, nb);
                    var highY1 = MathUtils.CeilDivPow2(tcY1 - highOffset, nb);

                    // In the split layout of JpxTileComponent.Samples, the high-pass bands are placed after the
                    // low-pass part, whose size equals resolution level r - 1.
                    var lowWidth = resolutions[level - 1].Width;
                    var lowHeight = resolutions[level - 1].Height;

                    // Quantization values appear in the order LL, then HL, LH, HH per decomposition level from the
                    // highest level (ITU-T T.800 (06/2019) Section A.6.4).
                    var quantizationIndex = 1 + (level - 1) * 3;

                    resolution.SubBands =
                    [
                        BuildSubBand(resolution, codingStyle, quantization, regionOfInterestShift,
                            JpxSubBandType.HL, nb,
                            quantizationIndex, highX0, lowY0, highX1, lowY1, lowWidth, 0),
                        BuildSubBand(resolution, codingStyle, quantization, regionOfInterestShift,
                            JpxSubBandType.LH, nb,
                            quantizationIndex + 1, lowX0, highY0, lowX1, highY1, 0, lowHeight),
                        BuildSubBand(resolution, codingStyle, quantization, regionOfInterestShift,
                            JpxSubBandType.HH, nb,
                            quantizationIndex + 2, highX0, highY0, highX1, highY1, lowWidth, lowHeight),
                    ];
                }

                BuildPrecincts(resolution, codingStyle);
                resolutions[level] = resolution;
            }

            return resolutions;
        }

        private static JpxResolutionLevelSubBand BuildSubBand(
            JpxResolutionLevel resolution,
            JpxCodingStyleDefaults codingStyle,
            JpxQuantizationDefaults quantization,
            int regionOfInterestShift,
            JpxSubBandType type,
            int decompositionLevel,
            int quantizationIndex,
            int tbX0,
            int tbY0,
            int tbX1,
            int tbY1,
            int localX0,
            int localY0)
        {
            GetQuantizationValue(
                quantization, codingStyle, type, decompositionLevel, quantizationIndex,
                out var exponent, out var mantissa);

            var subBand = new JpxResolutionLevelSubBand
            {
                Type = type,
                DecompositionLevel = decompositionLevel,
                TbX0 = tbX0,
                TbY0 = tbY0,
                TbX1 = tbX1,
                TbY1 = tbY1,
                LocalX0 = localX0,
                LocalY0 = localY0,
                Exponent = exponent,
                Mantissa = mantissa,

                // ITU-T T.800 (06/2019) Equation E-2 gives Mb = G + eb - 1. When an RGN marker segment applies to the
                // component, the code-blocks are entropy coded with M'b = Mb + s magnitude bit-planes (Equation H-3),
                // so the Tier-1 decoder must decode s additional bit-planes. The added planes are stripped again by
                // JpxQuantizer.ApplyRoiShift.
                MagnitudeBitPlanes = quantization.NumberOfGuardBits + exponent - 1 + regionOfInterestShift,

                CodeBlockStyle = codingStyle.CodeBlockStyle,
            };

            BuildCodeBlocks(subBand, resolution, codingStyle);
            return subBand;
        }

        private static void GetQuantizationValue(
            JpxQuantizationDefaults quantization,
            JpxCodingStyleDefaults codingStyle,
            JpxSubBandType type,
            int decompositionLevel,
            int quantizationIndex,
            out int exponent,
            out int mantissa)
        {
            if (quantization.ScalarDerived)
            {
                if (quantization.Values.Length == 0)
                {
                    throw new JpxException("Missing JPEG 2000 scalar derived quantization value");
                }

                // ITU-T T.800 (06/2019) Equation E-5:
                // The sub-band values are derived from the NL-LL value as (eb, ub) = (e0 - NL + nb, u0)
                var baseValue = quantization.Values[0];
                exponent = baseValue.Exponent - codingStyle.NumberOfDecompositionLevels + decompositionLevel;
                mantissa = baseValue.Mantissa;
            }
            else
            {
                // The number of signalled values is derived from the QCD/QCC segment length, so a
                // malformed segment can hold fewer values than there are sub-bands.
                if (quantizationIndex < 0 || quantizationIndex >= quantization.Values.Length)
                {
                    throw new JpxException("Missing JPEG 2000 quantization value for sub-band");
                }

                exponent = quantization.Values[quantizationIndex].Exponent;
                mantissa = quantization.Values[quantizationIndex].Mantissa;
            }
        }

        private static void ValidateQuantization(int decompositionLevels, JpxQuantizationDefaults quantization)
        {
            // ITU-T T.800 (06/2019) Sections A.6.4/A.6.5:
            // No quantization (reversible) and scalar expounded signal one value for the NL-LL band plus three values
            // per decomposition level. Scalar derived signals only the NL-LL value.
            var expectedValueCount = quantization.ScalarDerived
                ? 1
                : 1 + decompositionLevels * 3;

            // ITU-T T.800 (06/2019) Section A.6.4: QCD carries enough values for the tile-component with the greatest
            // number of decomposition levels, so components with fewer levels legitimately leave an unused tail. QCC
            // is component-specific and must have the exact count for that component.
            if (quantization.Values.Length < expectedValueCount ||
                quantization.ComponentSpecific && quantization.Values.Length != expectedValueCount)
            {
                throw new JpxException("Invalid JPEG 2000 quantization value count.");
            }
        }

        private static void BuildCodeBlocks(
            JpxResolutionLevelSubBand subBand,
            JpxResolutionLevel resolution,
            JpxCodingStyleDefaults codingStyle)
        {
            // ITU-T T.800 (06/2019) Section B.7
            var precinctSize = codingStyle.PrecinctSize[resolution.Level];

            var xcb = MathUtils.IntLog2(codingStyle.CodeBlockWidth);
            var ycb = MathUtils.IntLog2(codingStyle.CodeBlockHeight);

            // ITU-T T.800 (06/2019) Equation B-17:
            var xcbSubBand = resolution.Level == 0
                ? Math.Min(xcb, precinctSize.PPx)
                : Math.Min(xcb, precinctSize.PPx - 1);

            // ITU-T T.800 (06/2019) Equation B-18:
            var ycbSubBand = resolution.Level == 0
                ? Math.Min(ycb, precinctSize.PPy)
                : Math.Min(ycb, precinctSize.PPy - 1);

            // PPx/PPy = 0 is only allowed for r = 0 (ITU-T T.800 (06/2019) Section B.6). The marker parser rejects
            // such streams, but guard here as well since an invalid size would produce a nonsensical partition below
            if (xcbSubBand < 0 || ycbSubBand < 0)
            {
                throw new JpxException("Invalid JPEG 2000 precinct size");
            }

            var codeBlockWidth = 1 << xcbSubBand;
            var codeBlockHeight = 1 << ycbSubBand;

            subBand.CodeBlockWidth = codeBlockWidth;
            subBand.CodeBlockHeight = codeBlockHeight;

            if (subBand.Width <= 0 || subBand.Height <= 0)
            {
                subBand.CodeBlocks = ArrayUtils.Empty<JpxCodeBlock>();
                return;
            }

            // The code-block partition is anchored at (0, 0) in the sub-band domain.
            // Code-blocks are clipped to the sub-band area.
            var gridX0 = MathUtils.FloorDiv(subBand.TbX0, codeBlockWidth);
            var gridY0 = MathUtils.FloorDiv(subBand.TbY0, codeBlockHeight);
            var gridX1 = MathUtils.CeilDiv(subBand.TbX1, codeBlockWidth);
            var gridY1 = MathUtils.CeilDiv(subBand.TbY1, codeBlockHeight);

            subBand.CodeBlockGridX0 = gridX0;
            subBand.CodeBlockGridY0 = gridY0;
            subBand.CodeBlockCountX = gridX1 - gridX0;
            subBand.CodeBlockCountY = gridY1 - gridY0;

            var codeBlockCount = (long)subBand.CodeBlockCountX * subBand.CodeBlockCountY;
            if (codeBlockCount > JpxConstraints.MaxCodeBlocksPerSubBand)
            {
                throw new JpxException(
                    "The JPEG 2000 sub-band has too many code-blocks (" + codeBlockCount + "). " +
                    "Max code-blocks per sub-band is " + JpxConstraints.MaxCodeBlocksPerSubBand);
            }

            var codeBlocks = new JpxCodeBlock[codeBlockCount];
            var codeBlockIndex = 0;

            for (var gridY = gridY0; gridY < gridY1; gridY++)
            {
                for (var gridX = gridX0; gridX < gridX1; gridX++)
                {
                    var x0 = Math.Max(subBand.TbX0, gridX * codeBlockWidth);
                    var y0 = Math.Max(subBand.TbY0, gridY * codeBlockHeight);
                    var x1 = Math.Min(subBand.TbX1, (gridX + 1) * codeBlockWidth);
                    var y1 = Math.Min(subBand.TbY1, (gridY + 1) * codeBlockHeight);

                    codeBlocks[codeBlockIndex++] = new JpxCodeBlock(
                        subBand,
                        gridX,
                        gridY,
                        x0, y0, x1, y1,
                        localX0: subBand.LocalX0 + x0 - subBand.TbX0,
                        localY0: subBand.LocalY0 + y0 - subBand.TbY0);
                }
            }

            subBand.CodeBlocks = codeBlocks;
        }

        private static void BuildPrecincts(JpxResolutionLevel resolution, JpxCodingStyleDefaults codingStyle)
        {
            var precinctSize = codingStyle.PrecinctSize[resolution.Level];
            var precinctWidth = 1 << precinctSize.PPx;
            var precinctHeight = 1 << precinctSize.PPy;

            resolution.PrecinctWidth = precinctWidth;
            resolution.PrecinctHeight = precinctHeight;

            // ITU-T T.800 (06/2019) Equation B-16:
            var gridX0 = MathUtils.FloorDiv(resolution.TrX0, precinctWidth);
            var gridY0 = MathUtils.FloorDiv(resolution.TrY0, precinctHeight);
            var countX = resolution.TrX1 > resolution.TrX0
                ? MathUtils.CeilDiv(resolution.TrX1, precinctWidth) - gridX0
                : 0;
            var countY = resolution.TrY1 > resolution.TrY0
                ? MathUtils.CeilDiv(resolution.TrY1, precinctHeight) - gridY0
                : 0;

            resolution.PrecinctGridX0 = gridX0;
            resolution.PrecinctGridY0 = gridY0;
            resolution.PrecinctCountX = countX;
            resolution.PrecinctCountY = countY;

            var precinctCount = (long)countX * countY;
            if (precinctCount > JpxConstraints.MaxPrecinctsPerResolution)
            {
                throw new JpxException(
                    "The JPEG 2000 resolution level has too many precincts (" + precinctCount + "). " +
                    "Max precincts per resolution is " + JpxConstraints.MaxPrecinctsPerResolution);
            }

            if (countX < 1 || countY < 1)
            {
                resolution.Precincts = ArrayUtils.Empty<JpxPrecinct>();
                return;
            }

            var precincts = new JpxPrecinct[countX * countY];

            for (var y = 0; y < countY; y++)
            {
                for (var x = 0; x < countX; x++)
                {
                    var index = x + y * countX;
                    var gridX = gridX0 + x;
                    var gridY = gridY0 + y;

                    var precinct = new JpxPrecinct
                    {
                        Index = index,
                        GridX = gridX,
                        GridY = gridY,
                        X0 = Math.Max(resolution.TrX0, gridX * precinctWidth),
                        Y0 = Math.Max(resolution.TrY0, gridY * precinctHeight),
                        X1 = Math.Min(resolution.TrX1, (gridX + 1) * precinctWidth),
                        Y1 = Math.Min(resolution.TrY1, (gridY + 1) * precinctHeight),
                        SubBands = new JpxPrecinctSubBand[resolution.SubBands.Length],
                    };

                    for (var s = 0; s < resolution.SubBands.Length; s++)
                    {
                        precinct.SubBands[s] = BuildPrecinctSubBand(resolution, resolution.SubBands[s], gridX, gridY);
                    }

                    precincts[index] = precinct;
                }
            }

            resolution.Precincts = precincts;
        }

        private static JpxPrecinctSubBand BuildPrecinctSubBand(
            JpxResolutionLevel resolution,
            JpxResolutionLevelSubBand subBand,
            int precinctGridX,
            int precinctGridY)
        {
            // ITU-T T.800 (06/2019) Sections B.6/B.7: for r > 0 the precinct projects to half size in the sub-band
            // domain. Both partitions are anchored at (0, 0), and the code-block size never exceeds the projected
            // precinct size (Equations B-17/B-18), so code-block boundaries always align with precinct boundaries.
            var shift = resolution.Level == 0 ? 0 : 1;

            var bandX0 = (precinctGridX * resolution.PrecinctWidth) >> shift;
            var bandY0 = (precinctGridY * resolution.PrecinctHeight) >> shift;
            var bandX1 = ((precinctGridX + 1) * resolution.PrecinctWidth) >> shift;
            var bandY1 = ((precinctGridY + 1) * resolution.PrecinctHeight) >> shift;

            return new JpxPrecinctSubBand
            {
                CodeBlockX0 = Math.Max(MathUtils.FloorDiv(bandX0, subBand.CodeBlockWidth), subBand.CodeBlockGridX0),
                CodeBlockY0 = Math.Max(MathUtils.FloorDiv(bandY0, subBand.CodeBlockHeight), subBand.CodeBlockGridY0),
                CodeBlockX1 = Math.Min(MathUtils.CeilDiv(bandX1, subBand.CodeBlockWidth), subBand.CodeBlockGridX0 + subBand.CodeBlockCountX),
                CodeBlockY1 = Math.Min(MathUtils.CeilDiv(bandY1, subBand.CodeBlockHeight), subBand.CodeBlockGridY0 + subBand.CodeBlockCountY),
            };
        }
    }
}
