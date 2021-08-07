// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Runtime.CompilerServices
{
    internal static class MethodInliningOptions
    {
#if NET40
        // AggressiveInlining doesn't exist in .NET 4.0. If is probably safer to set no flag than using `MethodImpl(256)`.
        public const MethodImplOptions AggressiveInlining = 0;
#else
        public const MethodImplOptions AggressiveInlining = MethodImplOptions.AggressiveInlining;
#endif
    }
}
