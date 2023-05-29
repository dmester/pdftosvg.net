// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpeg
{
    internal enum JpegChromaSubSampling
    {
        /// <summary>
        /// No sub sampling (a.k.a. 4:4:4, Y 1x1, Cb 1x1, Cr 1x1)
        /// </summary>
        None = 0,
        /// <summary>
        /// No sub sampling (a.k.a. 4:4:4, Y 1x1, Cb 1x1, Cr 1x1)
        /// </summary>
        Ratio444 = 0x11,
        /// <summary>
        /// 4:4:0 sub sampling (Y 1x2, Cb 1x1, Cr 1x1)
        /// </summary>
        Ratio440 = 0x12,
        /// <summary>
        /// 4:2:2 sub sampling (Y 2x1, Cb 1x1, Cr 1x1)
        /// </summary>
        Ratio422 = 0x21,
        /// <summary>
        /// 4:2:0 sub sampling (Y 2x2, Cb 1x1, Cr 1x1)
        /// </summary>
        Ratio420 = 0x22,
        /// <summary>
        /// 4:1:1 sub sampling (Y 4x1, Cb 1x1, Cr 1x1)
        /// </summary>
        Ratio411 = 0x41,
    }
}
