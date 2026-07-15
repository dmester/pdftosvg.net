// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx;
using PdfToSvg.Imaging.Jpx.Container;
using PdfToSvg.Imaging.Jpx.Decoding;
using PdfToSvg.Imaging.Jpx.ImageModel;

namespace PdfToSvg.Tests.Images.Jpx.Decoding
{
    internal class JpxChannelMapTests
    {
        [Test]
        public void Resolve_NoComponentMappingBoxMapsComponentsDirectly()
        {
            var image = Image(3);

            var channelMap = JpxChannelMap.Resolve(image, 3, JpxAlphaMode.None);

            Assert.AreEqual(0, channelMap.ColorMappings[0].ComponentIndex);
            Assert.AreEqual(1, channelMap.ColorMappings[1].ComponentIndex);
            Assert.AreEqual(2, channelMap.ColorMappings[2].ComponentIndex);
        }

        [Test]
        public void Resolve_ComponentMappingBoxAppliesDirectMapping()
        {
            var image = Image(3);
            image.ComponentMappings = new[] { Direct(2), Direct(0), Direct(1) };

            var channelMap = JpxChannelMap.Resolve(image, 3, JpxAlphaMode.None);

            Assert.AreEqual(2, channelMap.ColorMappings[0].ComponentIndex);
            Assert.AreEqual(0, channelMap.ColorMappings[1].ComponentIndex);
            Assert.AreEqual(1, channelMap.ColorMappings[2].ComponentIndex);
        }

        [Test]
        public void Resolve_ComponentMappingReturnsPaletteMapping()
        {
            var image = Image(1);
            image.ComponentMappings = new[] { Palette(0, paletteColumn: 2) };

            var channelMap = JpxChannelMap.Resolve(image, 1, JpxAlphaMode.None);

            Assert.AreEqual(JpxComponentMappingType.Palette, channelMap.ColorMappings[0].Type);
            Assert.AreEqual(2, channelMap.ColorMappings[0].PaletteColumnIndex);
        }

        [Test]
        public void Resolve_ChannelDefinitionOrdersColorChannelsByAssociation()
        {
            // Channel 0 is association 3 (blue), channel 1 is association 1 (red), channel 2 is association 2 (green).
            var image = Image(3);
            image.ChannelDefinitions = new[]
            {
                Cdef(0, JpxChannelType.Color, 3),
                Cdef(1, JpxChannelType.Color, 1),
                Cdef(2, JpxChannelType.Color, 2),
            };

            var channelMap = JpxChannelMap.Resolve(image, 3, JpxAlphaMode.None);

            Assert.AreEqual(1, channelMap.ColorMappings[0].ComponentIndex); // Red   <- channel 1
            Assert.AreEqual(2, channelMap.ColorMappings[1].ComponentIndex); // Green <- channel 2
            Assert.AreEqual(0, channelMap.ColorMappings[2].ComponentIndex); // Blue  <- channel 0
        }

        [Test]
        public void Resolve_ChannelDefinitionAssociationOutOfRangeIsIgnored()
        {
            var image = Image(2);
            image.ChannelDefinitions = new[]
            {
                Cdef(0, JpxChannelType.Color, 1),
                Cdef(1, JpxChannelType.Color, 5), // Out of range for a 2-channel request; falls back below.
            };

            var channelMap = JpxChannelMap.Resolve(image, 2, JpxAlphaMode.None);

            Assert.AreEqual(0, channelMap.ColorMappings[0].ComponentIndex);
            Assert.AreEqual(1, channelMap.ColorMappings[1].ComponentIndex);
        }

        [Test]
        public void Resolve_ChannelDefinitionChannelIndexOutOfRangeIsIgnored()
        {
            var image = Image(2);
            image.ChannelDefinitions = new[]
            {
                Cdef(0, JpxChannelType.Color, 1),
                Cdef(5, JpxChannelType.Color, 2), // No channel 5; ignored entirely.
            };

            var channelMap = JpxChannelMap.Resolve(image, 2, JpxAlphaMode.None);

            Assert.AreEqual(0, channelMap.ColorMappings[0].ComponentIndex);
            Assert.AreEqual(1, channelMap.ColorMappings[1].ComponentIndex);
        }

        [Test]
        public void Resolve_UnassignedColorSlotBeyondAvailableMappingsClampsToLastMapping()
        {
            // Only 2 codestream components/mappings exist, but 3 colour channels are requested (a decode driven by
            // a colour space with more channels than the codestream can supply): the extra slot clamps to the last
            // mapping instead of indexing past the end of the mappings array.
            var image = Image(2);
            image.ChannelDefinitions = new[] { Cdef(0, JpxChannelType.Color, 1) };

            var channelMap = JpxChannelMap.Resolve(image, 3, JpxAlphaMode.None);

            Assert.AreEqual(0, channelMap.ColorMappings[0].ComponentIndex);
            Assert.AreEqual(1, channelMap.ColorMappings[1].ComponentIndex);
            Assert.AreEqual(1, channelMap.ColorMappings[2].ComponentIndex);
        }

