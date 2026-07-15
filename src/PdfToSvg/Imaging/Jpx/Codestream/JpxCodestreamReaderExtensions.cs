// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Imaging.Jpx.IO;

namespace PdfToSvg.Imaging.Jpx.Codestream
{
    internal static class JpxCodestreamReaderExtensions
    {
        public static bool TryReadMarker(this JpxDataReader reader, JpxMarker marker)
        {
            if (reader.Cursor + 2 <= reader.Length)
            {
                var startCursor = reader.Cursor;
                var actualMarker = (JpxMarker)reader.ReadUInt16();

                if (marker == actualMarker)
                {
                    return true;
                }

                reader.Cursor = startCursor;
            }

            return false;
        }

        public static bool PeekMarker(this JpxDataReader reader, JpxMarker marker)
        {
            if (reader.Cursor + 2 <= reader.Length)
            {
                var startCursor = reader.Cursor;
                var actualMarker = (JpxMarker)reader.ReadUInt16();
                reader.Cursor = startCursor;

                return marker == actualMarker;
            }

            return false;
        }

        public static JpxMarker ReadMarker(this JpxDataReader reader)
        {
            var code = reader.ReadUInt16();
            if ((code & 0xff00) != 0xff00)
            {
                throw new JpxException(
                    "Expected a marker in the JPEG 2000 codestream, but found 0x" + code.ToString("x4"));
            }
            return (JpxMarker)code;
        }

        public static JpxDataReader ReadSegmentContent(this JpxDataReader reader)
        {
            var length = reader.ReadUInt16();

            // ITU-T T.800 (06/2019) Section A.1:
            // The first two bytes after a marker hold the length of the marker segment parameters, including the length
            // field itself but not the marker.
            if (length < 2 || length - 2 > reader.Length - reader.Cursor)
            {
                throw new JpxException("Invalid JPEG 2000 marker segment length");
            }

            return reader.Slice(length - 2);
        }

        public static void SkipSegmentContent(this JpxDataReader reader)
        {
            var length = reader.ReadUInt16();

            if (length < 2 || length - 2 > reader.Length - reader.Cursor)
            {
                throw new JpxException("Invalid JPEG 2000 marker segment length");
            }

            reader.SkipBytes(length - 2);
        }
    }
}
