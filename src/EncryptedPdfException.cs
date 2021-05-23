using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg
{
    public class EncryptedPdfException : Exception
    {
        public EncryptedPdfException() : 
            base("The specified PDF file is encrypted. Encrypted PDF files are currently not supported.") 
        {
        }
    }
}
