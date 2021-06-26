// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.DocumentModel;
using PdfToSvg.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Imaging
{
    internal static class ImageFactory
    {
        public static Image? Create(PdfDictionary imageDictionary, ColorSpace colorSpace)
        {
            var stream = imageDictionary.Stream;
            if (stream == null)
            {
                throw new ArgumentException("The specified image dictionary does not contain a stream.", nameof(imageDictionary));
            }

            var hasUnsupportedFilter = stream.Filters.Any(filter => filter.Filter is UnsupportedFilter);
            if (hasUnsupportedFilter)
            {
                return null;
            }

            var lastFilter = stream.Filters.LastOrDefault();
            if (lastFilter != null)
            {
                if (lastFilter.Filter == Filter.DctDecode)
                {
                    return JpegImage.IsSupported(colorSpace) ? new JpegImage(imageDictionary) : null;
                }

                if (lastFilter.Filter == Filter.FlateDecode && KeepDataPngImage.IsSupported(imageDictionary, colorSpace))
                {
                    return new KeepDataPngImage(imageDictionary, colorSpace);
                }
            }

            return new ResampledPngImage(imageDictionary, colorSpace);
        }
    }
}
