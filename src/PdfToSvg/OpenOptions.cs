// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

namespace PdfToSvg
{
    /// <summary>
    /// Provides additional configuration options for opening a <see cref="PdfDocument"/>.
    /// </summary>
    public class OpenOptions
    {
        /// <summary>
        /// The password used for opening the PDF. This can be either the user password or the owner password. If the
        /// owner password is specified, any usage restrictions are bypassed.
        /// </summary>
        public string? Password { get; set; }
    }
}
