// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jpx.Container
{
    internal enum JpxBoxType : uint
    {
        // ITU-T T.800 (06/2019) Table I.2 defined JP2 boxes

        /// <summary>Type 'jP\040\040'</summary>
        Signature = 0x6A502020,

        /// <summary>Type 'ftyp'</summary>
        FileType = 0x66747970,

        /// <summary>Type 'jp2h'</summary>
        JP2Header = 0x6A703268,

        /// <summary>Type 'ihdr'</summary>
        ImageHeader = 0x69686472,

        /// <summary>Type 'bpcc'</summary>
        BitsPerComponent = 0x62706363,

        /// <summary>Type 'colr'</summary>
        ColourSpecification = 0x636F6C72,

        /// <summary>Type 'pclr'</summary>
        Palette = 0x70636C72,

        /// <summary>Type 'cmap'</summary>
        ComponentMapping = 0x636D6170,

        /// <summary>Type 'cdef'</summary>
        ChannelDefinition = 0x63646566,

        /// <summary>Type 'res\040'</summary>
        Resolution = 0x72657320,

        /// <summary>Type 'resc'</summary>
        CaptureResolution = 0x72657363,

        /// <summary>Type 'resd'</summary>
        DefaultDisplayResolution = 0x72657364,

        /// <summary>Type 'jp2c'</summary>
        ContiguousCodestream = 0x6A703263,

        /// <summary>Type 'jp2i'</summary>
        IntellectualProperty = 0x6A703269,

        /// <summary>Type 'xml\040'</summary>
        Xml = 0x786D6C20,

        /// <summary>Type 'uuid'</summary>
        Uuid = 0x75756964,

        /// <summary>Type 'uinf'</summary>
        UuidInfo = 0x75696E66,

        /// <summary>Type 'ulst'</summary>
        UuidList = 0x75637374,

        /// <summary>Type 'url\040'</summary>
        Url = 0x75726C20,
    }
}
