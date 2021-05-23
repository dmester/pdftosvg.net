using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg
{
    public interface IFontResolver
    {
        Font ResolveFont(string fontName);
    }
}
