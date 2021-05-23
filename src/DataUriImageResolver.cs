using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg
{
    public class DataUriImageResolver : IImageResolver
    {
        public string ResolveImageUrl(Image image)
        {
            return image.ToDataUri();
        }
    }
}
