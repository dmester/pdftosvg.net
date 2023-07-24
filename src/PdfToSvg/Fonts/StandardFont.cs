// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Fonts
{
    internal class StandardFont
    {
        public PdfName Name { get; }
        public byte[] Data { get; }
        public SingleByteEncoding Encoding { get; }
        public string? License { get; }

        public StandardFont(PdfName name, byte[] data, SingleByteEncoding encoding, string? license)
        {
            Name = name;
            Data = data;
            Encoding = encoding;
            License = license;
        }

        public override string ToString()
        {
            return Name.Value;
        }
    }
}
