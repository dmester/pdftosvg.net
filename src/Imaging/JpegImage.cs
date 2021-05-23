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

        public JpegImage(PdfDictionary imageDictionary) : base("image/jpeg")
        {
            this.imageDictionary = imageDictionary;
        }

        private Stream GetStream()
        {
            var filters = imageDictionary.Stream.Filters;

            var readStream = imageDictionary.Stream.Open();
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
                await jpegStream.CopyToAsync(memoryStream);
            }

            return memoryStream.ToArray();
        }
    }
}
