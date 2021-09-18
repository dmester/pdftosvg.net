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
    /// Represents errors that can occur while loading and converting a PDF file. This is the base class for all library specific
    /// exceptions.
    /// </summary>
    public class PdfException : Exception
    {
        /// <summary>
        /// Creates a new instance of a <see cref="PdfException"/>.
        /// </summary>
        /// <param name="message">Error message.</param>
        public PdfException(string message) : base(message)
        {
        }
    }
}
