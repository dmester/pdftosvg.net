// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Encodings
{
    internal struct CharacterCode
    {
        public CharacterCode(uint code, int sourceLength, string dstString)
        {
            Code = code;
            SourceLength = sourceLength;
            DestinationString = dstString;
        }

        /// <summary>
        /// Input character code.
        /// </summary>
        public uint Code { get; }

        /// <summary>
        /// Length of input code in bytes.
        /// </summary>
        public int SourceLength { get; }

        /// <summary>
        /// Replacement display string.
        /// </summary>
        public string DestinationString { get; }

        public bool IsEmpty => SourceLength == 0;

        public static CharacterCode Empty => new CharacterCode();

        public override string ToString() => IsEmpty ? "\ufffd" : DestinationString;
    }
}
