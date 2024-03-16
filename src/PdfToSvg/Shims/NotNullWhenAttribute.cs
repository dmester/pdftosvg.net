// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if !NETSTANDARD2_1_OR_GREATER && !NET5_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    /// <exclude/>
    [AttributeUsage(AttributeTargets.Parameter)]
    internal class NotNullWhenAttribute : Attribute
    {
        public NotNullWhenAttribute(bool returnValue) { }
    }

    /// <exclude/>
    [AttributeUsage(AttributeTargets.Parameter)]
    internal class MaybeNullWhenAttribute : Attribute
    {
        public MaybeNullWhenAttribute(bool returnValue) { }
    }
}
#endif
