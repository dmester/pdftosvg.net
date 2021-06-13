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

namespace PdfToSvg.Imaging
{
    internal class JpegImage : Image
    {
        private readonly PdfDictionary imageDictionary;
        private readonly PdfStream imageDictionaryStream;

        public JpegImage(PdfDictionary imageDictionary) : base("image/jpeg")
        {
            if (imageDictionary.Stream == null)
            {
                throw new ArgumentException("There was no data stream attached to the image dictionary.", nameof(imageDictionary));
            }

            this.imageDictionary = imageDictionary;
            this.imageDictionaryStream = imageDictionary.Stream;
        }

        private Stream GetStream()
        {
            var filters = imageDictionaryStream.Filters;

            var readStream = imageDictionaryStream.Open();
            try
            {
                return filters.Take(filters.Count - 1).Decode(readStream);
            }
            catch
            {
                readStream.Dispose();
                throw;
            }
        }

        public override byte[] GetContent()
        {
            var memoryStream = new MemoryStream();

            using (var jpegStream = GetStream())
            {
                jpegStream.CopyTo(memoryStream);
            }

            return memoryStream.ToArray();
        }

        public override async Task<byte[]> GetContentAsync()
        {
            var memoryStream = new MemoryStream();

            using (var jpegStream = GetStream())
            {
                await jpegStream.CopyToAsync(memoryStream).ConfigureAwait(false);
            }

            return memoryStream.ToArray();
        }
    }
}
