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
    /// Represents errors that can occur while loading and converting a PDF file.
    /// </summary>
    /// <remarks>
    /// This is the base class for all PdfToSvg.NET specific exceptions.
    /// </remarks>
    public class PdfConversionException : Exception
    {
        /// <summary>
        /// Creates a new instance of a <see cref="PdfConversionException"/>.
        /// </summary>
        /// <param name="message">Error message.</param>
        public PdfConversionException(string message) : base(message)
        {
        }
    }
}
