// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Imaging.Jpx.Container;
using PdfToSvg.Imaging.Jpx.Decoding;
using PdfToSvg.Imaging.Jpx.ImageModel;
using System;
using System.Threading;

namespace PdfToSvg.Imaging.Jpx
{
    /// <summary>
    /// Entry point to the JPEG 2000 decoder.
    /// </summary>
    internal class JpxDecoder
    {
        private JpxImageReader? reader;
        private JpxImageDecoder? decoder;
        private JpxChannelMap? channelMap;

        private JpxImageReader Reader => reader ??
            throw new InvalidOperationException(
                nameof(ReadMetadata) + " must be called before the JPEG 2000 image is accessed");

        /// <summary>Full resolution image width.</summary>
        public int NativeWidth => Reader.NativeWidth;

        /// <summary>Full resolution image height.</summary>
        public int NativeHeight => Reader.NativeHeight;

        /// <summary>Resulting image width after resolution reduction.</summary>
        public int DecodedWidth { get; private set; }

        /// <summary>Resulting image height after resolution reduction.</summary>
        public int DecodedHeight { get; private set; }

        /// <summary>
        /// Number of highest resolution levels discarded by the decode. 0 decodes the full resolution.
        /// </summary>
        public int ResolutionReduction { get; private set; }

        public JpxComponent[] Components => Reader.Image.Components;
        public bool MultipleComponentTransformation => Reader.CodingStyleDefaults.MultipleComponentTransformation;
        public bool ReversibleFilter => Reader.CodingStyleDefaults.ReversibleFilter;
        public JpxEnumeratedColorSpace EnumeratedColorSpace => Reader.Image.EnumeratedColorSpace;
        public JpxChannelDefinitionBox[] ChannelDefinitions => Reader.Image.ChannelDefinitions;
        public JpxPaletteBox? Palette => Reader.Image.Palette;
        public JpxComponentMappingBox[] ComponentMappings => Reader.Image.ComponentMappings;

        /// <summary>Bit depth per codestream component, from the SIZ Ssiz fields.</summary>
        public int[] Precisions
        {
            get
            {
                var components = Reader.Image.Components;
                var result = new int[components.Length];
                for (var i = 0; i < result.Length; i++)
                {
                    result[i] = components[i].Precision;
                }
                return result;
            }
        }

        /// <summary>Signedness per codestream component, from the SIZ Ssiz fields.</summary>
        public bool[] Signed
        {
            get
            {
                var components = Reader.Image.Components;
                var result = new bool[components.Length];
                for (var i = 0; i < result.Length; i++)
                {
                    result[i] = components[i].Signed;
                }
                return result;
            }
        }

        public JpxChannelInfo GetChannelInfo(JpxAlphaMode alphaMode)
        {
            return Reader.GetChannelInfo(alphaMode);
        }

        /// <summary>
        /// Read minimal metadata for <see cref="JpxImage"/> to be able to determine a target file format.
        /// </summary>
        public void ReadMetadata(byte[] data, int offset, int count)
        {
            this.reader = JpxImageReader.Create(data, offset, count);
            this.DecodedWidth = reader.NativeWidth;
            this.DecodedHeight = reader.NativeHeight;
        }

        /// <summary>
        /// Configures how <see cref="ReadImageData"/> maps codestream components to color and alpha channels.
        /// Must be called after <see cref="ReadMetadata"/> (the channel resolution depends on the JP2 header
        /// boxes), and before the image is decoded. Components not consumed by any channel are skipped by the
        /// decode entirely.
        /// </summary>
        /// <param name="componentsPerSample">
        /// Authoritative number of colour channels, supplied by the caller from the effective colour space's
        /// ComponentsPerSample (1 = gray, 3 = rgb, 4 = cmyk, ...).
        /// </param>
        /// <param name="alphaMode">How embedded opacity should be treated.</param>
        /// <param name="maxResolution">
        /// Max width/height of the decoded image. Larger images are decoded at a lower resolution by discarding the
        /// highest resolution levels (ITU-T T.800 (06/2019) Section B.5). The limit is best-effort: it cannot cut
        /// deeper than the decomposition levels signalled in the main header allow.
        /// </param>
        public void SetConstraints(int componentsPerSample, JpxAlphaMode alphaMode,
            int maxResolution = JpxConstraints.DefaultMaxDecodeResolution)
        {
            if (componentsPerSample < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(componentsPerSample));
            }

            if (maxResolution < 1 || maxResolution > JpxConstraints.MaxResolution)
            {
                throw new ArgumentOutOfRangeException(nameof(maxResolution),
                    "The max decode resolution must be between 1 and " + JpxConstraints.MaxResolution);
            }

            if (reader == null)
            {
                throw new InvalidOperationException("ReadMetadata must be called before the constraints are set");
            }

            if (decoder != null)
            {
                throw new InvalidOperationException("The constraints must be set before the image is decoded");
            }

            var image = reader.Image;

            this.ResolutionReduction = Reader.ComputeReduction(maxResolution);
            this.channelMap = JpxChannelMap.Resolve(image, componentsPerSample, alphaMode);
            this.DecodedWidth = JpxResolutionReducer.GetReducedResolution(image.Xsiz, image.XOsiz, this.ResolutionReduction);
            this.DecodedHeight = JpxResolutionReducer.GetReducedResolution(image.Ysiz, image.YOsiz, this.ResolutionReduction);
        }

        private JpxImageDecoder ReadImage(CancellationToken cancellationToken)
        {
            return decoder ??= Reader.ReadImage(ResolutionReduction, cancellationToken);
        }

        /// <summary>
        /// Returns reconstructed samples for one component at its native sample grid (reduced when a resolution
        /// reduction applies), before normalization, clamping, palette mapping, or resampling to the image grid.
        /// Intended for unit tests.
        /// </summary>
        public float[] GetComponentSamples(int componentIndex, CancellationToken cancellationToken = default)
        {
            if (componentIndex < 0 || componentIndex >= Components.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(componentIndex));
            }

            return ReadImage(cancellationToken).DecodeComponentSamples(componentIndex, cancellationToken);
        }

        /// <summary>
        /// Returns one palette index value per pixel (Width*Height bytes), resampled to the image pixel grid. Applies
        /// entropy decoding, inverse quantization, inverse DWT and inverse DC level shift, but no MCT, palette lookup
        /// or normalization. Intended for indexed PNG output.
        /// </summary>
        public byte[] GetComponentIndices(int componentIndex, CancellationToken cancellationToken = default)
        {
            if (componentIndex < 0 || componentIndex >= Components.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(componentIndex));
            }

            return ReadImage(cancellationToken)
                .DecodePaletteImage(componentIndex, cancellationToken)
                .GetComponentIndices();
        }

        /// <summary>
        /// Decodes the image into the planar colour channels configured by <see cref="SetConstraints"/> (normalized
        /// to [0, 1]) plus an optional separate straight-alpha plane.
        /// </summary>
        public JpxImageData ReadImageData(CancellationToken cancellationToken = default)
        {
            var channelMap = this.channelMap ??
                throw new InvalidOperationException("SetConstraints must be called before the image data is read");

            return ReadImage(cancellationToken)
                .DecodeTrueColorImage(channelMap, cancellationToken)
                .GetImageData();
        }
    }
}
