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
    /// <summary>
    /// Thrown when an encrypted PDF is opened.
    /// </summary>
    public class EncryptedPdfException : Exception
    {
        /// <summary>
        /// Creates a new instance of <see cref="EncryptedPdfException"/>.
        /// </summary>
        public EncryptedPdfException() :
            base("The specified PDF file is encrypted. Encrypted PDF files are currently not supported.")
        {
        }
    }
}
