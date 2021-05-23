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
        public static Image Create(PdfDictionary imageDictionary, ColorSpace colorSpace)
        {
            var lastFilter = imageDictionary.Stream.Filters.LastOrDefault();

            if (lastFilter.Filter == Filter.DctDecode)
            {
                return new JpegImage(imageDictionary);
            }

            if (lastFilter.Filter == Filter.FlateDecode && KeepDataPngImage.IsSupported(imageDictionary, colorSpace))
            {
                return new KeepDataPngImage(imageDictionary, colorSpace);
            }

            return new ResampledPngImage(imageDictionary, colorSpace);
        }
    }
}
