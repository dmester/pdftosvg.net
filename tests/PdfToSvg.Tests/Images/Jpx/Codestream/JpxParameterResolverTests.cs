// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx;
using PdfToSvg.Imaging.Jpx.Codestream;
using System.Collections.Generic;

namespace PdfToSvg.Tests.Images.Jpx.Codestream
{
    // ITU-T T.800 (06/2019) Sections A.6.1-A.6.6 and B.12.3 parameter precedence:
    //   Coding style:  Tile-part COC > Tile-part COD > Main COC > Main COD
    //   Quantization:  Tile-part QCC > Tile-part QCD > Main QCC > Main QCD
    //   Progression:   Tile-part POC > Main POC > Tile-part COD > Main COD
    //   ROI:           Tile-part RGN > Main RGN
    internal class JpxParameterResolverTests
    {
        private static JpxCodingStyleDefaults Cod(
            int numberOfLayers = 1,
            JpxCodingProgressionOrder progressionOrder = JpxCodingProgressionOrder.LayerResolutionLevelComponentPosition,
            int numberOfDecompositionLevels = 5,
            int codeBlockWidth = 64,
            int codeBlockHeight = 64,
            bool reversibleFilter = true,
            bool multipleComponentTransformation = false,
            bool sopMarkerSegments = false,
            bool ephMarkerSegments = false)
        {
            return new JpxCodingStyleDefaults
            {
                CodingStyle = new JpxCodingStyles
                {
                    SopMarkerSegments = sopMarkerSegments,
                    EphMarkerSegments = ephMarkerSegments,
                },
                ProgressionOrder = progressionOrder,
                NumberOfLayers = numberOfLayers,
                MultipleComponentTransformation = multipleComponentTransformation,
                NumberOfDecompositionLevels = numberOfDecompositionLevels,
                CodeBlockWidth = codeBlockWidth,
                CodeBlockHeight = codeBlockHeight,
                ReversibleFilter = reversibleFilter,
                PrecinctSize = MaxPrecincts(numberOfDecompositionLevels),
            };
        }

        private static JpxCodingStyleComponent Coc(
            int numberOfDecompositionLevels = 3,
            int codeBlockWidth = 32,
            int codeBlockHeight = 32,
            bool reversibleFilter = false)
        {
            return new JpxCodingStyleComponent
            {
                NumberOfDecompositionLevels = numberOfDecompositionLevels,
                CodeBlockWidth = codeBlockWidth,
                CodeBlockHeight = codeBlockHeight,
                ReversibleFilter = reversibleFilter,
                PrecinctSize = MaxPrecincts(numberOfDecompositionLevels),
            };
        }

        private static JpxPrecinctSize[] MaxPrecincts(int numberOfDecompositionLevels)
        {
            var result = new JpxPrecinctSize[numberOfDecompositionLevels + 1];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = new JpxPrecinctSize { PPx = 15, PPy = 15 };
            }
            return result;
        }

        private static JpxQuantizationDefaults Quant(int numberOfGuardBits, int exponent)
        {
            return new JpxQuantizationDefaults
            {
                NumberOfGuardBits = numberOfGuardBits,
                Values = new[] { new JpxQuantizationDefaultValues { Exponent = exponent } },
            };
        }

        private static JpxProgressionVolume Volume(
            int layerEnd,
            JpxCodingProgressionOrder progressionOrder = JpxCodingProgressionOrder.ResolutionLevelLayerComponentPosition)
        {
            return new JpxProgressionVolume
            {
                ResolutionStart = 0,
                ResolutionEnd = 6,
                ComponentStart = 0,
                ComponentEnd = 3,
                LayerEnd = layerEnd,
                ProgressionOrder = progressionOrder,
            };
        }

        private static JpxHeaderParameters MainHeader(
            JpxCodingStyleDefaults cod = null,
            JpxQuantizationDefaults qcd = null)
        {
            return new JpxHeaderParameters
            {
                CodingStyleDefaults = cod ?? Cod(),
                QuantizationDefaults = qcd ?? Quant(2, 10),
            };
        }

