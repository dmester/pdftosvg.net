// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpx.Codestream
{
    internal enum JpxMarker
    {
        // ITU-T T.800 (06/2019) Table A.2 marker and marker segment codes
        SOC = 0xFF4F, // Start of codestream
        SOT = 0xFF90, // Start of tile-part
        SOD = 0xFF93, // Start of data
        EOC = 0xFFD9, // End of codestream

        SIZ = 0xFF51, // Image and tile size
        PRF = 0xFF56, // Profile
        CAP = 0xFF50, // Extended capabilities

        COD = 0xFF52, // Coding style default
        COC = 0xFF53, // Coding style component
        RGN = 0xFF5E, // Region-of-interest
        QCD = 0xFF5C, // Quantization default
        QCC = 0xFF5D, // Quantization component
        POC = 0xFF5F, // Progression order change

        TLM = 0xFF55, // Tile-part lengths
        PLM = 0xFF57, // Packet length, main header
        PLT = 0xFF58, // Packet length, tile-part header
        PPM = 0xFF60, // Packed packet headers, main header
        PPT = 0xFF61, // Packed packet headers, tile-part header

        SOP = 0xFF91, // Start of packet
        EPH = 0xFF92, // End of packet header

        CRG = 0xFF63, // Component registration
        COM = 0xFF64, // Comment

        // ITU-T T.801 (06/2021) Table A.19 extended marker segment codes
        QPD = 0xFF5A, // Quantization default, precinct
        QPC = 0xFF5B, // Quantization component, precinct
        DCO = 0xFF70, // Variable DC offset
        VMS = 0xFF71, // Visual masking
        DFS = 0xFF72, // Downsampling factor style
        ADS = 0xFF73, // Arbitrary decomposition style
        MCT = 0xFF74, // Multiple component transformation definition
        MCC = 0xFF75, // Multiple component collection
        NLT = 0xFF76, // Non-linearity point transformation
        MCO = 0xFF77, // Multiple component transformation ordering
        CBD = 0xFF78, // Component bit depth
        ATK = 0xFF79, // Arbitrary transformation kernels
        RLT = 0xFF94, // Precinct length, tile-part header
    }
}
