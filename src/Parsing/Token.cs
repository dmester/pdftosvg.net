// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg
{
    internal enum Token
    {
        None = 0,
        EndOfInput,
        BeginDictionary,
        EndDictionary,
        Real,
        Integer,
        String,
        True,
        False,
        Name,
        BeginArray,
        EndArray,
        Null,
        LiteralString,
        HexString,
        Ref,
        Obj,
        EndObj,
        EndStream,
        Stream,
        Trailer,
        Xref,
        NotFree,
        Free,
        Keyword,
        BeginImage,
        BeginImageData,
        EndImage,
        BeginCodeSpaceRange,
        EndCodeSpaceRange,
        BeginBfChar,
        EndBfChar,
        BeginBfRange,
        EndBfRange,
        BeginNotDefChar,
        EndNotDefChar,
        BeginNotDefRange,
        EndNotDefRange,
    }
}