        [Test]
        public void Resolve_MainOnlyDefaults()
        {
            var main = MainHeader(
                cod: Cod(numberOfLayers: 4, progressionOrder: JpxCodingProgressionOrder.ResolutionLevelLayerComponentPosition),
                qcd: Quant(3, 11));

            var resolved = JpxParameterResolver.Resolve(main, null, componentCount: 3);

            Assert.AreEqual(4, resolved.NumberOfLayers);
            Assert.AreEqual(JpxCodingProgressionOrder.ResolutionLevelLayerComponentPosition, resolved.ProgressionOrder);
            Assert.IsNull(resolved.ProgressionOrderChanges);
            Assert.AreEqual(3, resolved.ComponentCodingStyles.Length);
            Assert.AreEqual(3, resolved.ComponentQuantizations.Length);

            for (var c = 0; c < 3; c++)
            {
                Assert.AreEqual(4, resolved.ComponentCodingStyles[c].NumberOfLayers);
                Assert.AreEqual(5, resolved.ComponentCodingStyles[c].NumberOfDecompositionLevels);
                Assert.AreEqual(64, resolved.ComponentCodingStyles[c].CodeBlockWidth);
                Assert.AreEqual(3, resolved.ComponentQuantizations[c].NumberOfGuardBits);
                Assert.AreEqual(11, resolved.ComponentQuantizations[c].Values[0].Exponent);
                Assert.AreEqual(0, resolved.RegionOfInterestShifts[c]);
            }
        }

        [Test]
        public void Resolve_MissingMainCod()
        {
            var main = new JpxHeaderParameters { QuantizationDefaults = Quant(2, 10) };
            Assert.Throws<JpxException>(() => JpxParameterResolver.Resolve(main, null, componentCount: 1));
        }

        [Test]
        public void Resolve_MissingMainQcd()
        {
            var main = new JpxHeaderParameters { CodingStyleDefaults = Cod() };
            Assert.Throws<JpxException>(() => JpxParameterResolver.Resolve(main, null, componentCount: 1));
        }

        [Test]
        public void Resolve_TileCodOverridesMainCod()
        {
            var main = MainHeader(cod: Cod(numberOfLayers: 1, numberOfDecompositionLevels: 5));
            var tilePart = new JpxHeaderParameters
            {
                CodingStyleDefaults = Cod(
                    numberOfLayers: 7,
                    progressionOrder: JpxCodingProgressionOrder.PositionComponentResolutionLevelLayer,
                    numberOfDecompositionLevels: 2,
                    codeBlockWidth: 16,
                    codeBlockHeight: 16,
                    reversibleFilter: false,
                    sopMarkerSegments: true),
            };

            var resolved = JpxParameterResolver.Resolve(main, tilePart, componentCount: 2);

            Assert.AreEqual(7, resolved.NumberOfLayers);
            Assert.AreEqual(JpxCodingProgressionOrder.PositionComponentResolutionLevelLayer, resolved.ProgressionOrder);
            Assert.AreEqual(true, resolved.SopMarkerSegments);
            Assert.AreEqual(false, resolved.EphMarkerSegments);

            for (var c = 0; c < 2; c++)
            {
                Assert.AreEqual(2, resolved.ComponentCodingStyles[c].NumberOfDecompositionLevels);
                Assert.AreEqual(16, resolved.ComponentCodingStyles[c].CodeBlockWidth);
                Assert.AreEqual(false, resolved.ComponentCodingStyles[c].ReversibleFilter);
            }
        }

        // Regression test for the old pipeline defect where the main header layer count was used
        // for packet progression even when a tile-part COD overrode it.
        [Test]
        public void Resolve_LayerCountFromTileCod()
        {
            var main = MainHeader(cod: Cod(numberOfLayers: 10));
            var tilePart = new JpxHeaderParameters { CodingStyleDefaults = Cod(numberOfLayers: 3) };

            var resolved = JpxParameterResolver.Resolve(main, tilePart, componentCount: 1);

            Assert.AreEqual(3, resolved.NumberOfLayers);
            Assert.AreEqual(3, resolved.ComponentCodingStyles[0].NumberOfLayers);
        }

