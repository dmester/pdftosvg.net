// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

#if NET40
namespace System.Reflection
{
    internal static class TypeExtensions
    {
        public static Type GetTypeInfo(this Type type)
        {
            return type;
        }

        public static Delegate CreateDelegate(this MethodInfo methodInfo, Type delegateType)
        {
            return Delegate.CreateDelegate(delegateType, methodInfo);
        }
    }
}
#endif
