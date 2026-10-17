// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.ColorSpaces;
using PdfToSvg.Common;
using PdfToSvg.DocumentModel;
using PdfToSvg.Drawing;
using PdfToSvg.Filters;
using PdfToSvg.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
        public static int DetectStreamLength(Stream stream, PdfDictionary imageDictionary,
            ResourceCache? resources = null, CancellationToken cancellationToken = default)
        {
            return DetectStreamLength(stream as BufferedReader ?? new BufferedStreamReader(stream), imageDictionary, resources, cancellationToken);
        }

        /// <inheritdoc cref="DetectStreamLength(Stream, PdfDictionary, ResourceCache?, CancellationToken)"/>
        public static int DetectStreamLength(BufferedReader reader, PdfDictionary imageDictionary,
            ResourceCache? resources = null, CancellationToken cancellationToken = default)
        {
            // Note: Dictionary keys are normalized when passed to DetectStreamLength => no abbreviated keys

            // PDF 2.0 has a /Length property
            // ISO 32000-2:2020 section 8.9.7 specifies 7096 as max length
            const int MaxLength = 4096;
            const int MaxLengthWithGrace = 2 * MaxLength;

            if (imageDictionary.TryGetInteger(Names.Length, out var fixedLength) &&
                fixedLength >= 0 &&
                fixedLength <= MaxLengthWithGrace &&
                fixedLength <= (int)(reader.Length - reader.Position))
            {
                return fixedLength;
            }

            var filterNames = imageDictionary[Names.Filter];

            var outerFilterName =
                filterNames is PdfName singleFilterName ? singleFilterName :
                filterNames is object?[] filterNamesArray ? filterNamesArray.FirstOrDefault() as PdfName :
                null;

            // Prefer deterministic stream length detectors
            if (outerFilterName == null)
            {
                var result = DetectUncompressedStreamLength(reader, imageDictionary, resources, cancellationToken);
                if (result >= 0)
                {
                    return result;
                }
            }
            else
            {
                var outerFilter = Filter.ByName(outerFilterName);
                if (outerFilter.CanDetectStreamLength)
                {
                    return outerFilter.DetectStreamLength(reader);
                }
            }

            return DetectHeuristicStreamLength(reader);
        }

        private static int DetectUncompressedStreamLength(BufferedReader reader,
            PdfDictionary imageDictionary, ResourceCache? resources, CancellationToken cancellationToken)
        {
            // Note: Dictionary keys are normalized when passed to DetectStreamLength => no abbreviated keys

            // Max size of images is 4096 bytes => we can limit the maximum dimension in any direction to 8 * 4096
            const int MinDimension = 1;
            const int MaxDimension = 8 * 4096;

            const int DefaultBitsPerComponent = 8;
            const int MinBitsPerComponent = 1;
            const int MaxBitsPerComponent = 16;

            const int Unknown = -1;

            if (!imageDictionary.TryGetInteger(Names.Width, out var width) ||
                !imageDictionary.TryGetInteger(Names.Height, out var height) ||
                width < MinDimension ||
                width > MaxDimension ||
                height < MinDimension ||
                height > MaxDimension)
            {
                return Unknown;
            }

            int bitsPerComponent;
            int componentsPerSample;

            // ISO 32000-2:2020 section 8.9.7:
            // Color space must be DeviceGray, DeviceRGB, or DeviceCMYK, unless ImageMask is specified

            var isStencilMask = imageDictionary.GetValueOrDefault(Names.ImageMask, false);
            if (isStencilMask)
            {
                bitsPerComponent = 1;
                componentsPerSample = 1;
            }
            else
            {
                bitsPerComponent = imageDictionary.GetValueOrDefault(Names.BitsPerComponent, DefaultBitsPerComponent);
                if (bitsPerComponent < MinBitsPerComponent ||
                    bitsPerComponent > MaxBitsPerComponent)
                {
                    return Unknown;
                }

                var colorSpaceName = imageDictionary.GetNameOrNull(Names.ColorSpace);
                if (colorSpaceName == Names.DeviceGray)
                {
                    componentsPerSample = 1;
                }
                else if (colorSpaceName == Names.DeviceRGB)
                {
                    componentsPerSample = 3;
                }
                else if (colorSpaceName == Names.DeviceCMYK)
                {
                    componentsPerSample = 4;
                }
                else if (
                    resources is not null &&
                    colorSpaceName is not null &&
                    resources.GetColorSpace(colorSpaceName, cancellationToken) is ColorSpace colorSpace &&
                    colorSpace is not UnsupportedColorSpace &&
                    colorSpace is not NullColorSpace)
                {
                    componentsPerSample = colorSpace.ComponentsPerSample;
                }
                else
                {
                    return Unknown;
                }
            }

            var bitCountPerLine = (long)width * componentsPerSample * bitsPerComponent;
            var byteCount = height * MathUtils.BitsToBytes(bitCountPerLine);

            var remainingBytes = reader.Length - reader.Position;
            if (remainingBytes < byteCount)
            {
                return Unknown;
            }

            return checked((int)byteCount);
        }

        private static int DetectHeuristicStreamLength(BufferedReader reader)
        {
            // Find first index of <whitespace> 'E' 'I' <whitespace>
            // Then try to parse a few tokens to ensure the following data is valid.
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
