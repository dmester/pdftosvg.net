// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.Codestream;
using PdfToSvg.Imaging.Jpx.ImageModel;
using System;

namespace PdfToSvg.Imaging.Jpx.Decoding
{
    /// <summary>
    /// Resolution reduction math. A decode may discard the highest resolution levels of the wavelet
    /// decomposition, reconstructing the image at half the resolution per discarded level
    /// (ITU-T T.800 (06/2019) Section B.5).
    /// </summary>
    internal static class JpxResolutionReducer
    {
        /// <summary>
        /// Reduced extent of a full-resolution range starting at <paramref name="offset"/>.
        /// </summary>
        /// <remarks>
        /// See ITU-T T.800 (06/2019) equation B-14
        /// </remarks>
        public static int GetReducedResolution(int originalResolution, int offset, int reduction) =>
            MathUtils.CeilDivPow2(originalResolution, reduction) - MathUtils.CeilDivPow2(offset, reduction);

        /// <summary>
        /// Computes the smallest resolution reduction whose decoded image fits <paramref name="maxResolution"/> and
        /// the decode memory budgets, bounded by the decomposition levels available in the main header.
        /// </summary>
        public static int ComputeReduction(JpxImageInfo image, JpxHeaderParameters mainHeaderParameters,
            int maxResolution)
        {
            var maxReduction = GetMainHeaderDecompositionLevels(mainHeaderParameters);

            var reduction = 0;
            while (reduction < maxReduction && !FitsDecodeBudget(image, reduction, maxResolution))
            {
                reduction++;
            }

            return reduction;
        }

        /// <summary>
        /// Coordinates and dimensions of a component's sample plane over the image area, at the given resolution
        /// reduction.
        /// </summary>
        /// <remarks>
        /// See ITU-T T.800 (06/2019) Equation B-12, reduced per Equation B-14. Both are ceil divisions, so the
        /// reduced plane is the ceil division of the reference grid coordinates by XRsiz * 2^R.
        /// </remarks>
        public static void GetComponentPlaneArea(JpxImageInfo image, JpxComponent component, int reduction,
            out int x0, out int y0, out int width, out int height)
        {
            x0 = MathUtils.CeilDivPow2(MathUtils.CeilDiv(image.XOsiz, component.XRsizi), reduction);
            y0 = MathUtils.CeilDivPow2(MathUtils.CeilDiv(image.YOsiz, component.YRsizi), reduction);
            width = MathUtils.CeilDivPow2(MathUtils.CeilDiv(image.Xsiz, component.XRsizi), reduction) - x0;
            height = MathUtils.CeilDivPow2(MathUtils.CeilDiv(image.Ysiz, component.YRsizi), reduction) - y0;
        }

        /// <summary>
        /// The smallest number of decomposition levels signalled for any component in the main header, which bounds
        /// how many resolution levels we can discard.
        /// </summary>
        private static int GetMainHeaderDecompositionLevels(JpxHeaderParameters mainParameters)
        {
            var cod = mainParameters.CodingStyleDefaults;
            if (cod == null)
            {
                // A codestream without a main header COD cannot be decoded anyway
                return 0;
            }

            var minLevels = cod.NumberOfDecompositionLevels;

            foreach (var coc in mainParameters.ComponentCodingStyles.Values)
            {
                minLevels = Math.Min(minLevels, coc.NumberOfDecompositionLevels);
            }

            return Math.Max(minLevels, 0);
        }

        private static bool FitsDecodeBudget(JpxImageInfo image, int reduction, int maxResolution)
        {
            var width = GetReducedResolution(image.Xsiz, image.XOsiz, reduction);
            var height = GetReducedResolution(image.Ysiz, image.YOsiz, reduction);

            if (width > maxResolution || height > maxResolution)
            {
                return false;
            }

            var components = image.Components;
            var totalPlaneSamples = 0L;

            for (var c = 0; c < components.Length; c++)
            {
                GetComponentPlaneArea(image, components[c], reduction,
                    out _, out _, out var planeWidth, out var planeHeight);
                totalPlaneSamples += (long)planeWidth * planeHeight;
            }

            return totalPlaneSamples <= JpxConstraints.MaxComponentSamples;
        }
    }
}
