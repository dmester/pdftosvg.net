// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace PdfToSvg.Common
{
    internal static class New
    {
        /// <remarks>
        /// Reason for the existence of this method:
        ///
        /// The .NET 5.0 definition of the constructor does not accept null parameters:
        ///
        /// <code>
        /// new XElement(XName name, params object[] content)
        /// </code>
        ///
        /// This is fixed in .NET 6.0:
        ///
        /// <code>
        /// new XElement(XName name, params object?[] content)
        /// </code>
        ///
        /// ...but it does not help us targetting .NET 5.
        /// </remarks>
        public static XElement XElement(XName name, params object?[] content) => new XElement(name, (object)content);
    }
}
