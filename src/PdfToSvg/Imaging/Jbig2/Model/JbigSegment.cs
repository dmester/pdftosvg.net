// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.Imaging.Jbig2.Coding;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PdfToSvg.Imaging.Jbig2.Model
{
    [DebuggerDisplay("Segment {SegmentNumber}: {Type}")]
    internal class JbigSegment
    {
        private static VariableBitReader EmptyReader = new VariableBitReader(ArrayUtils.Empty<byte>(), 0, 0);

        public int SegmentNumber;
        public JbigSegmentType Type;

        public int Offset;
        public int DataLength;

        public int UsedCustomHuffmanTables;

        public VariableBitReader Reader = EmptyReader;

        public List<int> ReferredSegmentNumbers = new();
        public List<JbigSegment> ReferredSegments = new();

        public JbigRegionSegmentInfo RegionInfo = new();

        public bool Decoded;

        // Results
        public JbigPage? Page;
        public JbigSymbolDictionary? SymbolDictionary;
        public JbigPatternDictionary? PatternDictionary;
        public JbigHuffmanTable? HuffmanTable;
        public JbigBitmap? Bitmap;
    }
}
