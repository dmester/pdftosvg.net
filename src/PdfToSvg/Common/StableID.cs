// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Common
{
    internal static class StableID
    {
        private const int BufferSize = 1024;
        private const int IdLength = 7;

        private static readonly byte[] Separator = new byte[] { 0, 0 };

        public static string Generate(string prefix, object inputs)
        {
            using (var sha1 = SHA1.Create())
            {
                const string Chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

                var buffer = new byte[BufferSize];

#if NETSTANDARD1_6
                var memoryStream = new MemoryStream();

                Update(memoryStream, inputs, buffer);

                memoryStream.Position = 0;
                var hashBytes = sha1.ComputeHash(memoryStream);
#else
                using (var cryptoStream = new CryptoStream(Stream.Null, sha1, CryptoStreamMode.Write))
                {
                    Update(cryptoStream, inputs, buffer);
                }

                var hashBytes = sha1.Hash;
#endif

                var result = new char[IdLength];
                prefix.CopyTo(0, result, 0, prefix.Length);

                for (var i = prefix.Length; i < result.Length; i++)
                {
                    result[i] = Chars[hashBytes[i] % Chars.Length];
                }

                return new string(result);
            }
        }

        public static string Generate(string prefix, params object?[] inputs)
        {
            return Generate(prefix, (object)inputs);
        }

        private static void Update(Stream hashStream, object? input, byte[] buffer)
        {
            if (input != null)
            {
                string? sValue = null;

                if (input is string tmpStringValue)
                {
                    // Must be checked before IEnumerable, since string implements IEnumerable
                    sValue = tmpStringValue;
                }
                else if (input is byte[] byteArray)
                {
                    hashStream.Write(byteArray, 0, byteArray.Length);
                }
                else if (input is IEnumerable enumerable)
                {
                    foreach (var item in enumerable)
                    {
                        Update(hashStream, item, buffer);
                        hashStream.Write(Separator, 0, Separator.Length);
                    }
                }
                else if (input is double inputDbl)
                {
                    // This special case is to ensure the same id is generated on .NET Core and Framework.
                    // See https://devblogs.microsoft.com/dotnet/floating-point-parsing-and-formatting-improvements-in-net-core-3-0/
                    sValue = inputDbl.ToString("G15", CultureInfo.InvariantCulture);
                }
                else if (input is IFormattable formattable)
                {
                    sValue = formattable.ToString(null, CultureInfo.InvariantCulture);
                }
                else if (input is Stream stream)
                {
                    int read;

                    do
                    {
                        read = stream.Read(buffer, 0, buffer.Length);
                        hashStream.Write(buffer, 0, read);
                    }
                    while (read > 0);
                }
                else
                {
                    sValue = input.ToString();
                }

                if (sValue != null)
                {
                    var encoding = Encoding.Unicode;
                    var maxCharsPerIteration = buffer.Length >> 1;

                    for (var stringCursor = 0; stringCursor < sValue.Length;)
                    {
                        var charCount = Math.Min(sValue.Length - stringCursor, maxCharsPerIteration);
                        var bytesWritten = encoding.GetBytes(sValue, stringCursor, charCount, buffer, 0);

                        hashStream.Write(buffer, 0, bytesWritten);

                        stringCursor += charCount;
                    }
                }
            }
        }
    }
}
