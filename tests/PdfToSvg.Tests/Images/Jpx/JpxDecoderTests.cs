// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Imaging.Jpx;
using PdfToSvg.Imaging.Jpx.Container;
using System;
using System.Linq;
using System.Threading;

namespace PdfToSvg.Tests.Images.Jpx
{
    // Phase-1 metadata population: JpxDecoder.ReadMetadata parses the container and the
    // codestream main header only (SOC up to the first SOT).
    internal class JpxDecoderTests
    {
        [Test]
        public void GetChannelInfo_ChannelDefinitionDeclaresOpacity()
        {
            var cdef = Jp2ChannelDefinitionEntries(Cdef(0, 0, 1), Cdef(1, 0, 2), Cdef(2, 0, 3), Cdef(3, 1, 0));
            var data = Jp2Container(4, cmapContent: null, cdefContent: cdef);

            Assert.AreEqual(true, GetChannelInfo(data, JpxAlphaMode.Opacity).HasAlphaChannel);

            // JpxAlphaMode.None still ignores the declared opacity channel.
            Assert.AreEqual(false, GetChannelInfo(data, JpxAlphaMode.None).HasAlphaChannel);
        }

        [Test]
        public void GetChannelInfo_ChannelDefinitionsCountColorChannelsOnly()
        {
            var cdef = Jp2ChannelDefinitionEntries(Cdef(0, 0, 1), Cdef(1, 0, 2), Cdef(2, 0, 3), Cdef(3, 1, 0));
            var data = Jp2Container(4, cmapContent: null, cdefContent: cdef);

            // 3 colour channels + 1 opacity channel declared via cdef; the count is driven by the
            // cdef box regardless of alpha mode once the box is present.
            Assert.AreEqual(3, GetChannelInfo(data, JpxAlphaMode.None).ColorChannelCount);
            Assert.AreEqual(3, GetChannelInfo(data, JpxAlphaMode.Opacity).ColorChannelCount);
        }

        [Test]
        public void GetChannelInfo_ChannelDefinitionUnspecifiedChannelFallsBackToAlpha()
        {
            // cdef declares 2 colour channels and one channel with the ITU-T T.800 (06/2019) Table I.18 "not
            // specified" Ctype (0xFFFF). SMaskInData still asserts a mask exists, so the fallback claims the
            // unspecified channel as alpha without touching the colour channel count.
            var cdef = Jp2ChannelDefinitionEntries(Cdef(0, 0, 1), Cdef(1, 0, 2), Cdef(2, 0xffff, 0));
            var data = Jp2Container(3, cmapContent: null, cdefContent: cdef);

            var channelInfo = GetChannelInfo(data, JpxAlphaMode.Opacity);

            Assert.AreEqual(2, channelInfo.ColorChannelCount);
            Assert.AreEqual(true, channelInfo.HasAlphaChannel);
        }

        [Test]
        public void GetChannelInfo_ChannelDefinitionWithoutUnspecifiedOrOpacityReportsNoAlpha()
        {
            // cdef declares a colour channel for every component and nothing else: even though SMaskInData asserts
            // a mask exists, there is no declared opacity or unspecified channel for the fallback to claim.
            var cdef = Jp2ChannelDefinitionEntries(Cdef(0, 0, 1), Cdef(1, 0, 2), Cdef(2, 0, 3));
            var data = Jp2Container(3, cmapContent: null, cdefContent: cdef);

            var channelInfo = GetChannelInfo(data, JpxAlphaMode.Opacity);

            Assert.AreEqual(3, channelInfo.ColorChannelCount);
            Assert.AreEqual(false, channelInfo.HasAlphaChannel);
        }

        [Test]
        public void GetChannelInfo_FromCodestreamIgnoresUnspecifiedChannel()
        {
            // FromCodestream trusts only channels explicitly typed as opacity in cdef; an unspecified channel does
            // not count as alpha under this mode.
            var cdef = Jp2ChannelDefinitionEntries(Cdef(0, 0, 1), Cdef(1, 0, 2), Cdef(2, 0xffff, 0));
            var data = Jp2Container(3, cmapContent: null, cdefContent: cdef);

            Assert.AreEqual(false, GetChannelInfo(data, JpxAlphaMode.FromCodestream).HasAlphaChannel);
        }

