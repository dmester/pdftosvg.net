// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Filters
{
    internal abstract class Filter
    {
        public static Filter Ascii85Decode { get; } = new Ascii85DecodeFilter();
        public static Filter AsciiHexDecode { get; } = new AsciiHexDecodeFilter();
        public static Filter DctDecode { get; } = new DctDecodeFilter();
        public static Filter FlateDecode { get; } = new FlateDecodeFilter();
        public static Filter LzwDecode { get; } = new LzwDecodeFilter();
        public static Filter RunLengthDecode { get; } = new RunLengthDecodeFilter();
        public static Filter Identity { get; } = new IdentityFilter();

        private static readonly Dictionary<PdfName, Filter> filters = new Dictionary<PdfName, Filter>
        {
            { Names.ASCII85Decode, Ascii85Decode },
            { Names.ASCIIHexDecode, AsciiHexDecode },
            { Names.DCTDecode, DctDecode },
            { Names.FlateDecode, FlateDecode },
            { Names.LZWDecode, LzwDecode },
            { Names.RunLengthDecode, RunLengthDecode },
        };

        public abstract Stream Decode(Stream encodedStream, PdfDictionary? decodeParms);

        public virtual bool CanDetectStreamLength => false;

        public virtual int DetectStreamLength(Stream stream)
        {
            throw new NotSupportedException();
        }

        public static Filter? ByName(PdfName? filterName)
        {
            return
                filterName != null && filters.TryGetValue(filterName, out var filter) ?
                filter : null;
        }
    }
}
