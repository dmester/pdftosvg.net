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
        public static Filter Crypt { get; } = new CryptFilter();
        public static Filter DctDecode { get; } = new DctDecodeFilter();
        public static Filter FlateDecode { get; } = new FlateDecodeFilter();
        public static Filter LzwDecode { get; } = new LzwDecodeFilter();
        public static Filter RunLengthDecode { get; } = new RunLengthDecodeFilter();
        public static Filter Jbig2Decode { get; } = new DctDecodeFilter();
        public static Filter CcittFaxDecode { get; } = new CcittFaxDecodeFilter();

        private static readonly Dictionary<PdfName, Filter> filters = new Dictionary<PdfName, Filter>
        {
            { Names.ASCII85Decode, Ascii85Decode },
            { AbbreviatedNames.A85, Ascii85Decode },
            { Names.ASCIIHexDecode, AsciiHexDecode },
            { AbbreviatedNames.AHx, AsciiHexDecode },
            { Names.Crypt, Crypt },
            { Names.CCITTFaxDecode, CcittFaxDecode },
            { AbbreviatedNames.CCF, CcittFaxDecode },
            { Names.DCTDecode, DctDecode },
            { AbbreviatedNames.DCT, DctDecode },
            { Names.FlateDecode, FlateDecode },
            { AbbreviatedNames.Fl, FlateDecode },
            { Names.LZWDecode, LzwDecode },
            { AbbreviatedNames.LZW, LzwDecode },
            { Names.RunLengthDecode, RunLengthDecode },
            { AbbreviatedNames.RL, RunLengthDecode },
            { Names.JBIG2Decode, Jbig2Decode },
        };

        public abstract Stream Decode(Stream encodedStream, PdfDictionary? decodeParms);

        public virtual bool CanDetectStreamLength => false;

        public virtual int DetectStreamLength(Stream stream)
        {
            throw new NotSupportedException();
        }

        public static Filter ByName(PdfName? filterName)
        {
            return
                filterName != null && filters.TryGetValue(filterName, out var filter) ?
                filter :
                new UnsupportedFilter(filterName);
        }
    }
}
