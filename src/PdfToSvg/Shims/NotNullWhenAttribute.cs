// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if !HAVE_NULLABLE
namespace System.Diagnostics.CodeAnalysis
{
    /// <exclude/>
    [AttributeUsage(AttributeTargets.Parameter)]
    internal class NotNullWhenAttribute : Attribute
    {
        public NotNullWhenAttribute(bool returnValue) { }
    }
}
#endif
