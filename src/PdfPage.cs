using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg
{
    public class PdfPage
    {
        private readonly PdfDocument owner;
        private readonly PdfDictionary page;

        internal PdfPage(PdfDocument owner, PdfDictionary page)
        {
            this.owner = owner;
            this.page = page;
        }

        public PdfDocument Document => owner;

        public string ToSvg()
        {
            return ToSvg(new SvgConversionOptions());
        }

        public string ToSvg(SvgConversionOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            return SvgRenderer.Convert(page, options).ToString();
        }

        public Task<string> ToSvgAsync()
        {
            return ToSvgAsync(new SvgConversionOptions());
        }

        public async Task<string> ToSvgAsync(SvgConversionOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            return (await SvgRenderer.ConvertAsync(page, options)).ToString();
        }
    }
}
