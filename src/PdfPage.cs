using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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

            return ToString(SvgRenderer.Convert(page, options));
        }

        public Task<string> ToSvgAsync()
        {
            return ToSvgAsync(new SvgConversionOptions());
        }

        public async Task<string> ToSvgAsync(SvgConversionOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            return ToString(await SvgRenderer.ConvertAsync(page, options));
        }

        private string ToString(XElement el)
        {
            using (var stringWriter = new StringWriter())
            {
                using (var writer = new SvgXmlWriter(stringWriter))
                {
                    el.WriteTo(writer);
                }

                return stringWriter.ToString();
            }
        }
    }
}