        [Test]
        public void Resolve_TileCocOverridesTileCodForSingleComponent()
        {
            var main = MainHeader(cod: Cod(numberOfDecompositionLevels: 5, codeBlockWidth: 64));
            var tilePart = new JpxHeaderParameters
            {
                CodingStyleDefaults = Cod(numberOfLayers: 2, numberOfDecompositionLevels: 4, codeBlockWidth: 32),
            };
            tilePart.ComponentCodingStyles[1] = Coc(numberOfDecompositionLevels: 1, codeBlockWidth: 8, codeBlockHeight: 8);

            var resolved = JpxParameterResolver.Resolve(main, tilePart, componentCount: 3);

            // Component 1 uses the tile-part COC.
            Assert.AreEqual(1, resolved.ComponentCodingStyles[1].NumberOfDecompositionLevels);
            Assert.AreEqual(8, resolved.ComponentCodingStyles[1].CodeBlockWidth);

            // The other components keep the tile-part COD values.
            Assert.AreEqual(4, resolved.ComponentCodingStyles[0].NumberOfDecompositionLevels);
            Assert.AreEqual(32, resolved.ComponentCodingStyles[0].CodeBlockWidth);
            Assert.AreEqual(4, resolved.ComponentCodingStyles[2].NumberOfDecompositionLevels);
            Assert.AreEqual(32, resolved.ComponentCodingStyles[2].CodeBlockWidth);

            // Tile-scoped SGcod parameters on the overridden component still come from the tile COD,
            // since a COC cannot carry them (ITU-T T.800 Section A.6.2).
            Assert.AreEqual(2, resolved.ComponentCodingStyles[1].NumberOfLayers);
        }

        // ITU-T T.800 (06/2019) Section A.6.2: Tile-part COD > Main COC.
        [Test]
        public void Resolve_TileCodOverridesMainCoc()
        {
            var main = MainHeader(cod: Cod(numberOfDecompositionLevels: 5));
            main.ComponentCodingStyles[0] = Coc(numberOfDecompositionLevels: 1);

            var tilePart = new JpxHeaderParameters
            {
                CodingStyleDefaults = Cod(numberOfDecompositionLevels: 3),
            };

            var resolved = JpxParameterResolver.Resolve(main, tilePart, componentCount: 2);

            Assert.AreEqual(3, resolved.ComponentCodingStyles[0].NumberOfDecompositionLevels);
            Assert.AreEqual(3, resolved.ComponentCodingStyles[1].NumberOfDecompositionLevels);
        }

        [Test]
        public void Resolve_MainCocUsedWithoutTileOverrides()
        {
            var main = MainHeader(cod: Cod(numberOfLayers: 5, numberOfDecompositionLevels: 5));
            main.ComponentCodingStyles[1] = Coc(numberOfDecompositionLevels: 2, codeBlockWidth: 16);

            var resolved = JpxParameterResolver.Resolve(main, new JpxHeaderParameters(), componentCount: 2);

            Assert.AreEqual(5, resolved.ComponentCodingStyles[0].NumberOfDecompositionLevels);
            Assert.AreEqual(2, resolved.ComponentCodingStyles[1].NumberOfDecompositionLevels);
            Assert.AreEqual(16, resolved.ComponentCodingStyles[1].CodeBlockWidth);

            // SGcod parameters come from the main COD.
            Assert.AreEqual(5, resolved.ComponentCodingStyles[1].NumberOfLayers);
        }

        [Test]
        public void Resolve_TileQccOverridesTileQcd()
        {
            var main = MainHeader(qcd: Quant(2, 10));
            var tilePart = new JpxHeaderParameters { QuantizationDefaults = Quant(3, 11) };
            tilePart.ComponentQuantizations[1] = Quant(4, 12);

            var resolved = JpxParameterResolver.Resolve(main, tilePart, componentCount: 3);

            Assert.AreEqual(3, resolved.ComponentQuantizations[0].NumberOfGuardBits);
            Assert.AreEqual(4, resolved.ComponentQuantizations[1].NumberOfGuardBits);
            Assert.AreEqual(12, resolved.ComponentQuantizations[1].Values[0].Exponent);
            Assert.AreEqual(3, resolved.ComponentQuantizations[2].NumberOfGuardBits);
        }

        // ITU-T T.800 (06/2019) Section A.6.5: Tile-part QCD > Main QCC.
        [Test]
        public void Resolve_TileQcdOverridesMainQcc()
        {
            var main = MainHeader(qcd: Quant(2, 10));
            main.ComponentQuantizations[0] = Quant(5, 13);

            var tilePart = new JpxHeaderParameters { QuantizationDefaults = Quant(3, 11) };

            var resolved = JpxParameterResolver.Resolve(main, tilePart, componentCount: 2);

            Assert.AreEqual(3, resolved.ComponentQuantizations[0].NumberOfGuardBits);
            Assert.AreEqual(11, resolved.ComponentQuantizations[0].Values[0].Exponent);
        }

