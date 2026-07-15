// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System.Collections.Generic;

namespace PdfToSvg.Imaging.Jpx.Codestream
{
    /// <summary>
    /// Marker segments collected from one header scope: either the main header, or the accumulated
    /// tile-part headers of a single tile.
    /// 
    /// Note that tile-part headers may add overrides during the phase-2 walk.
    /// </summary>
    internal sealed class JpxHeaderParameters
    {
        /// <summary>COD (ITU-T T.800 (06/2019) Section A.6.1), or null when this scope has none.</summary>
        public JpxCodingStyleDefaults? CodingStyleDefaults;

        /// <summary>COC per component index (ITU-T T.800 (06/2019) Section A.6.2).</summary>
        public readonly Dictionary<int, JpxCodingStyleComponent> ComponentCodingStyles = new();

        /// <summary>RGN max-shift per component index (ITU-T T.800 (06/2019) Section A.6.3).</summary>
        public readonly Dictionary<int, int> RegionOfInterestShifts = new();

        /// <summary>QCD (ITU-T T.800 (06/2019) Section A.6.4), or null when this scope has none.</summary>
        public JpxQuantizationDefaults? QuantizationDefaults;

        /// <summary>QCC per component index (ITU-T T.800 (06/2019) Section A.6.5).</summary>
        public readonly Dictionary<int, JpxQuantizationDefaults> ComponentQuantizations = new();

        /// <summary>POC (ITU-T T.800 (06/2019) Section A.6.6), or null when this scope has none.</summary>
        public List<JpxProgressionVolume>? ProgressionOrderChanges;
    }
}