        [Test]
        public void GetChannelInfo_NoChannelDefinitionsSMaskInDataFallsBackToTrailingComponent()
        {
            var data = Jp2Container(4, cmapContent: null, cdefContent: null);

            // No cdef box: the trailing component (index 3) is assumed to be the SMaskInData alpha channel whenever
            // there is more than one component to begin with.
            Assert.AreEqual(true, GetChannelInfo(data, JpxAlphaMode.Opacity).HasAlphaChannel);
            Assert.AreEqual(true, GetChannelInfo(data, JpxAlphaMode.Premultiplied).HasAlphaChannel);
        }

        [Test]
        public void GetChannelInfo_NoChannelDefinitionsSubtractsAlphaComponent()
        {
            var data = Jp2Container(4, cmapContent: null, cdefContent: null);

            // No cdef box: falls back to "last component is alpha" only for Opacity/Premultiplied.
            Assert.AreEqual(4, GetChannelInfo(data, JpxAlphaMode.None).ColorChannelCount);
            Assert.AreEqual(3, GetChannelInfo(data, JpxAlphaMode.Opacity).ColorChannelCount);
            Assert.AreEqual(3, GetChannelInfo(data, JpxAlphaMode.Premultiplied).ColorChannelCount);
            Assert.AreEqual(4, GetChannelInfo(data, JpxAlphaMode.FromCodestream).ColorChannelCount);
        }

        [Test]
        public void GetChannelInfo_NoneModeNeverReportsAlpha()
        {
            var data = Jp2Container(4, cmapContent: null, cdefContent: null);

            Assert.AreEqual(false, GetChannelInfo(data, JpxAlphaMode.None).HasAlphaChannel);
        }

        [Test]
        public void GetComponentIndices_AppliesDcLevelShiftWithoutNormalization()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);

            // All coefficients are zero, so every sample is the inverse DC level shift 2^(8-1).
            var indices = decoder.GetComponentIndices(0);

