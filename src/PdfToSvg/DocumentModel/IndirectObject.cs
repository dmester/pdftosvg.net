﻿// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.IO;

namespace PdfToSvg.DocumentModel
{
    internal class IndirectObject
    {
        public IndirectObject(PdfObjectId id, object? value)
        {
            ID = id;
            Value = value;
        }

        public PdfObjectId ID { get; }
        public object? Value { get; }
    }
}
