using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Filters
{
    internal static class PredictorStream
    {
        private const int DefaultPredictor = 1;
        private const int DefaultColumns = 1;
        private const int DefaultBitsPerComponent = 8;
        private const int DefaultColors = 1;

        public static Stream Create(Stream decompressedStream, PdfDictionary? decodeParms)
        {
            var predictor = DefaultPredictor;
            var columns = DefaultColumns;
            var bitsPerComponent = DefaultBitsPerComponent;
            var colors = DefaultColors;

            if (decodeParms != null)
            {
                predictor = decodeParms.GetValueOrDefault(Names.Predictor, DefaultPredictor);
                columns = decodeParms.GetValueOrDefault(Names.Columns, DefaultColumns);
                bitsPerComponent = decodeParms.GetValueOrDefault(Names.BitsPerComponent, DefaultBitsPerComponent);
                colors = decodeParms.GetValueOrDefault(Names.Colors, DefaultColors);
            }

            if (predictor >= 10)
            {
                return new PngDepredictorStream(decompressedStream, colors, bitsPerComponent, columns);
            }

            if (predictor == 2)
            {
                return new TiffDepredictorStream(decompressedStream, colors, bitsPerComponent, columns);
            }

            return decompressedStream;
        }
    }
}
