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
    /// Represents errors that occur when a document is password protected, but an incorrect password is specified
    /// or not specified at all.
    /// </summary>
    public class InvalidCredentialException : PdfException
    {
        /// <summary>
        /// Creates a new instance of <see cref="InvalidCredentialException"/>.
        /// </summary>
        public InvalidCredentialException(string message) : base(message)
        {
        }
    }
}
