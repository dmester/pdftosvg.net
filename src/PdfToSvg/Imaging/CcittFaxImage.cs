// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.DocumentModel;
using PdfToSvg.Imaging.Fax;
using PdfToSvg.Imaging.Png;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Imaging
{
    internal class CcittFaxImage : Image
    {
        private readonly PdfStream imageDictionaryStream;
        private readonly PdfDictionary imageDictionary;
        private readonly ColorSpace colorSpace;
        private readonly PdfDictionary decodeParms;

        public CcittFaxImage(PdfDictionary imageDictionary, PdfDictionary? decodeParms, ColorSpace colorSpace)
            : base(imageDictionary, "image/png", ".png")
        {
            if (imageDictionary.Stream == null)
            {
                throw new ArgumentException("There was no data stream attached to the image dictionary.", nameof(imageDictionary));
            }

            this.imageDictionary = imageDictionary;
            this.imageDictionaryStream = imageDictionary.Stream;
            this.colorSpace = colorSpace;
            this.decodeParms = decodeParms ?? new PdfDictionary();
        }

        private byte[] Convert(byte[] sourceFaxData)
        {
            var decoder = new FaxDecoder();
            var pngStream = new MemoryStream();
            var pngWriter = new PngEncoder(pngStream);

            decoder.K = decodeParms.GetValueOrDefault(Names.K, 0);
            decoder.Width = decodeParms.GetValueOrDefault(Names.Columns, 1728);
            decoder.Height = decodeParms.GetValueOrDefault(Names.Rows, 0);
            decoder.EncodedByteAlign = decodeParms.GetValueOrDefault(Names.EncodedByteAlign, false);

            var blackIs1 = decodeParms.GetValueOrDefault(Names.BlackIs1, false);
            var decodeArray = ImageHelper.GetDecodeArray(imageDictionary, colorSpace);

            // Prepare palette
            const int PaletteBytesPerColor = 3;
            var palette = new byte[2 * PaletteBytesPerColor];
            var indexedColorValue = new[] { 0f, 1f };
            decodeArray.Decode(indexedColorValue, 0, 2);
            colorSpace.ToRgb8(indexedColorValue, 0, palette, 0, indexedColorValue.Length);

            // Write PNG headers
            pngWriter.WriteSignature();

            var headerPosition = pngStream.Position;
            pngWriter.WriteImageHeader(decoder.Width, decoder.Height, PngColorType.IndexedColour, 1);

            pngWriter.WriteImageGamma();
            pngWriter.WritePalette(palette);

            var actualHeight = 0;

            using (var pngDataStream = pngWriter.GetImageDataStream())
            {
                var bytesPerRow = (decoder.Width - 1) / 8 + 1;
                var pngRow = new byte[1 + bytesPerRow];

                foreach (var faxRow in decoder.ReadRows(sourceFaxData, 0, sourceFaxData.Length))
                {
                    var pngRowCursor = 1;
                    var packedByteCursor = 0;
                    var packedByteValue = 0;

                    for (var i = 0; i < faxRow.Length; i++)
                    {
                        packedByteValue = (packedByteValue << 1) | (faxRow[i] == blackIs1 ? 0 : 1);
                        packedByteCursor++;

                        if (packedByteCursor == 8)
                        {
                            pngRow[pngRowCursor++] = (byte)packedByteValue;
                            packedByteCursor = 0;
                        }
                    }

                    if (packedByteCursor > 0)
                    {
                        packedByteValue = packedByteValue << (8 - packedByteCursor);
                        pngRow[pngRowCursor++] = (byte)packedByteValue;
                    }

                    pngDataStream.Write(pngRow);
                    actualHeight++;
                }
            }

            pngWriter.WriteImageEnd();

            // Update height
            pngStream.Position = headerPosition;
            pngWriter.WriteImageHeader(decoder.Width, actualHeight, PngColorType.IndexedColour, 1);

            return pngStream.ToArray();
        }

        public override byte[] GetContent(CancellationToken cancellationToken)
        {
            return Convert(ImageHelper.GetContent(imageDictionaryStream, cancellationToken));
        }

#if HAVE_ASYNC
        public override async Task<byte[]> GetContentAsync(CancellationToken cancellationToken)
        {
            return Convert(await ImageHelper
                .GetContentAsync(imageDictionaryStream, cancellationToken)
                .ConfigureAwait(false));
        }
#endif

        public override int GetHashCode() =>
            604859080 ^
            RuntimeHelpers.GetHashCode(imageDictionaryStream) ^
            colorSpace.GetHashCode();

        public override bool Equals(object? obj) =>
            obj is CcittFaxImage ccittImage &&
            ReferenceEquals(ccittImage.imageDictionaryStream, imageDictionaryStream) &&
            ccittImage.colorSpace.Equals(colorSpace);
    }
}
