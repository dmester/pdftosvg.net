// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg
{
    public abstract class Image
    {
        internal Image(string contentType)
        {
            ContentType = contentType;
        }

        public string ContentType { get; }

        public abstract byte[] GetContent();

        public abstract Task<byte[]> GetContentAsync();

        public string ToDataUri()
        {
            return "data:" + ContentType + ";base64," + Convert.ToBase64String(GetContent());
        }

        public async Task<string> ToDataUriAsync()
        {
            return "data:" + ContentType + ";base64," + Convert.ToBase64String(await GetContentAsync());
        }
    }
}
