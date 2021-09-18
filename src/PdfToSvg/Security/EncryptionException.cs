// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Security
{
    /// <summary>
    /// Represents errors that occurs when a document cannot be decrypted, usually
    /// because it is malformed, or using a non-supported security handler.
    /// </summary>
    internal class EncryptionException : PdfException
    {
        /// <summary>
        /// Creates a new instance of <see cref="EncryptionException"/>.
        /// </summary>
        public EncryptionException(string message) : base(message)
        {
        }
    }
}
