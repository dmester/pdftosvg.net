// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;


namespace System.Reflection
{
    internal static class TypeExtensions
    {
#if NET40
        public static Type GetTypeInfo(this Type type)
        {
            return type;
        }

        public static Delegate CreateDelegate(this MethodInfo methodInfo, Type delegateType)
        {
            return Delegate.CreateDelegate(delegateType, methodInfo);
        }

        public static Delegate CreateDelegate(this MethodInfo methodInfo, Type delegateType, object target)
        {
            return Delegate.CreateDelegate(delegateType, target, methodInfo);
        }

        public static MethodInfo GetMethodInfo(this Delegate del)
        {
            return del.Method;
        }
#endif

        public static TDelegate CreateDelegate<TDelegate>(this MethodInfo methodInfo)
        {
            return (TDelegate)(object)methodInfo.CreateDelegate(typeof(TDelegate));
        }

        public static TDelegate CreateDelegate<TDelegate>(this MethodInfo methodInfo, object target)
        {
            return (TDelegate)(object)methodInfo.CreateDelegate(typeof(TDelegate), target);
        }
    }
}
