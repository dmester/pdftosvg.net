// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.DocumentModel;
using PdfToSvg.Filters;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Parsing
{
    internal class InlineImageHelper
    {
        private const int VerifyFollowingTokenCount = 10;

        /// <summary>
        /// More restrictive comparison than <see cref="PdfCharacters.IsWhiteSpace(char)"/>, to minimize the risk of
        /// matching whitespace characters inside a binary stream.
        /// </summary>
        private static bool IsWhiteSpace(char ch)
        {
            return
                ch == ' ' ||
                ch == '\r' ||
                ch == '\n' ||
                ch == '\t';
        }

        private static bool IsWhiteSpaceOrEndOfStream(char ch)
        {
            return IsWhiteSpace(ch) || ch == BufferedReader.EndOfStreamMarker;
        }

        /// <summary>
        /// <para>
        ///     Tries to determine the length of an inline image data stream starting from the current position of the stream.
        /// </para>
        /// <para>
        ///     Note: Leaves the stream in an undefined position.
        /// </para>
        /// </summary>
        public static int DetectStreamLength(Stream stream, object filter)
        {
            return DetectStreamLength(stream as BufferedReader ?? new BufferedStreamReader(stream), filter);
        }

        /// <inheritdoc cref="DetectStreamLength(Stream, object)"/>
        public static int DetectStreamLength(BufferedReader reader, object? filterNames)
        {
            // Detecting length of inline image data is problematic, since the end marker is only two letter,
            // and according to the specification /Length is not specified on inline streams.

            // Prefer deterministic stream length detectors
            var outerFilterName =
                filterNames is PdfName singleFilterName ? singleFilterName :
                filterNames is object?[] filterNamesArray ? filterNamesArray[0] as PdfName :
                null;

            var outerFilter = Filter.ByName(outerFilterName);
            if (outerFilter.CanDetectStreamLength)
            {
                return outerFilter.DetectStreamLength(reader);
            }

            // For other filters, or no filter at all, we need to revert back to heuristics.
            // Find first index of <whitespace> 'E' 'I' <whitespace>
            // Then try to parse a few tokens to ensure the following data is valid.
            //
            // Let's have a look at how other libraries handle this case:
            //
            //  * Pdfium
            //    Looks for <whitespace> 'E' 'I' <whitespace>
            //    https://github.com/PDFium/PDFium/blob/ee6088e63bfdee81153ef1eeec9b90e42c87064f/core/src/fpdfapi/fpdf_page/fpdf_page_parser_new.cpp#L238
            //
            //  * PdfSharp
            //    Looks for <whitespace> 'E' 'I' <whitespace>
            //    https://github.com/empira/PDFsharp/blob/3205bd933b464d150c0f42e8bcdff3314b6c6164/src/PdfSharp/Pdf.Content/CLexer.cs#L178
            //
            //  * Pdf.js
            //    Looks for 'E' 'I' <whitespace> <10 ASCII characters>
            //    https://github.com/mozilla/pdf.js/blob/f6f335173d9b162120484db82c53c00b20697d4a/src/core/parser.js#L212
            //
            var streamLength = 0;

            while (reader.PeekChar() != BufferedReader.EndOfStreamMarker)
            {
                if (IsWhiteSpace(reader.PeekChar(1)) &&
                    reader.PeekChar(2) == 'E' &&
                    reader.PeekChar(3) == 'I' &&
                    IsWhiteSpaceOrEndOfStream(reader.PeekChar(4)))
                {
                    var originalPosition = reader.Position;

                    var invalidDataFound = false;

                    var followingLexer = new Lexer(reader);
                    for (var i = 0; i < VerifyFollowingTokenCount; i++)
                    {
                        var lexeme = followingLexer.Read();

                        if (lexeme.Token == Token.BeginImageData || // Start of another image
                            lexeme.Token == Token.EndOfInput)
                        {
                            break;
                        }

                        if (lexeme.Token == Token.UnexpectedCharacter)
                        {
                            invalidDataFound = true;
                            break;
                        }
                    }

                    reader.Position = originalPosition;

                    if (!invalidDataFound)
                    {
                        break;
                    }
                }

                reader.Skip();
                streamLength++;
            }

            return streamLength;
        }

        public static PdfDictionary DeabbreviateInlineImageDictionary(PdfDictionary dict)
        {
            DeabbreviateInlineImageValue(dict, true);
            return dict;
        }

        private static object? DeabbreviateInlineImageValue(object? value, bool isRootDictionary)
        {
            if (value is PdfDictionary dict)
            {
                foreach (var pair in dict.ToList())
                {
                    var deabbreviatedName = AbbreviatedNames.Translate(pair.Key, isRootDictionary);
                    var deabbreviatedValue = DeabbreviateInlineImageValue(pair.Value, false);

                    dict[pair.Key] = deabbreviatedValue;

                    if (!dict.ContainsKey(deabbreviatedName))
                    {
                        dict[deabbreviatedName] = deabbreviatedValue;
                    }
                }
            }
            else if (value is object?[] array)
            {
                for (var i = 0; i < array.Length; i++)
                {
                    array[i] = DeabbreviateInlineImageValue(array[i], false);
                }
            }
            else if (value is PdfName name)
            {
                value = AbbreviatedNames.Translate(name, false);
            }

            return value;
        }

    }
}
