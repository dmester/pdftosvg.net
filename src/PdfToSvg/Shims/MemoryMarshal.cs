// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if !NET5_0_OR_GREATER
namespace System.Runtime.InteropServices
{
    internal static class MemoryMarshal
    {
        public static ref T GetArrayDataReference<T>(T[] array)
        {
            return ref array[0];
        }
    }
}
#endif
