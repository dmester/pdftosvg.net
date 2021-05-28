using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Common
{
    internal static class ArrayExtensions
    {
        public static T[] Slice<T>(this T[] source, int offset, int length)
        {
            var result = new T[length];
            Array.Copy(source, offset, result, 0, length);
            return result;
        }
    }
}
