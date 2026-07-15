// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.Container;
using PdfToSvg.Imaging.Jpx.ImageModel;
using System;

namespace PdfToSvg.Imaging.Jpx.Decoding
{
    /// <summary>
    /// A truecolor image: the decoded components materialized into the planar colour and alpha channels configured
    /// by a <see cref="JpxChannelMap"/>.
    /// </summary>
    internal sealed class JpxTrueColorImage : JpxDecodedImage
    {
        private readonly JpxChannelMap channelMap;

        public JpxTrueColorImage(JpxImageInfo image, float[]?[] componentPlanes,
            JpxComponentUsage[] componentUsages, int resolutionReduction, JpxChannelMap channelMap)
            : base(image, componentPlanes, componentUsages, resolutionReduction)
        {
            this.channelMap = channelMap;
        }

        /// <summary>
        /// Materializes the decoded components into planar colour channels (normalized to [0, 1]) plus an optional
        /// separate straight-alpha plane.
        /// </summary>
        public JpxImageData GetImageData()
        {
            var colorMappings = channelMap.ColorMappings;
            var handedOutPlanes = new bool[image.Components.Length];

            var colorChannels = new float[channelMap.ComponentsPerSample][];
            for (var i = 0; i < colorChannels.Length; i++)
            {
                colorChannels[i] = GetChannel(handedOutPlanes, colorMappings[i]);
            }

            float[]? alphaChannel = null;

            if (channelMap.HasAlphaChannel)
            {
                alphaChannel = GetDirectChannel(handedOutPlanes, channelMap.AlphaComponentIndex);

                if (channelMap.PremultipliedAlpha || channelMap.AlphaMode == JpxAlphaMode.Premultiplied)
                {
                    UnPremultiplyAlpha(colorChannels, alphaChannel,
                        image.Components[channelMap.AlphaComponentIndex].MaxValue);
                }
            }

            return new JpxImageData(DecodedWidth, DecodedHeight, colorChannels, alphaChannel);
        }

        private float[] GetChannel(bool[] handedOutPlanes, JpxComponentMappingBox mapping)
        {
            if (mapping.Type == JpxComponentMappingType.Palette && image.Palette != null)
            {
                return GetPaletteChannel(mapping, image.Palette);
            }
            else
            {
                return GetDirectChannel(handedOutPlanes, mapping.ComponentIndex);
            }
        }

        /// <summary>
        /// Returns a channel of normalized samples backed by one component, owned by the caller. The component
        /// plane is handed out directly when possible; a component consumed by multiple channels gets copies for
        /// the subsequent channels.
        /// </summary>
        private float[] GetDirectChannel(bool[] handedOutPlanes, int componentIndex)
        {
            var gridPlane = GetImageGridPlane(componentIndex);

            if (componentUsages[componentIndex] == JpxComponentUsage.Normalized)
            {
                if (!gridPlane.Shared)
                {
                    return gridPlane.Samples;
                }

                if (!handedOutPlanes[componentIndex])
                {
                    handedOutPlanes[componentIndex] = true;
                    return gridPlane.Samples;
                }

                return (float[])gridPlane.Samples.Clone();
            }

            // The component was decoded raw because it is also consumed as palette indices. Normalize the raw
            // samples into a channel of their own. ITU-T T.800 (06/2019) Section A.5.1: signed p-bit component
            // samples span [-2^(p-1), 2^(p-1)-1]; translate that interval to the unsigned application range
            // (unsigned components are already DC level shifted) before normalizing to [0, 1].
            var component = image.Components[componentIndex];
            var offset = component.Signed ? 1 << (component.Precision - 1) : 0;
            var scale = 1f / component.MaxValue;

            var output = gridPlane.Shared
                ? new float[gridPlane.Samples.Length]
                : gridPlane.Samples;

            for (var i = 0; i < gridPlane.Samples.Length; i++)
            {
                output[i] = MathUtils.Clamp((gridPlane.Samples[i] + offset) * scale, 0f, 1f);
            }

            return output;
        }

        private float[] GetPaletteChannel(JpxComponentMappingBox mapping, JpxPaletteBox palette)
        {
            // ITU-T T.800 (06/2019) Section I.5.3.5
            if (mapping.PaletteColumnIndex < 0 || mapping.PaletteColumnIndex >= palette.ColumnCount)
            {
                throw new JpxException("A JPEG 2000 component mapping refers to a missing palette column");
            }

            var gridPlane = GetImageGridPlane(mapping.ComponentIndex);

            var entryCount = palette.EntryCount;
            if (entryCount < 1)
            {
                // Nothing to look up; keep the raw samples.
                return gridPlane.Shared
                    ? (float[])gridPlane.Samples.Clone()
                    : gridPlane.Samples;
            }

            var columnIndex = mapping.PaletteColumnIndex;
            ref var column = ref palette.Columns[columnIndex];
            var columnMaxValue = column.MaxValue;
            var scale = columnMaxValue > 0 ? 1f / columnMaxValue : 0f;
            var offset = column.Signed ? 1 << (column.Precision - 1) : 0;

            // The same component can back several channels through different palette columns, so the lookup writes
            // to a channel of its own instead of transforming the shared plane
            var output = gridPlane.Shared
                ? new float[gridPlane.Samples.Length]
                : gridPlane.Samples;

            for (var i = 0; i < gridPlane.Samples.Length; i++)
            {
                var entry = MathUtils.Clamp((int)(gridPlane.Samples[i] + 0.5f), 0, entryCount - 1);
                // ITU-T T.800 (06/2019) Section I.5.3.4: signed palette columns use two's-complement values.
                // Translate their signed sample interval to the unsigned application range before normalization.
                var value = (palette.Values[entry, columnIndex] + offset) * scale;
                output[i] = MathUtils.Clamp(value, 0f, 1f);
            }

            return output;
        }

        private static void UnPremultiplyAlpha(float[][] colorChannels, float[] alpha, int alphaMaxValue)
        {
            var zeroThreshold = 0.5f / alphaMaxValue;

            foreach (var channel in colorChannels)
            {
                for (var i = 0; i < channel.Length; i++)
                {
                    var a = alpha[i];
                    if (a < zeroThreshold)
                    {
                        channel[i] = 0f;
                    }
                    else if (a < 1f)
                    {
                        var value = channel[i] / a;
                        channel[i] = value > 1f ? 1f : value;
                    }
                }
            }
        }
    }
}
