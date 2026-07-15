// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpx
{
    /// <summary>
    /// These constraints declare reasonable max values to protect against malicious files.
    /// </summary>
    internal static class JpxConstraints
    {
        // Compliant with Cclass 2 (parameter W x H)
        public const int MaxResolution = 16384;

        // Default max width/height of decoded images. Images exceeding this limit are decoded at a lower resolution.
        public const int DefaultMaxDecodeResolution = 8192;

        // Compliant with Cclass 1 (parameter Ncomp)
        public const int MaxComponents = 384;

        // Compliant with Cclass 1 (parameter C); CMYK + alpha
        public const int MaxOutputPlanes = 5;

        // Coefficients are stored as Int32
        public const int MaxComponentPrecision = 31;

        public const int MaxTileCount = 4096;

        // Max total samples across the full-image component planes of an image. The resolution reduction planning
        // counts every component, whether or not it will be decoded, so the reduction is independent of the decode
        // plan; the decode itself enforces the limit over the actually allocated planes.
        public const int MaxComponentSamples = 512 * MB / sizeof(float);

        public const int MaxPrecinctsPerResolution = 262_144;
        public const int MaxCodeBlocksPerSubBand = 262_144;

        // ITU-T T.800 (06/2019) Table A.46 constrains SPrgn to at most 37
        // for the base profiles supported by this decoder.
        public const int MaxRegionOfInterestShift = 37;

        public const int MaxComponentMappings = 8;

        private const int MB = 1 << 20;
    }
}
