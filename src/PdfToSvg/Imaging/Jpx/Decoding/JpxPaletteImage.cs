// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.ImageModel;
using System;

namespace PdfToSvg.Imaging.Jpx.Decoding
{
    /// <summary>
    /// An indexed image: one raw-decoded index component, materialized as palette index values on the image pixel
    /// grid. The index values are meaningful only together with a palette (the JP2 palette box or a PDF /Indexed
    /// color space).
    /// </summary>
    internal sealed class JpxPaletteImage : JpxDecodedImage
    {
        private readonly int indexComponentIndex;

        public JpxPaletteImage(JpxImageInfo image, float[]?[] componentPlanes,
            JpxComponentUsage[] componentUsages, int resolutionReduction, int indexComponentIndex)
            : base(image, componentPlanes, componentUsages, resolutionReduction)
        {
            this.indexComponentIndex = indexComponentIndex;
        }

        /// <summary>
        /// Returns one palette index value per pixel (Width*Height bytes), resampled to the image pixel grid. The
        /// decode applied entropy decoding, inverse quantization, inverse DWT and inverse DC level shift, but no
        /// MCT, palette lookup or normalization. Intended for indexed PNG output.
        /// </summary>
        public byte[] GetComponentIndices()
        {
            var samples = GetImageGridPlane(indexComponentIndex).Samples;
            var maxValue = Math.Min(image.Components[indexComponentIndex].MaxValue, 255);

            var result = new byte[samples.Length];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = (byte)MathUtils.Clamp((int)(samples[i] + 0.5f), 0, maxValue);
            }

            return result;
        }
    }
}
