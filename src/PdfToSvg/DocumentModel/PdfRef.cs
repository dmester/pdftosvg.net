// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;

namespace PdfToSvg.DocumentModel
{
    internal class PdfRef
    {
        public PdfRef(PdfObjectId id)
        {
            Id = id;
        }

        public PdfRef(int objectNumber, int generation)
        {
            Id = new PdfObjectId(objectNumber, generation);
        }

        public PdfObjectId Id { get; }

        public override string ToString() => Id.ToString();
    }
}
