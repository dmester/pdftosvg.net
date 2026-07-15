// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jpx.ImageModel;
using System;

namespace PdfToSvg.Imaging.Jpx.Decoding
{
    internal abstract class JpxDecodedImage
    {
        protected readonly JpxImageInfo image;
        protected readonly float[]?[] componentPlanes;
        protected readonly JpxComponentUsage[] componentUsages;
        protected readonly int resolutionReduction;

        protected JpxDecodedImage(JpxImageInfo image, float[]?[] componentPlanes,
            JpxComponentUsage[] componentUsages, int resolutionReduction)
        {
            this.image = image;
            this.componentPlanes = componentPlanes;
            this.componentUsages = componentUsages;
            this.resolutionReduction = resolutionReduction;
        }

        protected int DecodedWidth =>
            JpxResolutionReducer.GetReducedResolution(image.Xsiz, image.XOsiz, resolutionReduction);

        protected int DecodedHeight =>
            JpxResolutionReducer.GetReducedResolution(image.Ysiz, image.YOsiz, resolutionReduction);

        /// <summary>
        /// Returns the decoded samples of one codestream component resampled to the image pixel grid
        /// </summary>
        protected ImageGridPlane GetImageGridPlane(int componentIndex)
        {
            if (componentIndex < 0 || componentIndex >= image.Components.Length)
            {
                throw new JpxException("A JPEG 2000 channel refers to a component not present in the codestream");
            }

            var component = image.Components[componentIndex];
            var plane = componentPlanes[componentIndex] ??
                throw new JpxException("The JPEG 2000 component was not decoded");

            var imageWidth = DecodedWidth;
            var imageHeight = DecodedHeight;

            JpxResolutionReducer.GetComponentPlaneArea(image, component, resolutionReduction,
                out var planeX0, out var planeY0, out var planeWidth, out var planeHeight);

            if (planeWidth == imageWidth && planeHeight == imageHeight)
            {
                return new ImageGridPlane(plane, shared: true);
            }

            if (planeWidth < 1 || planeHeight < 1)
            {
                return new ImageGridPlane(ArrayUtils.Empty<float>(), shared: false);
            }

            var result = new float[imageWidth * imageHeight];

            // Sample replication to the image pixel grid. At a resolution reduction R, both the pixels and the plane
            // samples live on grids whose coordinates are the reference grid coordinates divided by 2^R, so the
            // full-resolution relations of ITU-T T.800 (06/2019) Section B.2 carry over with reduced origins:
            // pixel (x, y) sits at reduced grid point (imageX0 + x, imageY0 + y), and plane sample p covers reduced
            // grid columns [(planeX0 + p) * XRsiz, (planeX0 + p + 1) * XRsiz). Each row is expanded as runs of
            // XRsiz pixels per sample: pixels preceding the first sample's coverage are clamped to the first
            // sample, and the runs cannot overrun the plane, since the last pixel maps to sample
            // floor((imageX0 + imageWidth - 1) / XRsiz), which equals planeX0 + planeWidth - 1 by the definition of
            // the plane area.
            var imageX0 = MathUtils.CeilDivPow2(image.XOsiz, resolutionReduction);
            var imageY0 = MathUtils.CeilDivPow2(image.YOsiz, resolutionReduction);

            var firstImageX = Math.Min(planeX0 * component.XRsizi - imageX0, imageWidth);

            var previousPlaneY = -1;
            for (var imageY = 0; imageY < imageHeight; imageY++)
            {
                var planeY = MathUtils.Clamp((imageY0 + imageY) / component.YRsizi - planeY0, 0, planeHeight - 1);
                var planeRowIndex = planeY * planeWidth;
                var targetRowIndex = imageY * imageWidth;

                if (planeY == previousPlaneY)
                {
                    Array.Copy(result, targetRowIndex - imageWidth, result, targetRowIndex, imageWidth);
                    continue;
                }

                var imageX = 0;
                for (; imageX < firstImageX; imageX++)
                {
                    result[targetRowIndex + imageX] = plane[planeRowIndex];
                }

                for (var planeX = 0; imageX < imageWidth; planeX++)
                {
                    var value = plane[planeRowIndex + planeX];
                    var runEnd = Math.Min(imageX + component.XRsizi, imageWidth);

                    for (; imageX < runEnd; imageX++)
                    {
                        result[targetRowIndex + imageX] = value;
                    }
                }

                previousPlaneY = planeY;
            }

            return new ImageGridPlane(result, shared: false);
        }

        protected readonly struct ImageGridPlane(float[] samples, bool shared)
        {
            public readonly float[] Samples = samples;

            /// <summary>
            /// When <c>true</c>, <see cref="Samples"/> is the component's decoded plane itself and must not be mutated
            /// before being returned to the decoder consumer; otherwise the array is freshly allocated and owned by
            /// the caller.
            /// </summary>
            public readonly bool Shared = shared;
        }
    }
}
