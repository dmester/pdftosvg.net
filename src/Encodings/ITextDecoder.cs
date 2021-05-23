using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Encodings
{
    internal interface ITextDecoder
    {
        CharacterCode GetCharacter(PdfString value, int index);
    }
}
