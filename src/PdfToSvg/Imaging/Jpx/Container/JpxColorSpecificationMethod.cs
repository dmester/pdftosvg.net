// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg.Imaging.Jpx.Container
{
    internal enum JpxColorSpecificationMethod
    {
        // ITU-T T.800 (06/2019) Table I.9 – Legal METH values
        EnumeratedColorSpace = 1,
        RestrictedIcc = 2,

        // ITU-T T.801 (06/2021) Table M.22 – Legal METH values
        AnyIcc = 3,
        VendorColor = 4,
        ParameterizedColorspace = 5,
    }
}
