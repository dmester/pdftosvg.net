// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PdfToSvg.Encodings
{
    internal class CMap : ITextDecoder
    {
        // CMap specification
        // https://wwwimages2.adobe.com/content/dam/acom/en/devnet/font/pdfs/5014.CIDFont_Spec.pdf
        //
        // ToUnicode Mapping File Tutorial
        // https://web.archive.org/web/20210303045910/https://www.adobe.com/content/dam/acom/en/devnet/acrobat/pdfs/5411.ToUnicode.pdf


        private readonly Dictionary<uint, string>[] mappingsByCodeLength;

        public CMap()
        {
            // Max code length is 4 bytes according to PDF spec 1.7, 9.7.6.2.
            // Waste one element to make the indexes one-based.
            mappingsByCodeLength = new Dictionary<uint, string>[5];

            for (var i = 1; i < mappingsByCodeLength.Length; i++)
            {
                mappingsByCodeLength[i] = new Dictionary<uint, string>();
            }
        }

        public void AddBfChar(PdfString srcCode, PdfString dstCode)
        {
            if (srcCode.Length > 0 && srcCode.Length < mappingsByCodeLength.Length)
            {
                var mappings = mappingsByCodeLength[srcCode.Length];
                var intSrcCode = ReadCode(srcCode, 0, srcCode.Length);
                mappings[intSrcCode] = dstCode.ToString(Encoding.BigEndianUnicode);
            }
            else
            {
                Log.WriteLine($"Tried to add a bfchar with an invalid source code length ({srcCode.Length}). It will be ignored.");
            }
        }

        public void AddBfRange(PdfString srcCodeLo, PdfString srcCodeHi, IList<PdfString> dstStrings)
        {
            if (srcCodeLo.Length > 0 && srcCodeLo.Length < mappingsByCodeLength.Length &&
                srcCodeHi.Length == srcCodeLo.Length)
            {
                var mappings = mappingsByCodeLength[srcCodeLo.Length];

                var intSrcCodeLo = ReadCode(srcCodeLo, 0, srcCodeLo.Length);
                var intSrcCodeHi = ReadCode(srcCodeHi, 0, srcCodeHi.Length);

                // Should be the same, but if the range would happen to be malformed, use as much as possible
                var count = Math.Min(dstStrings.Count, intSrcCodeHi - intSrcCodeLo + 1);

                for (var i = 0; i < count; i++)
                {
                    mappings[intSrcCodeLo + unchecked((uint)i)] = dstStrings[i].ToString(Encoding.BigEndianUnicode);
                }
            }
            else
            {
                Log.WriteLine($"Tried to add a bfrange with an invalid source code length ({srcCodeLo.Length}, {srcCodeHi.Length}). It will be ignored.");
            }
        }

        public void AddBfRange(PdfString srcCodeLo, PdfString srcCodeHi, PdfString dstStringLo)
        {
            if (srcCodeLo.Length > 0 && srcCodeLo.Length < mappingsByCodeLength.Length &&
                srcCodeHi.Length == srcCodeLo.Length)
            {
                var mappings = mappingsByCodeLength[srcCodeLo.Length];

                var intSrcCodeLo = ReadCode(srcCodeLo, 0, srcCodeLo.Length);
                var intSrcCodeHi = ReadCode(srcCodeHi, 0, srcCodeHi.Length);

                var strDstStringLo = dstStringLo.ToString(Encoding.BigEndianUnicode);
                if (intSrcCodeHi > intSrcCodeLo)
                {
                    var charsDstStringLo = strDstStringLo.ToCharArray();
                    var lastChar = charsDstStringLo[charsDstStringLo.Length - 1];

                    for (var srcCode = intSrcCodeLo; srcCode <= intSrcCodeHi; srcCode++)
                    {
                        mappings[srcCode] = new string(charsDstStringLo);

                        // The last byte must not increment past 255 according to PDF spec 1.7, 9.10.3, page 303.
                        // Otherwise, the mapping is undefined.
                        lastChar = unchecked((char)(lastChar + 1));
                        charsDstStringLo[charsDstStringLo.Length - 1] = lastChar;
                    }
                }
                else if (intSrcCodeLo == intSrcCodeHi)
                {
                    mappings[intSrcCodeLo] = strDstStringLo;
                }
            }
            else
            {
                Log.WriteLine($"Tried to add a bfrange with an invalid source code length ({srcCodeLo.Length}, {srcCodeHi.Length}). It will be ignored.");
            }
        }

        private static uint ReadCode(PdfString str, int index, int codeLength)
        {
            var result = 0u;

            for (var i = 0; i < codeLength; i++)
            {
                result = (result << 8) | str[index + i];
            }

            return result;
        }

        public CharacterCode GetCharacter(PdfString value, int index)
        {
            var code = 0u;
            var maxCodeLength = Math.Min(value.Length - index, mappingsByCodeLength.Length - 1);

            for (var codeLength = 1; codeLength <= maxCodeLength; codeLength++)
            {
                code = (code << 8) | value[index + codeLength - 1];

                if (mappingsByCodeLength[codeLength].TryGetValue(code, out var dstString))
                {
                    return new CharacterCode(code, codeLength, dstString);
                }
            }

            return new CharacterCode();
        }

        public IDictionary<uint, string> ToLookup()
        {
            var result = new Dictionary<uint, string>();

            for (var codeLength = 1; codeLength < mappingsByCodeLength.Length; codeLength++)
            {
                foreach (var mapping in mappingsByCodeLength[codeLength])
                {
                    result[mapping.Key] = mapping.Value;
                }
            }

            return result;
        }
    }
}