        // ---- Resolve: alpha ----

        [Test]
        public void Resolve_ChannelDefinitionDeclaresOpacity()
        {
            var image = Image(2);
            image.ChannelDefinitions = new[]
            {
                Cdef(0, JpxChannelType.Color, 1),
                Cdef(1, JpxChannelType.Opacity, 0),
            };

            var channelMap = JpxChannelMap.Resolve(image, 1, JpxAlphaMode.Opacity);

            Assert.AreEqual(1, channelMap.AlphaComponentIndex);
            Assert.AreEqual(false, channelMap.PremultipliedAlpha);
            Assert.AreEqual(true, channelMap.HasAlphaChannel);
        }

        [Test]
        public void Resolve_ChannelDefinitionDeclaresPremultipliedOpacity()
        {
            var image = Image(2);
            image.ChannelDefinitions = new[]
            {
                Cdef(0, JpxChannelType.Color, 1),
                Cdef(1, JpxChannelType.PremultipliedOpacity, 0),
            };

            var channelMap = JpxChannelMap.Resolve(image, 1, JpxAlphaMode.Premultiplied);

            Assert.AreEqual(1, channelMap.AlphaComponentIndex);
            Assert.AreEqual(true, channelMap.PremultipliedAlpha);
        }

        [Test]
        public void Resolve_ChannelDefinitionFirstOpacityEntryWins()
        {
            var image = Image(3);
            image.ChannelDefinitions = new[]
            {
                Cdef(0, JpxChannelType.Color, 1),
                Cdef(1, JpxChannelType.Opacity, 0),
                Cdef(2, JpxChannelType.Opacity, 0),
            };

            var channelMap = JpxChannelMap.Resolve(image, 1, JpxAlphaMode.Opacity);

            Assert.AreEqual(1, channelMap.AlphaComponentIndex);
        }

        [Test]
        public void Resolve_ChannelDefinitionResolvesAlphaComponentRegardlessOfAlphaMode()
        {
            // The channel definition box is parsed unconditionally; only HasAlphaChannel depends on the mode.
            var image = Image(2);
            image.ChannelDefinitions = new[]
            {
                Cdef(0, JpxChannelType.Color, 1),
                Cdef(1, JpxChannelType.Opacity, 0),
            };

            var channelMap = JpxChannelMap.Resolve(image, 1, JpxAlphaMode.None);

            Assert.AreEqual(1, channelMap.AlphaComponentIndex);
            Assert.AreEqual(false, channelMap.HasAlphaChannel);
        }

        [Test]
        public void Resolve_NoChannelDefinitionSMaskInDataFallsBackToTrailingComponent()
        {
            // 3 codestream components, 2 requested colour channels: the spare trailing component (index 2) becomes
            // alpha when SMaskInData asserts a mask exists.
            var image = Image(3);

            var channelMap = JpxChannelMap.Resolve(image, 2, JpxAlphaMode.Opacity);

            Assert.AreEqual(2, channelMap.AlphaComponentIndex);
            Assert.AreEqual(true, channelMap.HasAlphaChannel);
        }

        [Test]
        public void Resolve_SMaskInDataFallbackRequiresSpareComponent()
        {
            var image = Image(2);

            var channelMap = JpxChannelMap.Resolve(image, 2, JpxAlphaMode.Opacity);

            Assert.AreEqual(-1, channelMap.AlphaComponentIndex);
            Assert.AreEqual(false, channelMap.HasAlphaChannel);
        }

        [Test]
        public void Resolve_FromCodestreamDoesNotApplySMaskInDataFallback()
        {
            var image = Image(3);

            var channelMap = JpxChannelMap.Resolve(image, 2, JpxAlphaMode.FromCodestream);

            Assert.AreEqual(false, channelMap.HasAlphaChannel);
        }

        [Test]
        public void Resolve_AlphaComponentIndexOutOfRangeIsIgnored()
        {
            // The component mapping box points beyond the actual codestream component count.
            var image = Image(1);
            image.ComponentMappings = new[] { Direct(0), Direct(5) };
            image.ChannelDefinitions = new[]
            {
                Cdef(0, JpxChannelType.Color, 1),
                Cdef(1, JpxChannelType.Opacity, 0),
            };

            var channelMap = JpxChannelMap.Resolve(image, 1, JpxAlphaMode.Opacity);

            Assert.AreEqual(-1, channelMap.AlphaComponentIndex);
            Assert.AreEqual(false, channelMap.HasAlphaChannel);
        }

        [Test]
        public void CreateComponentUsages_DirectColorChannelsAreNormalized()
        {
            var image = Image(3);
            var channelMap = JpxChannelMap.Resolve(image, 3, JpxAlphaMode.None);

            var usages = channelMap.CreateComponentUsages(image);

            Assert.AreEqual(
                new[] { JpxComponentUsage.Normalized, JpxComponentUsage.Normalized, JpxComponentUsage.Normalized },
                usages);
        }

