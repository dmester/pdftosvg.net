// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Imaging.Jpx
{
    /// <summary>
    /// Determines how <see cref="JpxDecoder.ReadImageData"/> resolves the alpha channel.
    /// The PDF spec (ISO 32000-2:2020 Table 87) makes alpha independent of the color space:
    /// the /SMaskInData entry alone decides whether embedded opacity is used.
    /// </summary>
    internal enum JpxAlphaMode
    {
        /// <summary>Drop any embedded alpha (SMaskInData = 0 / absent).</summary>
        None,

        /// <summary>Straight opacity (SMaskInData = 1).</summary>
        Opacity,

        /// <summary>Premultiplied opacity (SMaskInData = 2); colours are un-premultiplied.</summary>
        Premultiplied,

        /// <summary>Defer to the JP2 channel definition box (non-PDF / standalone callers).</summary>
        FromCodestream,
    }
}
