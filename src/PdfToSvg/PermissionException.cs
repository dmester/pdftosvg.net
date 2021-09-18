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
    /// Represents errors that occurs when an attempt is made to perform an operation not allowed by the document author.
    /// </summary>
    public class PermissionException : PdfException
    {
        /// <summary>
        /// Creates a new instance of a <see cref="PermissionException"/>.
        /// </summary>
        /// <param name="message">Error message.</param>
        public PermissionException(string message) : base(message)
        {
        }
    }
}