        [Test]
        public void CreateComponentUsages_ComponentsNotConsumedAreSkipped()
        {
            var image = Image(3);
            var channelMap = JpxChannelMap.Resolve(image, 1, JpxAlphaMode.None);

            var usages = channelMap.CreateComponentUsages(image);

            Assert.AreEqual(
                new[] { JpxComponentUsage.Normalized, JpxComponentUsage.Skipped, JpxComponentUsage.Skipped },
                usages);
        }

        [Test]
        public void CreateComponentUsages_PaletteMappedComponentsAreRaw()
        {
            var palette = new JpxPaletteBox
            {
                Columns = new[] { new JpxPaletteColumn(precision: 8, signed: false) },
                Values = new int[1, 1],
            };
            var image = Image(1, palette);
            image.ComponentMappings = new[] { Palette(0, paletteColumn: 0) };

            var channelMap = JpxChannelMap.Resolve(image, 1, JpxAlphaMode.None);
            var usages = channelMap.CreateComponentUsages(image);

            Assert.AreEqual(JpxComponentUsage.Raw, usages[0]);
        }

        [Test]
        public void CreateComponentUsages_PaletteMappingWithoutPaletteBoxIsNormalized()
        {
            var image = Image(1); // No palette box, even though the mapping claims to be palette-typed.
            image.ComponentMappings = new[] { Palette(0, paletteColumn: 0) };

            var channelMap = JpxChannelMap.Resolve(image, 1, JpxAlphaMode.None);
            var usages = channelMap.CreateComponentUsages(image);

            Assert.AreEqual(JpxComponentUsage.Normalized, usages[0]);
        }

        [Test]
        public void CreateComponentUsages_AlphaComponentIsNormalized()
        {
            var image = Image(2);
            var channelMap = JpxChannelMap.Resolve(image, 1, JpxAlphaMode.Opacity);

            var usages = channelMap.CreateComponentUsages(image);

            Assert.AreEqual(JpxComponentUsage.Normalized, usages[0]);
            Assert.AreEqual(JpxComponentUsage.Normalized, usages[1]); // The SMaskInData fallback alpha component.
        }

        [Test]
        public void CreateComponentUsages_RawWinsWhenComponentIsBothPaletteAndAlpha()
        {
            // A single channel (mapped through cmap to a palette-indexed component) is described twice in cdef:
            // once as the colour channel, once as opacity for the whole image (ITU-T T.800 (06/2019) I.5.3.6
            // explicitly allows multiple descriptions for one channel). The palette lookup needs the raw indices,
            // so raw must win over the alpha channel's normalization.
            var palette = new JpxPaletteBox
            {
                Columns = new[] { new JpxPaletteColumn(precision: 8, signed: false) },
                Values = new int[1, 1],
            };
            var image = Image(1, palette);
            image.ComponentMappings = new[] { Palette(0, paletteColumn: 0) };
            image.ChannelDefinitions = new[]
            {
                Cdef(0, JpxChannelType.Color, 1),
                Cdef(0, JpxChannelType.Opacity, 0),
            };

            var channelMap = JpxChannelMap.Resolve(image, 1, JpxAlphaMode.Opacity);

            Assert.AreEqual(0, channelMap.AlphaComponentIndex);
            Assert.AreEqual(true, channelMap.HasAlphaChannel);

            var usages = channelMap.CreateComponentUsages(image);

            Assert.AreEqual(JpxComponentUsage.Raw, usages[0]);
        }

        [Test]
        public void CreateComponentUsages_OutOfRangeComponentIndexIsIgnored()
        {
            var image = Image(1);
            image.ComponentMappings = new[] { Direct(5) }; // Points beyond the single actual component.

            var channelMap = JpxChannelMap.Resolve(image, 1, JpxAlphaMode.None);

            JpxComponentUsage[] usages = null;
            Assert.DoesNotThrow(() => usages = channelMap.CreateComponentUsages(image));
            Assert.AreEqual(new[] { JpxComponentUsage.Skipped }, usages);
        }
        private static JpxImageInfo Image(int componentCount, JpxPaletteBox palette = null)
        {
            var components = new JpxComponent[componentCount];
            for (var i = 0; i < componentCount; i++)
            {
                components[i] = new JpxComponent();
            }

            return new JpxImageInfo
            {
                Components = components,
                Palette = palette,
            };
        }

        private static JpxComponentMappingBox Direct(int componentIndex) =>
            new()
            {
                ComponentIndex = componentIndex,
                Type = JpxComponentMappingType.Direct
            };

        private static JpxComponentMappingBox Palette(int componentIndex, int paletteColumn) =>
            new()
            {
                ComponentIndex = componentIndex,
                Type = JpxComponentMappingType.Palette,
                PaletteColumnIndex = paletteColumn,
            };

        private static JpxChannelDefinitionBox Cdef(int channelIndex, JpxChannelType type, int association) =>
            new()
            {
                ChannelIndex = channelIndex,
                Type = type,
                Association = association
            };

    }
}