        [Test]
        public void Resolve_MainQccUsedWithoutTileOverrides()
        {
            var main = MainHeader(qcd: Quant(2, 10));
            main.ComponentQuantizations[1] = Quant(5, 13);

            var resolved = JpxParameterResolver.Resolve(main, new JpxHeaderParameters(), componentCount: 2);

            Assert.AreEqual(2, resolved.ComponentQuantizations[0].NumberOfGuardBits);
            Assert.AreEqual(5, resolved.ComponentQuantizations[1].NumberOfGuardBits);
            Assert.AreEqual(13, resolved.ComponentQuantizations[1].Values[0].Exponent);
        }

        // ITU-T T.800 (06/2019) Section B.12.3: Tile-part POC > Main POC > COD.
        [Test]
        public void Resolve_TilePocOverridesMainPoc()
        {
            var main = MainHeader(cod: Cod(numberOfLayers: 8));
            main.ProgressionOrderChanges = new List<JpxProgressionVolume>
            {
                Volume(layerEnd: 2, progressionOrder: JpxCodingProgressionOrder.ResolutionLevelLayerComponentPosition),
            };

            var tilePart = new JpxHeaderParameters
            {
                ProgressionOrderChanges = new List<JpxProgressionVolume>
                {
                    Volume(layerEnd: 5, progressionOrder: JpxCodingProgressionOrder.ComponentPositionResolutionLevelLayer),
                },
            };

            var resolved = JpxParameterResolver.Resolve(main, tilePart, componentCount: 3);

            Assert.IsNotNull(resolved.ProgressionOrderChanges);
            Assert.AreEqual(1, resolved.ProgressionOrderChanges!.Count);
            Assert.AreEqual(5, resolved.ProgressionOrderChanges[0].LayerEnd);
            Assert.AreEqual(
                JpxCodingProgressionOrder.ComponentPositionResolutionLevelLayer,
                resolved.ProgressionOrderChanges[0].ProgressionOrder);
        }

        [Test]
        public void Resolve_MainPocUsedWithoutTilePoc()
        {
            var main = MainHeader(cod: Cod(numberOfLayers: 8));
            main.ProgressionOrderChanges = new List<JpxProgressionVolume>
            {
                Volume(layerEnd: 2),
            };

            var tilePart = new JpxHeaderParameters
            {
                CodingStyleDefaults = Cod(numberOfLayers: 8, progressionOrder: JpxCodingProgressionOrder.PositionComponentResolutionLevelLayer),
            };

            var resolved = JpxParameterResolver.Resolve(main, tilePart, componentCount: 3);

            Assert.IsNotNull(resolved.ProgressionOrderChanges);
            Assert.AreEqual(1, resolved.ProgressionOrderChanges!.Count);
            Assert.AreEqual(2, resolved.ProgressionOrderChanges[0].LayerEnd);
        }

        [Test]
        public void Resolve_CodProgressionUsedWithoutPoc()
        {
            var main = MainHeader(cod: Cod(progressionOrder: JpxCodingProgressionOrder.ResolutionLevelPositionComponentLayer));

            var resolved = JpxParameterResolver.Resolve(main, null, componentCount: 1);

            Assert.IsNull(resolved.ProgressionOrderChanges);
            Assert.AreEqual(JpxCodingProgressionOrder.ResolutionLevelPositionComponentLayer, resolved.ProgressionOrder);
        }

        // A main header POC may reference more layers than the tile has after a tile-part COD
        // lowered the layer count. LYEpoc is bounded by the resolved layer count.
        [Test]
        public void Resolve_PocLayerEndBoundedByTileLayers()
        {
            var main = MainHeader(cod: Cod(numberOfLayers: 10));
            main.ProgressionOrderChanges = new List<JpxProgressionVolume>
            {
                Volume(layerEnd: 10),
            };

            var tilePart = new JpxHeaderParameters { CodingStyleDefaults = Cod(numberOfLayers: 3) };

            var resolved = JpxParameterResolver.Resolve(main, tilePart, componentCount: 3);

            Assert.AreEqual(3, resolved.NumberOfLayers);
            Assert.IsNotNull(resolved.ProgressionOrderChanges);
            Assert.AreEqual(3, resolved.ProgressionOrderChanges![0].LayerEnd);
        }

