// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Common
{
    internal static class StableID
    {
        public static string Generate(string prefix, IEnumerable inputs)
        {
            var sb = new StringBuilder();

            if (inputs != null)
            {
                foreach (var input in inputs)
                {
                    if (input != null)
                    {
                        if (input is double inputDbl)
                        {
                            // This special case is to ensure the same id is generated on .NET Core and Framework.
                            // See https://devblogs.microsoft.com/dotnet/floating-point-parsing-and-formatting-improvements-in-net-core-3-0/
                            sb.Append(inputDbl.ToString("G15", CultureInfo.InvariantCulture));
                        }
                        else if (input is IFormattable formattable)
                        {
                            sb.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(input.ToString());
                        }
                    }

                    sb.Append('\0');
                }
            }

            return Generate(prefix, sb.ToString());
        }

        public static string Generate(string prefix, params object?[] inputs)
        {
            return Generate(prefix, (IEnumerable)inputs);
        }

        public static string Generate(string prefix, string input)
        {
            using (var sha1 = SHA1.Create())
            {
                const string Chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

                var inputBytes = Encoding.Unicode.GetBytes(input);
                var hashBytes = sha1.ComputeHash(inputBytes);

                var result = new char[7];
                prefix.CopyTo(0, result, 0, prefix.Length);

                for (var i = prefix.Length; i < result.Length; i++)
                {
                    result[i] = Chars[hashBytes[i] % Chars.Length];
                }

                return new string(result);
            }
        }
    }
}
