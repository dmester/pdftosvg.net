// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpeg
{
    internal class JpegComponent
    {
        public int ComponentIndex;
        public int ComponentId;

        public int HorizontalSamplingFactor = 1;
        public int VerticalSamplingFactor = 1;

        public int QuantizationTableId;
        public JpegQuantizationTable QuantizationTable = JpegQuantizationTable.Identity;

        public int HuffmanDCTableId;
        public JpegHuffmanTable HuffmanDCTable = JpegHuffmanTable.Empty;

        public int HuffmanACTableId;
        public JpegHuffmanTable HuffmanACTable = JpegHuffmanTable.Empty;

        public int DCPredictor;

        public void Restart()
        {
            DCPredictor = 0;
        }
    }

    internal static class JpegComponentExtensions
    {
        public static void Restart(this JpegComponent[] components)
        {
            for (var i = 0; i < components.Length; i++)
            {
                components[i].Restart();
            }
        }
    }
}