        [Test]
        public void Resolve_TileRgnOverridesMainRgn()
        {
            var main = MainHeader();
            main.RegionOfInterestShifts[0] = 5;
            main.RegionOfInterestShifts[1] = 6;

            var tilePart = new JpxHeaderParameters();
            tilePart.RegionOfInterestShifts[0] = 9;

            var resolved = JpxParameterResolver.Resolve(main, tilePart, componentCount: 3);

            Assert.AreEqual(9, resolved.RegionOfInterestShifts[0]);
            Assert.AreEqual(6, resolved.RegionOfInterestShifts[1]);
            Assert.AreEqual(0, resolved.RegionOfInterestShifts[2]);
        }

        // The resolver must be re-invocable when a tile-part header adds overrides during the
        // phase-2 tile-part walk, without disturbing previously returned results.
        [Test]
        public void Resolve_ReResolvesAfterLateTilePartOverride()
        {
            var main = MainHeader(cod: Cod(numberOfLayers: 10, numberOfDecompositionLevels: 5), qcd: Quant(2, 10));
            var tilePart = new JpxHeaderParameters();

            var first = JpxParameterResolver.Resolve(main, tilePart, componentCount: 2);
            Assert.AreEqual(10, first.NumberOfLayers);
            Assert.AreEqual(5, first.ComponentCodingStyles[0].NumberOfDecompositionLevels);

            // A later tile-part header adds overrides.
            tilePart.CodingStyleDefaults = Cod(numberOfLayers: 2, numberOfDecompositionLevels: 3);
            tilePart.ComponentQuantizations[1] = Quant(4, 12);

            var second = JpxParameterResolver.Resolve(main, tilePart, componentCount: 2);
            Assert.AreEqual(2, second.NumberOfLayers);
            Assert.AreEqual(3, second.ComponentCodingStyles[0].NumberOfDecompositionLevels);
            Assert.AreEqual(4, second.ComponentQuantizations[1].NumberOfGuardBits);

            // The first result is unaffected by the re-resolution.
            Assert.AreEqual(10, first.NumberOfLayers);
            Assert.AreEqual(5, first.ComponentCodingStyles[0].NumberOfDecompositionLevels);
            Assert.AreEqual(2, first.ComponentQuantizations[1].NumberOfGuardBits);
        }

        [Test]
        public void Resolve_ResultDoesNotAliasInputs()
        {
            var main = MainHeader(cod: Cod(numberOfLayers: 4, numberOfDecompositionLevels: 5), qcd: Quant(2, 10));

            var resolved = JpxParameterResolver.Resolve(main, null, componentCount: 1);

            // Mutating the inputs after resolution must not change the resolved result.
            main.CodingStyleDefaults!.NumberOfDecompositionLevels = 1;
            main.QuantizationDefaults!.NumberOfGuardBits = 7;

            Assert.AreEqual(5, resolved.ComponentCodingStyles[0].NumberOfDecompositionLevels);
            Assert.AreEqual(2, resolved.ComponentQuantizations[0].NumberOfGuardBits);

            // Mutating the resolved result must not change the inputs.
            resolved.ComponentCodingStyles[0].NumberOfLayers = 99;
            Assert.AreEqual(4, main.CodingStyleDefaults!.NumberOfLayers);
        }

        [Test]
        public void Overlay_KeepsTileScopedParametersFromBase()
        {
            var baseStyle = Cod(
                numberOfLayers: 6,
                progressionOrder: JpxCodingProgressionOrder.ResolutionLevelLayerComponentPosition,
                multipleComponentTransformation: true,
                sopMarkerSegments: true);

            var overlaid = JpxParameterResolver.Overlay(baseStyle, Coc(numberOfDecompositionLevels: 2, codeBlockWidth: 16));

            // Component-scoped parameters come from the COC.
            Assert.AreEqual(2, overlaid.NumberOfDecompositionLevels);
            Assert.AreEqual(16, overlaid.CodeBlockWidth);
            Assert.AreEqual(false, overlaid.ReversibleFilter);

            // Tile-scoped parameters are kept from the base COD.
            Assert.AreEqual(6, overlaid.NumberOfLayers);
            Assert.AreEqual(JpxCodingProgressionOrder.ResolutionLevelLayerComponentPosition, overlaid.ProgressionOrder);
            Assert.AreEqual(true, overlaid.MultipleComponentTransformation);
            Assert.AreEqual(true, overlaid.CodingStyle.SopMarkerSegments);

            // The base instance is not mutated.
            Assert.AreEqual(5, baseStyle.NumberOfDecompositionLevels);
        }
    }
}
