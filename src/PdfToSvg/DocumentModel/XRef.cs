// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfToSvg.DocumentModel
{
    internal class XRef : IEquatable<XRef?>
    {
        public int ObjectNumber;

        public XRefEntryType Type;

        public long ByteOffset;
        public int Generation;

        public int CompressedObjectNumber;
        public int CompressedObjectElementIndex;

        public bool Equals(XRef? other)
        {
            return
                other != null &&
                other.CompressedObjectElementIndex == CompressedObjectElementIndex &&
                other.CompressedObjectNumber == CompressedObjectNumber &&
                other.ByteOffset == ByteOffset &&
                other.ObjectNumber == ObjectNumber &&
                other.Type == Type &&
                other.Generation == Generation;
        }

        public override bool Equals(object obj) => Equals(obj as XRef);

        public override int GetHashCode() => (ObjectNumber << 10) ^ Generation;

        public override string ToString()
        {
            return $"{ObjectNumber} {Type} {ByteOffset} {Generation}";
        }

    }
}
