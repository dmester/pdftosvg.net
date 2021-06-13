// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LzwEncoder
{
    /// <summary>
    /// This encoder is not thoroughly tested and should not be used for production. It has only been used
    /// to produce test pdf files.
    /// </summary>
    internal class Encoder
    {
        private const int ClearTable = 256;
        private const int EndOfDecode = 257;
        private const int FirstDictionaryKey = 258;

        private const int MaxCode = 4095;
        private const int StartCodeLength = 9;

        public static byte[] Encode(byte[] input, bool earlyChange)
        {
            var packer = new BitPacker();
            var codeLength = StartCodeLength;
            packer.Write(ClearTable, codeLength);

            var table = new Dictionary<byte[], int>(new ByteArrayComparer());

            var nextCode = FirstDictionaryKey;

            var code = new byte[] { input[0] };

            for (var i = 1; i < input.Length; i++)
            {
                var nextByte = input[i];
                var newCode = Concat(code, nextByte);

                if (table.ContainsKey(newCode))
                {
                    code = newCode;
                }
                else
                {
                    packer.Write(code.Length == 1 ? code[0] : table[code], codeLength);
                    table.Add(newCode, nextCode++);

                    code = new byte[] { nextByte };

                    if (nextCode > MaxCode)
                    {
                        packer.Write(ClearTable, codeLength);
                        table.Clear();
                        codeLength = StartCodeLength;
                        nextCode = FirstDictionaryKey;
                    }
                    else if (nextCode == (1 << codeLength) + (earlyChange ? 0 : 1))
                    {
                        codeLength++;
                    }
                }
            }

            packer.Write(code.Length == 1 ? code[0] : table[code], codeLength);
            packer.Write(EndOfDecode, codeLength);

            return packer.ToArray();
        }

        private static byte[] Concat(byte[] a, byte b)
        {
            var result = new byte[a.Length + 1];
            Buffer.BlockCopy(a, 0, result, 0, a.Length);
            result[^1] = b;
            return result;
        }
    }
}
