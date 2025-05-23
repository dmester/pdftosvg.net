﻿// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompressCMaps
{
    internal class CMapWriter : BinaryWriter
    {
        public CMapWriter() : base(new MemoryStream()) { }
        public byte[] ToArray() => ((MemoryStream)BaseStream).ToArray();
    }
}
