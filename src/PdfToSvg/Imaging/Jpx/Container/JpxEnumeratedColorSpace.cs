// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Imaging.Jpx.Container
{
    internal enum JpxEnumeratedColorSpace
    {
        Unknown = -1,

        // ITU-T T.801 (06/2021) Table M.25 – Additional legal EnumCS values
        BiLevel = 0,
        YCbCr1 = 1,
        YCbCr2 = 3,
        YCbCr3 = 4,
        PhotoYcc = 9,
        Cmy = 11,
        Cmyk = 12,
        Ycck = 13,
        CieLab = 14,
        Bilevel2 = 15,

        // ITU-T T.800 (06/2019) Table I.10 – Legal EnumCS values
        SRgb = 16,
        Greyscale = 17,
        SYcc = 18,

        // ITU-T T.801 (06/2021) Table M.25 – Additional legal EnumCS values (continued)
        CieJab = 19,
        ESRgb = 20,
        RommRgb = 21,
        YPbPr1125 = 22,
        YPbPr1250 = 23,
        ESYcc = 24,
        ScRgb = 25,
        ScRgbGrey = 26,
    }
}
