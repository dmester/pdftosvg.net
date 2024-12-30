// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.DocumentModel;
using PdfToSvg.Imaging.Fax;
using PdfToSvg.Imaging.Jbig2;
using PdfToSvg.Imaging.Jpeg;
using PdfToSvg.Imaging.Png;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Imaging
{
    internal class Jbig2Image : Image
    {
        private readonly PdfStream imageDictionaryStream;
        private readonly PdfDictionary imageDictionary;
        private readonly ColorSpace colorSpace;
        private readonly PdfDictionary decodeParms;

        public Jbig2Image(PdfDictionary imageDictionary, PdfDictionary? decodeParms, ColorSpace colorSpace)
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

        private byte[] Convert(byte[]? globalsData, byte[] pageData, CancellationToken cancellationToken)
        {
            var decoder = new JbigDecoder();
            var pngStream = new MemoryStream();
            var pngWriter = new PngEncoder(pngStream);

            decoder.EmbeddedMode = true;

            if (globalsData != null)
            {
                decoder.Read(globalsData, 0, globalsData.Length);
            }

            decoder.Read(pageData, 0, pageData.Length);

            var pageBitmap = decoder.DecodePage(1, cancellationToken);

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
            pngWriter.WriteImageHeader(pageBitmap.Width, pageBitmap.Height, PngColorType.IndexedColour, 1);

            pngWriter.WriteImageGamma();
            pngWriter.WritePalette(palette);

            using (var pngDataStream = pngWriter.GetImageDataStream())
            {
                var bytesPerRow = (pageBitmap.Width - 1) / 8 + 1;
                var pngRow = new byte[1 + bytesPerRow];

                var pageBuffer = pageBitmap.GetBuffer();
                var pageBufferCursor = 0;

                for (var y = 0; y < pageBitmap.Height; y++)
                {
                    var pngRowCursor = 1;
                    var packedByteCursor = 0;
                    var packedByteValue = 0;

                    for (var x = 0; x < pageBitmap.Width; x++)
                    {
                        packedByteValue = (packedByteValue << 1) | (pageBuffer[pageBufferCursor] ? 0 : 1);
                        packedByteCursor++;
                        pageBufferCursor++;

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
                }
            }

            pngWriter.WriteImageEnd();

            return pngStream.ToArray();
        }

        public override byte[] GetContent(CancellationToken cancellationToken)
        {
            byte[]? globalsData = null;

            if (decodeParms.TryGetStream(Names.JBIG2Globals, out var globalsStream))
            {
                using var memoryStream = new MemoryStream();

                using (var decodedStream = globalsStream.OpenDecoded(cancellationToken))
                {
                    decodedStream.CopyTo(memoryStream, cancellationToken);
                }

                globalsData = memoryStream.ToArray();
            }

            var pageData = ImageHelper.GetContent(imageDictionaryStream, cancellationToken);

            return Convert(globalsData, pageData, cancellationToken);
        }

#if HAVE_ASYNC
        public override async Task<byte[]> GetContentAsync(CancellationToken cancellationToken)
        {
            byte[]? globalsData = null;

            if (decodeParms.TryGetStream(Names.JBIG2Globals, out var globalsStream))
            {
                using var memoryStream = new MemoryStream();

                using (var decodedStream = await globalsStream.OpenDecodedAsync(cancellationToken).ConfigureAwait(false))
                {
                    await decodedStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
                }

                globalsData = memoryStream.ToArray();
            }

            var pageData = await ImageHelper
                .GetContentAsync(imageDictionaryStream, cancellationToken)
                .ConfigureAwait(false);

            return Convert(globalsData, pageData, cancellationToken);
        }
#endif

        public override int GetHashCode() =>
            20523491 ^
            RuntimeHelpers.GetHashCode(imageDictionaryStream) ^
            colorSpace.GetHashCode();

        public override bool Equals(object? obj) =>
            obj is Jbig2Image jbigImage &&
            ReferenceEquals(jbigImage.imageDictionaryStream, imageDictionaryStream) &&
            jbigImage.colorSpace.Equals(colorSpace);
    }
}