            Assert.AreEqual(32 * 16, indices.Length);
            Assert.AreEqual(128, indices[0]);
            Assert.AreEqual(128, indices[indices.Length - 1]);
        }

        [Test]
        public void GetComponentIndices_OutOfRangeThrows()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);

            Assert.Throws<ArgumentOutOfRangeException>(() => decoder.GetComponentIndices(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => decoder.GetComponentIndices(1));
        }

        // A full decode of the ITU-T T.800 (06/2019) Section J.10 example codestream through all decoding stages.
        // The codestream is reversibly coded, so the decode is deterministic and the output must match the
        // samples given in Section J.10.5 exactly.
        [Test]
        public void GetComponentIndices_T800AnnexJ10Example_ExactMatch()
        {
            var data = JpxAnnexJ10.Codestream;

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(data, 0, data.Length);

            var indices = decoder.GetComponentIndices(0);

            Assert.AreEqual(JpxAnnexJ10.ExpectedSamples, indices);
        }

        [Test]
        public void GetComponentSamples_AfterReadImageDataSucceeds()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);
            decoder.SetConstraints(1, JpxAlphaMode.None);
            decoder.ReadImageData();

            var samples = decoder.GetComponentSamples(0);

            Assert.AreEqual(128f, samples[0]);
        }

        [Test]
        public void GetComponentSamples_OutOfRangeThrows()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);

            Assert.Throws<ArgumentOutOfRangeException>(() => decoder.GetComponentSamples(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => decoder.GetComponentSamples(1));
        }

        [Test]
        public void GetComponentSamples_ReducedResolutionUsesNativeGrid()
        {
            // Odd dimensions: the reduced component plane is ceil(33/2) x ceil(17/2) = 17x9.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(33, 17, 0, 0, 33, 17, 0, 0, [7, 1, 1]),
                JpxTestCodestream.Cod(decompositionLevels: 1),
                JpxTestCodestream.Qcd(subBandCount: 4),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);
            decoder.SetConstraints(1, JpxAlphaMode.None, maxResolution: 20);

            Assert.AreEqual(1, decoder.ResolutionReduction);
            Assert.AreEqual(17, decoder.DecodedWidth);
            Assert.AreEqual(9, decoder.DecodedHeight);

            var samples = decoder.GetComponentSamples(0);

            Assert.AreEqual(17 * 9, samples.Length);
            Assert.AreEqual(128f, samples[0]);
            Assert.AreEqual(128f, samples[samples.Length - 1]);
        }

        [Test]
        public void ReadImageData_AfterRawDecodeSucceeds()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);
            decoder.SetConstraints(1, JpxAlphaMode.None);
            decoder.GetComponentSamples(0);

            var image = decoder.ReadImageData();

            Assert.AreEqual(128f / 255f, image.ColorChannels[0][0], 0.000001f);
        }

        [Test]
        public void ReadImageData_BeforeReadMetadataThrows()
        {
            var decoder = new JpxDecoder();
            Assert.Throws<InvalidOperationException>(() => decoder.SetConstraints(1, JpxAlphaMode.None));
            Assert.Throws<InvalidOperationException>(() => decoder.ReadImageData());
        }

        [Test]
        public void ReadImageData_CanceledTokenThrows()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);
            decoder.SetConstraints(1, JpxAlphaMode.None);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() => decoder.ReadImageData(cts.Token));
        }

        [Test]
        public void ReadImageData_ChannelDefinitionDeclaresPremultipliedOpacity()
        {
            var cdef = Jp2ChannelDefinitionEntries(Cdef(0, 0, 1), Cdef(1, 0, 2), Cdef(2, 0, 3), Cdef(3, 2, 0));
            var data = Jp2Container(4, cmapContent: null, cdefContent: cdef);

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(data, 0, data.Length);
            decoder.SetConstraints(3, JpxAlphaMode.Opacity);

            var image = decoder.ReadImageData();

            Assert.IsNotNull(image.AlphaChannel);
        }

        [Test]
        public void ReadImageData_ComponentSampleLimitCountsDecodedPlanes()
        {
            // 7500 * 6000 * 3 = 135,000,000 component samples. Each plane remains below the per-tile-component
            // limit, but the three decoded planes together exceed the aggregate budget, and with no decomposition
            // levels the decode cannot fall back to a reduced resolution.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(7500, 6000, 0, 0, 7500, 6000, 0, 0,
                    [7, 1, 1],
                    [7, 1, 1],
                    [7, 1, 1]),
                JpxTestCodestream.Cod(decompositionLevels: 0),
                JpxTestCodestream.Qcd(subBandCount: 1),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);
            decoder.SetConstraints(3, JpxAlphaMode.None);

            var exception = Assert.Throws<JpxException>(() => decoder.ReadImageData());
            StringAssert.Contains("135000000", exception!.Message);
            StringAssert.Contains(JpxConstraints.MaxComponentSamples.ToString(), exception.Message);
        }

        [Test]
        public void ReadImageData_ComponentSampleLimitExcludesSkippedComponents()
        {
            // Same three-component layout as above, but only the first component is consumed as an output channel.
            // The skipped components get no planes and do not count toward the aggregate sample budget, so the
            // decode succeeds even though all three planes together would exceed it.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(7500, 6000, 0, 0, 7500, 6000, 0, 0,
                    [7, 1, 1],
                    [7, 1, 1],
                    [7, 1, 1]),
                JpxTestCodestream.Cod(decompositionLevels: 0),
                JpxTestCodestream.Qcd(subBandCount: 1),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);
            decoder.SetConstraints(1, JpxAlphaMode.None);

            var data = decoder.ReadImageData();

            Assert.AreEqual(7500, data.Width);
            Assert.AreEqual(6000, data.Height);
            Assert.AreEqual(1, data.ColorChannels.Length);
        }

        [Test]
        public void ReadImageData_DecodesPaletteMappedAlpha()
        {
            // The empty component reconstructs palette index 128. Palette column 0 is greyscale and column 1 is
            // whole-image opacity; cdef describes the channels produced by the two cmap entries.
            var paletteContent = new byte[3 + 2 + 256 * 2];
            paletteContent[0] = 1; // NE = 256
            paletteContent[1] = 0;
            paletteContent[2] = 2; // NPC = 2
            paletteContent[3] = 7;
            paletteContent[4] = 7;
            for (var entry = 0; entry < 256; entry++)
            {
                paletteContent[5 + entry * 2] = 64;
                paletteContent[5 + entry * 2 + 1] = 128;
            }

            var cmap = JpxTestCodestream.Concat(
                Jp2ComponentMappingEntry(0, type: 1, paletteColumn: 0),
                Jp2ComponentMappingEntry(0, type: 1, paletteColumn: 1));
            var cdef = Jp2ChannelDefinitionEntries(Cdef(0, 0, 1), Cdef(1, 1, 0));
            var data = Jp2Container(1, cmap, cdef, paletteContent, enumCs: 17);

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(data, 0, data.Length);
            decoder.SetConstraints(1, JpxAlphaMode.Opacity);

            var image = decoder.ReadImageData();

            Assert.AreEqual(64f / 255f, image.ColorChannels[0][0], 0.000001f);
            Assert.AreEqual(128f / 255f, image.AlphaChannel[0], 0.000001f);
        }

        [Test]
        public void ReadImageData_DuplicateChannelsGetIndependentArrays()
        {
            // A single gray component expanded to three colour channels: every channel must be usable and safe to
            // mutate independently, even though the decoder hands out the decoded plane to the first channel
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);
            decoder.SetConstraints(3, JpxAlphaMode.None);

            var data = decoder.ReadImageData();

            Assert.AreEqual(3, data.ColorChannels.Length);
            Assert.AreNotSame(data.ColorChannels[0], data.ColorChannels[1]);
            Assert.AreNotSame(data.ColorChannels[0], data.ColorChannels[2]);
            Assert.AreNotSame(data.ColorChannels[1], data.ColorChannels[2]);
            Assert.AreEqual(data.ColorChannels[0], data.ColorChannels[1]);
            Assert.AreEqual(data.ColorChannels[0], data.ColorChannels[2]);
            Assert.AreEqual(128f / 255f, data.ColorChannels[0][0], 0.000001f);
        }

        [Test]
        public void ReadImageData_NormalizesComponentsIndependently()
        {
            // Two unsigned components of different precisions. No packet data is decoded, so all
            // coefficients stay zero and every reconstructed sample equals the component's DC
            // level shift (ITU-T T.800 (06/2019) Section G.1.2). Each component must be
            // normalized by its own MaxValue (2^Ssiz - 1) rather than a shared bit depth.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(8, 8, 0, 0, 8, 8, 0, 0,
                    [7, 1, 1],     // 8-bit unsigned
                    [3, 1, 1]),    // 4-bit unsigned
                JpxTestCodestream.Cod(),
                JpxTestCodestream.Qcd(),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);
            decoder.SetConstraints(2, JpxAlphaMode.None);

            var data = decoder.ReadImageData();

            Assert.AreEqual(8, data.Width);
            Assert.AreEqual(8, data.Height);
            Assert.AreEqual(2, data.ColorChannels.Length);
            Assert.IsNull(data.AlphaChannel);

            Assert.AreEqual(64, data.ColorChannels[0].Length);
            Assert.AreEqual(64, data.ColorChannels[1].Length);

            Assert.AreEqual(128f / 255f, data.ColorChannels[0][0], 0.000001f);
            Assert.AreEqual(8f / 15f, data.ColorChannels[1][0], 0.000001f);
            Assert.AreEqual(128f / 255f, data.ColorChannels[0][63], 0.000001f);
            Assert.AreEqual(8f / 15f, data.ColorChannels[1][63], 0.000001f);
        }

        [Test]
        public void ReadImageData_NormalizesSignedComponentRange()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(1, 1, 0, 0, 1, 1, 0, 0,
                    [0x83, 1, 1]), // 4-bit signed
                JpxTestCodestream.Cod(decompositionLevels: 0),
                JpxTestCodestream.Qcd(subBandCount: 1),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);
            decoder.SetConstraints(1, JpxAlphaMode.None);

            // With no packet data the reconstructed signed sample is zero, the middle of [-8, 7].
            var data = decoder.ReadImageData();

            Assert.AreEqual(8f / 15f, data.ColorChannels[0][0], 0.000001f);
        }

        [Test]
        public void ReadImageData_NormalizesSignedComponents()
        {
            // Signed 4-bit components reconstruct 0 from the empty codestream. ITU-T T.800 (06/2019) Section A.5.1:
            // signed p-bit samples span [-2^(p-1), 2^(p-1)-1]; the midpoint translation maps 0 to 8/15.
            var data = Jp2Container(
                3, cmapContent: null, cdefContent: null, enumCs: 18, bitsPerComponent: 0x83);
            var decoder = new JpxDecoder();
            decoder.ReadMetadata(data, 0, data.Length);
            decoder.SetConstraints(3, JpxAlphaMode.None);

            var image = decoder.ReadImageData();

            Assert.AreEqual(8f / 15f, image.ColorChannels[0][0], 0.000001f);
            Assert.AreEqual(8f / 15f, image.ColorChannels[1][0], 0.000001f);
            Assert.AreEqual(8f / 15f, image.ColorChannels[2][0], 0.000001f);
        }

        [TestCase(-4, 4f / 15f)]
        [TestCase(3, 11f / 15f)]
        public void ReadImageData_NormalizesSignedPaletteColumn(int paletteValue, float expected)
        {
            // The empty 8-bit codestream reconstructs index 128. Give all 256 entries the same signed 4-bit value
            // so this test focuses on signed palette normalization rather than Tier-1 packet construction.
            var paletteContent = new byte[3 + 1 + 256];
            paletteContent[0] = 1; // NE = 256
            paletteContent[1] = 0;
            paletteContent[2] = 1; // NPC = 1
            paletteContent[3] = 0x83; // Bi: signed, 4-bit
            for (var i = 4; i < paletteContent.Length; i++)
            {
                paletteContent[i] = (byte)(paletteValue & 0xf);
            }

            var cmap = Jp2ComponentMappingEntry(0, type: 1, paletteColumn: 0);
            var data = Jp2Container(1, cmap, cdefContent: null, paletteContent: paletteContent, enumCs: 17);

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(data, 0, data.Length);
            decoder.SetConstraints(1, JpxAlphaMode.None);
            var image = decoder.ReadImageData();

            // ITU-T T.800 (06/2019) Section I.5.3.4: signed palette columns use two's-complement values. The signed
            // sample interval is translated to the unsigned application range before normalization.
            Assert.AreEqual(expected, image.ColorChannels[0][0], 0.000001f);
        }

        [Test]
        public void ReadImageData_OutputPlaneLimitEnforced()
        {
            // Six 1x1 components, all consumed as colour channels: one output plane more than
            // JpxConstraints.MaxOutputPlanes permits.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(1, 1, 0, 0, 1, 1, 0, 0,
                    [7, 1, 1], [7, 1, 1], [7, 1, 1], [7, 1, 1], [7, 1, 1], [7, 1, 1]),
                JpxTestCodestream.Cod(decompositionLevels: 0),
                JpxTestCodestream.Qcd(subBandCount: 1),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);
            decoder.SetConstraints(6, JpxAlphaMode.None);

            var exception = Assert.Throws<JpxException>(() => decoder.ReadImageData());
            StringAssert.Contains("6 component planes", exception!.Message);
            StringAssert.Contains("5", exception.Message);
        }

        [Test]
        public void ReadImageData_ReducedResolutionMultiTile()
        {
            // 32x16 image of four 16x8 tiles, reduced one level to 16x8: each tile writes an 8x4 area of the
            // reduced planes through the scratch-buffer path.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(32, 16, 0, 0, 16, 8, 0, 0, [7, 1, 1]),
                JpxTestCodestream.Cod(decompositionLevels: 1),
                JpxTestCodestream.Qcd(subBandCount: 4),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.TilePart(1, 0, 1, [], []),
                JpxTestCodestream.TilePart(2, 0, 1, [], []),
                JpxTestCodestream.TilePart(3, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);
            decoder.SetConstraints(1, JpxAlphaMode.None, maxResolution: 8);

            Assert.AreEqual(1, decoder.ResolutionReduction);

            var data = decoder.ReadImageData();

            Assert.AreEqual(16, data.Width);
            Assert.AreEqual(8, data.Height);
            Assert.AreEqual(128, data.ColorChannels[0].Length);

            for (var i = 0; i < data.ColorChannels[0].Length; i++)
            {
                Assert.AreEqual(128f / 255f, data.ColorChannels[0][i], 0.000001f, "Sample " + i);
            }
        }

        [Test]
        public void ReadImageData_ReducedResolutionSubsampledComponent()
        {
            // The 2x2 subsampled second component has a 8x4 reduced plane that is replicated to the 16x8 reduced
            // image grid.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(32, 16, 0, 0, 32, 16, 0, 0,
                    [7, 1, 1],
                    [7, 2, 2]),
                JpxTestCodestream.Cod(decompositionLevels: 1),
                JpxTestCodestream.Qcd(subBandCount: 4),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);
            decoder.SetConstraints(2, JpxAlphaMode.None, maxResolution: 8);

            Assert.AreEqual(1, decoder.ResolutionReduction);

            var data = decoder.ReadImageData();

            Assert.AreEqual(16, data.Width);
            Assert.AreEqual(8, data.Height);
            Assert.AreEqual(128, data.ColorChannels[0].Length);
            Assert.AreEqual(128, data.ColorChannels[1].Length);

            for (var i = 0; i < data.ColorChannels[1].Length; i++)
            {
                Assert.AreEqual(128f / 255f, data.ColorChannels[1][i], 0.000001f, "Sample " + i);
            }
        }

        [Test]
        public void ReadImageData_SecondCallReturnsIndependentData()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);
            decoder.SetConstraints(1, JpxAlphaMode.None);

            var first = decoder.ReadImageData();
            first.ColorChannels[0][0] = 0f;

            var second = decoder.ReadImageData();

            Assert.AreNotSame(first.ColorChannels[0], second.ColorChannels[0]);
            Assert.AreEqual(128f / 255f, second.ColorChannels[0][0], 0.000001f);
        }

        [Test]
        public void ReadImageData_SkipsUnusedComponents()
        {
            // Four components consumed as three colour channels without alpha: the fourth component is skipped by
            // the decode entirely and must not affect the produced channels
            var data = Jp2Container(4, cmapContent: null, cdefContent: null);

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(data, 0, data.Length);
            decoder.SetConstraints(3, JpxAlphaMode.None);

            var image = decoder.ReadImageData();

            Assert.AreEqual(3, image.ColorChannels.Length);
            Assert.IsNull(image.AlphaChannel);
            Assert.AreEqual(128f / 255f, image.ColorChannels[0][0], 0.000001f);
            Assert.AreEqual(128f / 255f, image.ColorChannels[2][0], 0.000001f);
        }

        [Test]
        public void ReadMetadata_InvalidCodestream()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Cod()); // SIZ must immediately follow SOC

            var decoder = new JpxDecoder();
            Assert.Throws<JpxException>(() => decoder.ReadMetadata(codestream, 0, codestream.Length));
        }

        [Test]
        public void ReadMetadata_MctDisabled()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 1, [], [1]),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);

            Assert.AreEqual(32, decoder.DecodedWidth);
            Assert.AreEqual(16, decoder.DecodedHeight);
            Assert.AreEqual(1, decoder.Components.Length);
            Assert.AreEqual(false, decoder.MultipleComponentTransformation);
        }

        [Test]
        public void ReadMetadata_PopulatesSizMetadata()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(100, 50, 10, 5, 90, 45, 0, 0,
                    [7, 1, 1],     // 8-bit unsigned
                    [0x8B, 1, 1],  // 12-bit signed
                    [4, 2, 2]),    // 5-bit unsigned, 2x2 sub-sampling
                JpxTestCodestream.Cod(layers: 2, mct: true),
                JpxTestCodestream.Qcd(),
                JpxTestCodestream.TilePart(0, 0, 1, [], [1, 2, 3]),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);

            Assert.AreEqual(90, decoder.DecodedWidth);
            Assert.AreEqual(45, decoder.DecodedHeight);
            Assert.AreEqual(3, decoder.Components.Length);
            Assert.AreEqual(new[] { 8, 12, 5 }, decoder.Precisions);
            Assert.AreEqual(new[] { false, true, false }, decoder.Signed);
            Assert.AreEqual(true, decoder.MultipleComponentTransformation);
            Assert.AreEqual(true, decoder.ReversibleFilter);

            // Raw codestreams carry no JP2 header boxes.
            Assert.AreEqual(JpxEnumeratedColorSpace.Unknown, decoder.EnumeratedColorSpace);
            Assert.IsNull(decoder.Palette);
            Assert.AreEqual(0, decoder.ComponentMappings.Length);
            Assert.AreEqual(0, decoder.ChannelDefinitions.Length);
        }

        [Test]
        public void ReadMetadata_PrecisionAbove31Rejected()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(1, 1, 0, 0, 1, 1, 0, 0,
                    [31, 1, 1]), // 32-bit unsigned
                JpxTestCodestream.Cod(decompositionLevels: 0),
                JpxTestCodestream.Qcd(subBandCount: 1),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            var exception = Assert.Throws<JpxException>(() => decoder.ReadMetadata(codestream, 0, codestream.Length));

            StringAssert.Contains("32 bits", exception!.Message);
            StringAssert.Contains("31 bits", exception.Message);
        }

        [Test]
        public void ReadMetadata_T800AnnexJ10Example()
        {
            // The main header fields of the ITU-T T.800 (06/2019) Section J.10 example codestream, as decoded in
            // Section J.10.1.
            var data = JpxAnnexJ10.Codestream;

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(data, 0, data.Length);

            Assert.AreEqual(1, decoder.DecodedWidth);
            Assert.AreEqual(9, decoder.DecodedHeight);
            Assert.AreEqual(1, decoder.Components.Length);
            Assert.AreEqual(new[] { 8 }, decoder.Precisions);
            Assert.AreEqual(new[] { false }, decoder.Signed);
            Assert.AreEqual(false, decoder.MultipleComponentTransformation);
            Assert.AreEqual(true, decoder.ReversibleFilter);
            Assert.AreEqual(1, decoder.Components[0].XRsizi);
            Assert.AreEqual(1, decoder.Components[0].YRsizi);
        }

        [Test]
        public void SetConstraints_AfterDecodeThrows()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);
            decoder.GetComponentIndices(0);

            Assert.Throws<InvalidOperationException>(() => decoder.SetConstraints(1, JpxAlphaMode.None));
        }

        [Test]
        public void SetConstraints_MaxResolutionOutOfRangeThrows()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                decoder.SetConstraints(1, JpxAlphaMode.None, maxResolution: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                decoder.SetConstraints(1, JpxAlphaMode.None, maxResolution: JpxConstraints.MaxResolution + 1));

            // The inclusive bounds are accepted
            decoder.SetConstraints(1, JpxAlphaMode.None, maxResolution: JpxConstraints.MaxResolution);
        }

        [Test]
        public void SetConstraints_ReducesResolutionToMaxResolution()
        {
            // 32x16 image with two decomposition levels: a max resolution of 8 discards both, decoding at 8x4.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(32, 16, 0, 0, 32, 16, 0, 0, [7, 1, 1]),
                JpxTestCodestream.Cod(decompositionLevels: 2),
                JpxTestCodestream.Qcd(subBandCount: 7),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);
            decoder.SetConstraints(1, JpxAlphaMode.None, maxResolution: 8);

            Assert.AreEqual(2, decoder.ResolutionReduction);
            Assert.AreEqual(8, decoder.DecodedWidth);
            Assert.AreEqual(4, decoder.DecodedHeight);

            // With no packet data every reconstructed sample is the inverse DC level shift, at any resolution.
            var data = decoder.ReadImageData();

            Assert.AreEqual(8, data.Width);
            Assert.AreEqual(4, data.Height);
            Assert.AreEqual(32, data.ColorChannels[0].Length);
            Assert.AreEqual(128f / 255f, data.ColorChannels[0][0], 0.000001f);
            Assert.AreEqual(128f / 255f, data.ColorChannels[0][31], 0.000001f);
        }

        [Test]
        public void SetConstraints_ReductionLimitedByDecompositionLevels()
        {
            // A max resolution of 4 would need two reductions, but the single decomposition level only allows one.
            // The decode falls back to the lowest available resolution instead of rejecting the image.
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(32, 16, 0, 0, 32, 16, 0, 0, [7, 1, 1]),
                JpxTestCodestream.Cod(decompositionLevels: 1),
                JpxTestCodestream.Qcd(subBandCount: 4),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);
            decoder.SetConstraints(1, JpxAlphaMode.None, maxResolution: 4);

            Assert.AreEqual(1, decoder.ResolutionReduction);
            Assert.AreEqual(16, decoder.DecodedWidth);
            Assert.AreEqual(8, decoder.DecodedHeight);
        }

        [Test]
        public void SetConstraints_ZeroComponentsPerSampleThrows()
        {
            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.MainHeader(),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var decoder = new JpxDecoder();
            decoder.ReadMetadata(codestream, 0, codestream.Length);

            Assert.Throws<ArgumentOutOfRangeException>(() => decoder.SetConstraints(0, JpxAlphaMode.None));
        }

        // ITU-T T.800 (06/2019) Section I.4: box = LBox (4) + TBox (4) + content.
        private static byte[] Jp2Box(string type, byte[] content)
        {
            var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
            var box = new byte[8 + content.Length];
            var length = box.Length;

            box[0] = (byte)(length >> 24);
            box[1] = (byte)(length >> 16);
            box[2] = (byte)(length >> 8);
            box[3] = (byte)length;
            Array.Copy(typeBytes, 0, box, 4, 4);
            Array.Copy(content, 0, box, 8, content.Length);

            return box;
        }

        private static byte[] Jp2SignatureBox() => Jp2Box("jP  ", [0x0d, 0x0a, 0x87, 0x0a]);

        private static byte[] Jp2FileTypeBox() => Jp2Box("ftyp", JpxTestCodestream.Concat(
            [(byte)'j', (byte)'p', (byte)'2', (byte)' '],
            [0, 0, 0, 0],
            [(byte)'j', (byte)'p', (byte)'2', (byte)' ']));

        private static byte[] Jp2ImageHeaderBox(int width, int height, int numComponents, int bitsPerComponent = 7)
        {
            var content = new byte[14]
            {
                (byte)(height >> 24),
                (byte)(height >> 16),
                (byte)(height >> 8),
                (byte)height,
                (byte)(width >> 24),
                (byte)(width >> 16),
                (byte)(width >> 8),
                (byte)width,
                0,
                (byte)numComponents,
                (byte)bitsPerComponent,
                7, // Compression type (fixed value per I.5.3.1)
                0, // Unknown colour space
                0, // IPR
            };
            return Jp2Box("ihdr", content);
        }

        private static byte[] Jp2ColourSpecificationBox(int enumCs = 16 /* sRGB */)
        {
            var content = new byte[7];
            content[0] = 1; // METH = enumerated colour space
            content[3] = (byte)(enumCs >> 24);
            content[4] = (byte)(enumCs >> 16);
            content[5] = (byte)(enumCs >> 8);
            content[6] = (byte)enumCs;
            return Jp2Box("colr", content);
        }

        private static byte[] Jp2CodestreamBox(byte[] codestream) => Jp2Box("jp2c", codestream);

        /// <summary>cmap entry: component index (2), mapping type (1), palette column (1).</summary>
        private static byte[] Jp2ComponentMappingEntry(int componentIndex, int type = 0, int paletteColumn = 0) =>
            [
                (byte)(componentIndex >> 8), (byte)componentIndex,
                (byte)type,
                (byte)paletteColumn,
            ];

        /// <summary>One cdef entry: channel index, channel type, association.</summary>
        private static int[] Cdef(int channelIndex, int type, int association) =>
            [channelIndex, type, association];

        /// <summary>cdef entry set: count (2) then per entry channel index/type/association (2 each).</summary>
        private static byte[] Jp2ChannelDefinitionEntries(params int[][] entries)
        {
            var content = new byte[2 + entries.Length * 6];
            content[0] = (byte)(entries.Length >> 8);
            content[1] = (byte)entries.Length;

            for (var i = 0; i < entries.Length; i++)
            {
                var offset = 2 + i * 6;
                var channelIndex = entries[i][0];
                var type = entries[i][1];
                var association = entries[i][2];

                content[offset + 0] = (byte)(channelIndex >> 8);
                content[offset + 1] = (byte)channelIndex;
                content[offset + 2] = (byte)(type >> 8);
                content[offset + 3] = (byte)type;
                content[offset + 4] = (byte)(association >> 8);
                content[offset + 5] = (byte)association;
            }

            return content;
        }

        /// <summary>
        /// A minimal JP2 container wrapping a raw codestream with <paramref name="componentCount"/>
        /// 8-bit unsigned components, all in one tile, optionally with cmap/cdef boxes.
        /// </summary>
        private static byte[] Jp2Container(
            int componentCount, byte[] cmapContent, byte[] cdefContent, byte[] paletteContent = null, int enumCs = 16,
            int bitsPerComponent = 7)
        {
            var components = new byte[componentCount][];
            for (var i = 0; i < componentCount; i++)
            {
                components[i] = [(byte)bitsPerComponent, 1, 1];
            }

            var codestream = JpxTestCodestream.Concat(
                JpxTestCodestream.Soc(),
                JpxTestCodestream.Siz(4, 4, 0, 0, 4, 4, 0, 0, components),
                JpxTestCodestream.Cod(),
                JpxTestCodestream.Qcd(),
                JpxTestCodestream.TilePart(0, 0, 1, [], []),
                JpxTestCodestream.Eoc());

            var jp2hChildren = new System.Collections.Generic.List<byte[]> { Jp2ColourSpecificationBox(enumCs) };
            if (paletteContent != null)
            {
                jp2hChildren.Add(Jp2Box("pclr", paletteContent));
            }
            if (cmapContent != null) jp2hChildren.Add(Jp2Box("cmap", cmapContent));
            if (cdefContent != null) jp2hChildren.Add(Jp2Box("cdef", cdefContent));

            return JpxTestCodestream.Concat(
                Jp2SignatureBox(),
                Jp2FileTypeBox(),
                Jp2Box("jp2h", JpxTestCodestream.Concat(
                    new[] { Jp2ImageHeaderBox(4, 4, componentCount, bitsPerComponent) }
                        .Concat(jp2hChildren).ToArray())),
                Jp2CodestreamBox(codestream));
        }

        private static JpxChannelInfo GetChannelInfo(byte[] data, JpxAlphaMode alphaMode)
        {
            var decoder = new JpxDecoder();
            decoder.ReadMetadata(data, 0, data.Length);
            return decoder.GetChannelInfo(alphaMode);
        }
    }
}
