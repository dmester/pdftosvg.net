// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Imaging.Jpx
{
    internal sealed class JpxImageData
    {
        public JpxImageData(int width, int height, float[][] colorChannels, float[]? alphaChannel)
        {
            Width = width;
            Height = height;
            ColorChannels = colorChannels;
            AlphaChannel = alphaChannel;
        }

        public int Width { get; }

        public int Height { get; }

        /// <summary>
        /// One plane per colour channel, each a <see cref="Width"/> * <see cref="Height"/> array of samples in [0, 1],
        /// row-major.
        /// </summary>
        public float[][] ColorChannels { get; }

        /// <summary>
        /// Straight (un-premultiplied) alpha plane (<see cref="Width"/> * <see cref="Height"/>, [0, 1]), or <c>null</c>
        /// when there is no alpha. Kept separate from <see cref="ColorChannels"/> because alpha bypasses the color
        /// /Decode array and colorspace conversion.
        /// </summary>
        public float[]? AlphaChannel { get; }
    }
}
