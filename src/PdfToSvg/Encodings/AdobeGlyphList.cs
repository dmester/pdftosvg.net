// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Encodings
{
    internal static class AdobeGlyphList
    {
        private static readonly Dictionary<string, string> unicodeToGlyphName = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> glyphNameToUnicode = new Dictionary<string, string>();

        static AdobeGlyphList()
        {
            PopulateGlyphList("glyphlist.txt");

            // ITC Zapf Dingbats Glyph List
            // Symbols should only be mapped when Zapf Dingbats is used, but we will always map them,
            // since an invalid char will cause garbage output anyways.
            PopulateGlyphList("zapfdingbats.txt");
        }

        [Conditional("DEBUG")]
        private static void InvalidMapping(string line)
        {
            throw new Exception("The following AGL mapping line could not be parsed: " + line);
        }

        private static void PopulateGlyphList(string filename)
        {
            var type = typeof(AdobeGlyphList).GetTypeInfo();

            using var stream = type.Assembly.GetManifestResourceStreamOrThrow(type.FullName + "." + filename);
            using var reader = new StreamReader(stream);

            var partSeparators = new char[] { ';' };
            var charSeparators = new char[] { ' ' };

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("#"))
                {
                    continue;
                }

                var parts = line.Split(partSeparators, 2);
                if (parts.Length != 2)
                {
                    InvalidMapping(line);
                    continue;
                }

                var glyphName = parts[0];
                var unicodeHexChars = parts[1].Split(charSeparators);
                var unicodeChars = new char[unicodeHexChars.Length];
                var unicodeCharCount = 0;

                for (var i = 0; i < unicodeHexChars.Length; i++)
                {
                    if (ushort.TryParse(unicodeHexChars[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var unicodeValue))
                    {
                        unicodeChars[unicodeCharCount++] = (char)unicodeValue;
                    }
                    else
                    {
                        InvalidMapping(line);
                    }
                }

                if (unicodeCharCount == 0)
                {
                    InvalidMapping(line);
                    continue;
                }

                var unicode = new string(unicodeChars, 0, unicodeCharCount);

                glyphNameToUnicode[glyphName] = unicode;
                unicodeToGlyphName.TryAdd(unicode, glyphName);
            }
        }

        public static bool TryGetGlyphName(string unicode, [NotNullWhen(true)] out string? result)
        {
            if (unicode.Length > 0)
            {
                if (!unicodeToGlyphName.TryGetValue(unicode, out result))
                {
                    result = "uni";

                    for (var i = 0; i < unicode.Length; i++)
                    {
                        result += ((int)unicode[i]).ToString("X4");
                    }
                }

                return true;
            }

            result = null;
            return false;
        }

        public static bool TryGetUnicode(PdfName name, [NotNullWhen(true)] out string? result)
        {
            return TryGetUnicode(name.Value, out result);
        }

        public static bool TryGetUnicode(string? name, [NotNullWhen(true)] out string? result)
        {
            // Parsing is described in the spec available here:
            // https://github.com/adobe-type-tools/agl-specification#2-the-mapping

            result = null;

            if (name == null)
            {
                return false;
            }

            var firstPeriod = name.IndexOf('.');
            if (firstPeriod >= 0)
            {
                name = name.Substring(0, firstPeriod);
            }

            var components = name.Split('_');
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component.Length > 0)
                {
                    if (glyphNameToUnicode.TryGetValue(component, out var componentResult) ||
                        TryParseUnicode(component, out componentResult))
                    {
                        result += componentResult;
                    }
                }
            }

            return result != null;
        }

        private static bool TryParseUnicode(string value, out string? result)
        {
            result = null;

            if (value.Length < 5 || value[0] != 'u')
            {
                return false;
            }

            var isUni = false;
            var startHexAt = 1;

            if (value[1] == 'n' && value[2] == 'i')
            {
                isUni = true;
                startHexAt = 3;
            }

            // Is the remaining string hexadecimal?
            for (var i = startHexAt; i < value.Length; i++)
            {
                var ch = value[i];

                // Lower case hex not allowed according to spec, see
                // https://github.com/adobe-type-tools/agl-specification#2-the-mapping

                if ((ch < '0' || ch > '9') && (ch < 'A' || ch > 'F'))
                {
                    return false;
                }
            }

            if (isUni)
            {
                // "uni([0-9A-F]{4})*"
                if (((value.Length - 3) % 4) != 0)
                {
                    return false;
                }

                var resultChars = new char[(value.Length - 3) / 4];
                var resultCharCursor = 0;

                for (var i = startHexAt; i < value.Length; i += 4)
                {
                    var utf16HexValue = value.Substring(i, 4);

                    if (!ushort.TryParse(utf16HexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var utf16NumValue))
                    {
                        return false;
                    }

                    if (utf16NumValue > 0xD7FF && utf16NumValue < 0xE000)
                    {
                        return false;
                    }

                    resultChars[resultCharCursor++] = (char)utf16NumValue;
                }

                result = new string(resultChars);
            }
            else
            {
                // "u[0-9A-F]{4,6}"
                if (value.Length < 5 || value.Length > 7)
                {
                    return false;
                }

                if (!uint.TryParse(value.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var unicodeNumValue))
                {
                    return false;
                }

                if (unicodeNumValue > 0xD7FF && unicodeNumValue < 0xE000 || unicodeNumValue > 0x10FFFF)
                {
                    return false;
                }

                result = Utf16Encoding.EncodeCodePoint(unicodeNumValue);
            }

            return true;
        }
    }
}
