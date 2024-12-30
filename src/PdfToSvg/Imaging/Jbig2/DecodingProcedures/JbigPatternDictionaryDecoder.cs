// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Imaging.Jbig2.Model;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jbig2.DecodingProcedures
{
    internal class JbigPatternDictionaryDecoder
    {
        /// <summary>
        /// HDMMR
        /// </summary>
        public bool UseMmr;

        /// <summary>
        /// HDPW
        /// </summary>
        public int Width;

        /// <summary>
        /// HDPH
        /// </summary>
        public int Height;

        /// <summary>
        /// GRAYMAX
        /// </summary>
        public int GrayMax;

        /// <summary>
        /// HDTEMPLATE
        /// </summary>
        public int Template;


        public JbigPatternDictionary Decode(VariableBitReader reader)
        {
            var decoder = new JbigGenericRegionDecoder();

            // Table 27
            decoder.UseMmr = UseMmr;
            decoder.Width = Width * (GrayMax + 1);
            decoder.Height = Height;
            decoder.Template = Template;
            decoder.TypicalPrediction = false;
            decoder.ATX = [(sbyte)-Width, -3, 2, -2];
            decoder.ATY = [0, -1, -2, -2];

            var combinedBitmap = decoder.Decode(reader);

            var dic = new JbigPatternDictionary();
            dic.Patterns = new JbigBitmap[GrayMax + 1];

            for (var i = 0; i < dic.Patterns.Length; i++)
            {
                dic.Patterns[i] = combinedBitmap.Crop(i * Width, 0, Width, Height);
            }

            return dic;
        }
    }
}
