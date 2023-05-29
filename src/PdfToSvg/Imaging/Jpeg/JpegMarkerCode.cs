// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpeg
{
    internal enum JpegMarkerCode
    {
        // ITU T.81
        // Table B.1 – Marker code assignments

        FirstMarker = 0xff01,

        /// <summary>
        /// Baseline DCT
        /// </summary>
        SOF0 = 0xffc0,

        /// <summary>
        /// Define Huffman table(s)
        /// </summary>
        DHT = 0xffc4,

        /// <summary>
        /// Define arithmetic coding conditioning(s)
        /// </summary>
        DAC = 0xffcc,

        /// <summary>
        /// Start of image
        /// </summary>
        SOI = 0xffd8,

        /// <summary>
        /// End of image
        /// </summary>
        EOI = 0xffd9,

        /// <summary>
        /// Start of scan
        /// </summary>
        SOS = 0xffda,

        /// <summary>
        /// Define quantization table(s)
        /// </summary>
        DQT = 0xffdb,

        /// <summary>
        /// Define restart interval
        /// </summary>
        DRI = 0xffdd,

        /// <summary>
        /// JFIF segment
        /// </summary>
        APP0 = 0xffe0,

        /// <summary>
        /// Adobe application-specific
        /// </summary>
        APP14 = 0xffee,
    }
}
