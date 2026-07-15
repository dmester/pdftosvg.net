// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Imaging.Jpx.Codestream
{
    internal enum JpxCodingProgressionOrder
    {
        // ITU-T T.800 (06/2019) Table A.16 – Progression order for the SGcod, SPcoc and Ppoc parameters
        LayerResolutionLevelComponentPosition = 0b0000_0000,
        ResolutionLevelLayerComponentPosition = 0b0000_0001,
        ResolutionLevelPositionComponentLayer = 0b0000_0010,
        PositionComponentResolutionLevelLayer = 0b0000_0011,
        ComponentPositionResolutionLevelLayer = 0b0000_0100,
    }
}
