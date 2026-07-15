// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Imaging.Jpx.Container;
using PdfToSvg.Imaging.Jpx.ImageModel;
using System;

namespace PdfToSvg.Imaging.Jpx.Decoding
{
    /// <summary>
    /// Specifies, based on JP2 header boxes and PDF constraints, how channels will be mapped in the decoded image.
    /// </summary>
    internal sealed class JpxChannelMap
    {
        private JpxChannelMap(int componentsPerSample, JpxAlphaMode alphaMode,
            JpxComponentMappingBox[] colorMappings, int alphaComponentIndex, bool premultipliedAlpha)
        {
            ComponentsPerSample = componentsPerSample;
            AlphaMode = alphaMode;
            ColorMappings = colorMappings;
            AlphaComponentIndex = alphaComponentIndex;
            PremultipliedAlpha = premultipliedAlpha;
        }

        /// <summary>
        /// Authoritative number of colour channels, supplied by the caller from the effective colour space's
        /// ComponentsPerSample (1 = gray, 3 = rgb, 4 = cmyk, ...).
        /// </summary>
        public int ComponentsPerSample { get; }

        /// <summary>
        /// By caller desired alpha mode.
        /// </summary>
        public JpxAlphaMode AlphaMode { get; }

        /// <summary>
        /// One component mapping per output color channel.
        /// </summary>
        public JpxComponentMappingBox[] ColorMappings { get; }

        /// <summary>
        /// Codestream component backing the alpha channel, or -1 when there is none.
        /// </summary>
        public int AlphaComponentIndex { get; }

        /// <summary>
        /// Whether the colour channels are premultiplied by the alpha channel in the codestream.
        /// </summary>
        public bool PremultipliedAlpha { get; }

        /// <summary>
        /// Whether the decode will emit a separate alpha channel.
        /// </summary>
        public bool HasAlphaChannel => AlphaMode != JpxAlphaMode.None && AlphaComponentIndex >= 0;

        // ITU-T T.800 (06/2019) Sections I.5.3.5 (Component Mapping) and I.5.3.6 (Channel Definition) determine how
        // codestream components map to colour and opacity channels
        public static JpxChannelMap Resolve(JpxImageInfo image, int componentsPerSample, JpxAlphaMode alphaMode)
        {
            var colorMappings = new JpxComponentMappingBox[componentsPerSample];
            var alphaComponentIndex = -1;
            var premultiplied = false;

            var mappings = image.ComponentMappings;
            if (mappings.Length == 0)
            {
                // No Component Mapping box: each codestream component maps directly.
                mappings = new JpxComponentMappingBox[image.Components.Length];
                for (var i = 0; i < mappings.Length; i++)
                {
                    mappings[i] = new JpxComponentMappingBox { ComponentIndex = i };
                }
            }

            if (image.ChannelDefinitions.Length > 0)
            {
                // Association (1-indexed) orders colour channels;
                const int FirstAssociationValue = 1; // (e.g. R in sRGB, gray in greyscale)
                foreach (var definition in image.ChannelDefinitions)
                {
                    if (definition.ChannelIndex < 0 || definition.ChannelIndex >= mappings.Length)
                    {
                        continue;
                    }

                    if (definition.Type == JpxChannelType.Color)
                    {
                        var colorMappingIndex = definition.Association - FirstAssociationValue;
                        if (colorMappingIndex >= 0 && colorMappingIndex < componentsPerSample)
                        {
                            colorMappings[colorMappingIndex] = mappings[definition.ChannelIndex];
                        }
                    }
                    else if (
                        definition.Type == JpxChannelType.Opacity ||
                        definition.Type == JpxChannelType.PremultipliedOpacity)
                    {
                        if (alphaComponentIndex < 0)
                        {
                            alphaComponentIndex = mappings[definition.ChannelIndex].ComponentIndex;
                            premultiplied = definition.Type == JpxChannelType.PremultipliedOpacity;
                        }
                    }
                }
            }

            // Fill any colour slot the channel definitions did not assign.
            for (var i = 0; i < componentsPerSample; i++)
            {
                colorMappings[i] ??= mappings[Math.Min(i, mappings.Length - 1)];
            }

            // SMaskInData = 1/2 assert the mask exists, so fall back to a trailing extra component when no opacity
            // channel was declared. FromCodestream trusts the channel definition box only.
            if ((alphaMode == JpxAlphaMode.Opacity || alphaMode == JpxAlphaMode.Premultiplied) &&
                alphaComponentIndex < 0 &&
                mappings.Length > componentsPerSample)
            {
                alphaComponentIndex = mappings[componentsPerSample].ComponentIndex;
            }

            if (alphaComponentIndex < 0 || alphaComponentIndex >= image.Components.Length)
            {
                alphaComponentIndex = -1;
            }

            return new JpxChannelMap(componentsPerSample, alphaMode, colorMappings, alphaComponentIndex, premultiplied);
        }

        /// <summary>
        /// Decides how each codestream component is consumed by a channel-mode decode. Components not consumed by
        /// any color or alpha channel are skipped.
        /// </summary>
        public JpxComponentUsage[] CreateComponentUsages(JpxImageInfo image)
        {
            var usages = new JpxComponentUsage[image.Components.Length];

            foreach (var mapping in ColorMappings)
            {
                var usage = mapping.Type == JpxComponentMappingType.Palette && image.Palette != null
                    ? JpxComponentUsage.Raw
                    : JpxComponentUsage.Normalized;

                MergeUsage(usages, mapping.ComponentIndex, usage);
            }

            if (HasAlphaChannel)
            {
                MergeUsage(usages, AlphaComponentIndex, JpxComponentUsage.Normalized);
            }

            return usages;
        }

        private static void MergeUsage(JpxComponentUsage[] usages, int componentIndex, JpxComponentUsage usage)
        {
            if (componentIndex < 0 || componentIndex >= usages.Length)
            {
                // Mappings referring to missing components are reported when the channel is materialized
                return;
            }

            // Raw samples can still be normalized during channel materialization, but not the other way around, so
            // raw wins when a component is consumed both as palette indices and as a normalized channel
            if (usages[componentIndex] == JpxComponentUsage.Skipped || usage == JpxComponentUsage.Raw)
            {
                usages[componentIndex] = usage;
            }
        }
    }
}
