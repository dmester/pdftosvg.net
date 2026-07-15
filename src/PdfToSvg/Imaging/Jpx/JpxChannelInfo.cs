// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Imaging.Jpx
{
    internal readonly struct JpxChannelInfo(int colorChannelCount, bool hasAlphaChannel)
    {
        public readonly int ColorChannelCount = colorChannelCount;
        public readonly bool HasAlphaChannel = hasAlphaChannel;
    }
}
