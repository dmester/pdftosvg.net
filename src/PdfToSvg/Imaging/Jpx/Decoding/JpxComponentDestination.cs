// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Imaging.Jpx.Decoding
{
    /// <summary>
    /// Where and how <see cref="JpxTileDecoder"/> stores the reconstructed samples of one component.
    /// </summary>
    internal struct JpxComponentDestination
    {
        /// <summary>
        /// Full-image sample plane of the component at its native resolution, or <c>null</c> when the component
        /// should not be decoded at all (unless it is needed as input to the multiple component transformation).
        /// </summary>
        public float[]? Plane;

        // Coordinates of the plane's upper left sample in the component's coordinate system
        public int PlaneX0;
        public int PlaneY0;

        public int PlaneWidth;

        /// <summary>
        /// Offset added to every reconstructed sample before it is stored.
        /// </summary>
        public float SampleOffset;

        /// <summary>
        /// Scale applied after <see cref="SampleOffset"/>. Only used when <see cref="Normalize"/> is set.
        /// </summary>
        public float SampleScale;

        /// <summary>
        /// When set, stored samples are computed as <c>clamp((sample + SampleOffset) * SampleScale, 0, 1)</c>;
        /// otherwise only <see cref="SampleOffset"/> is added and the samples are stored unclamped.
        /// </summary>
        public bool Normalize;
    }
}
