// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;

namespace PdfToSvg.Svg
{
    /// <summary>
    /// Inheritable copy-on-write style
    /// </summary>
    internal struct SvgElementStyle
    {
        // Must match the number of entries in ValueNames
        private const int ValueCount = 18;

        private static readonly string[] ValueNames =
        [
            "clip-path",
            "clip-rule",
            "transform",
            "class",
            "fill-rule",
            "fill",
            "fill-opacity",
            "opacity",
            "style",
            "stroke",
            "stroke-width",
            "stroke-opacity",
            "stroke-linecap",
            "stroke-linejoin",
            "stroke-miterlimit",
            "stroke-dasharray",
            "stroke-dashoffset",
            "image-rendering",
        ];

        private const int ClipPathIndex = 0;
        private const int ClipRuleIndex = 1;
        private const int TransformIndex = 2;
        private const int ClassIndex = 3;
        private const int FillRuleIndex = 4;
        private const int FillIndex = 5;
        private const int FillOpacityIndex = 6;
        private const int OpacityIndex = 7;
        private const int StyleIndex = 8;
        private const int StrokeIndex = 9;
        private const int StrokeWidthIndex = 10;
        private const int StrokeOpacityIndex = 11;
        private const int StrokeLineCapIndex = 12;
        private const int StrokeLineJoinIndex = 13;
        private const int StrokeMiterLimitIndex = 14;
        private const int StrokeDashArrayIndex = 15;
        private const int StrokeDashOffsetIndex = 16;
        private const int ImageRenderingIndex = 17;

        private Storage? storage;

        private class Storage
        {
            public readonly string?[] values = new string?[ValueCount];
            public int firstIndex = ValueCount;
            public bool isSealed;
        }

        public bool IsEmpty => storage == null || storage.firstIndex == storage.values.Length;

        public string? Class
        {
            get => GetStyle(ClassIndex);
            set => SetStyle(ClassIndex, value);
        }

        /// <summary>
        /// Note: Value is passed unencoded as-is to the XML.
        /// </summary>
        public string? Style
        {
            get => GetStyle(StyleIndex);
            set => SetStyle(StyleIndex, value);
        }

        /// <summary>
        /// Note: Value is passed unencoded as-is to the XML.
        /// </summary>
        public string? ClipPath
        {
            get => GetStyle(ClipPathIndex);
            set => SetStyle(ClipPathIndex, value);
        }

        public string? ClipRule
        {
            get => GetStyle(ClipRuleIndex);
            set => SetStyle(ClipRuleIndex, value);
        }

        public string? Opacity
        {
            get => GetStyle(OpacityIndex);
            set => SetStyle(OpacityIndex, value);
        }

        /// <summary>
        /// Note: Value is passed unencoded as-is to the XML.
        /// </summary>
        public string? Fill
        {
            get => GetStyle(FillIndex);
            set => SetStyle(FillIndex, value);
        }

        public string? FillOpacity
        {
            get => GetStyle(FillOpacityIndex);
            set => SetStyle(FillOpacityIndex, value);
        }

        public string? FillRule
        {
            get => GetStyle(FillRuleIndex);
            set => SetStyle(FillRuleIndex, value);
        }

        /// <summary>
        /// Note: Value is passed unencoded as-is to the XML.
        /// </summary>
        public string? Stroke
        {
            get => GetStyle(StrokeIndex);
            set => SetStyle(StrokeIndex, value);
        }

        public string? StrokeWidth
        {
            get => GetStyle(StrokeWidthIndex);
            set => SetStyle(StrokeWidthIndex, value);
        }

        public string? StrokeOpacity
        {
            get => GetStyle(StrokeOpacityIndex);
            set => SetStyle(StrokeOpacityIndex, value);
        }

        public string? StrokeLineCap
        {
            get => GetStyle(StrokeLineCapIndex);
            set => SetStyle(StrokeLineCapIndex, value);
        }

        public string? StrokeLineJoin
        {
            get => GetStyle(StrokeLineJoinIndex);
            set => SetStyle(StrokeLineJoinIndex, value);
        }

        public string? StrokeMiterLimit
        {
            get => GetStyle(StrokeMiterLimitIndex);
            set => SetStyle(StrokeMiterLimitIndex, value);
        }

        public string? StrokeDashArray
        {
            get => GetStyle(StrokeDashArrayIndex);
            set => SetStyle(StrokeDashArrayIndex, value);
        }

        public string? StrokeDashOffset
        {
            get => GetStyle(StrokeDashOffsetIndex);
            set => SetStyle(StrokeDashOffsetIndex, value);
        }

        public string? ImageRendering
        {
            get => GetStyle(ImageRenderingIndex);
            set => SetStyle(ImageRenderingIndex, value);
        }

        public string? Transform
        {
            get => GetStyle(TransformIndex);
            set => SetStyle(TransformIndex, value);
        }

        /// <summary>
        /// All subsequent writes to this instance or copied instances will be isolated after being forked.
        /// </summary>
        public void Fork()
        {
            var storage = this.storage;
            if (storage != null)
            {
                storage.isSealed = true;
            }
        }

        public override int GetHashCode()
        {
            if (storage == null || storage.firstIndex == storage.values.Length)
            {
                return 0;
            }

            return 0x6241_5d69 ^
                storage.firstIndex ^
                storage.values[storage.firstIndex]?.GetHashCode() ?? 0;
        }

        public void WriteTo(SvgTextWriter writer)
        {
            var storage = this.storage;
            if (storage != null)
            {
                var allocatedMap = storage.values;

                for (var i = storage.firstIndex; i < allocatedMap.Length; i++)
                {
                    var value = allocatedMap[i];
                    if (value is not null)
                    {
                        writer.WriteAscii(' ');
                        writer.WriteAscii(ValueNames[i]);
                        writer.WriteAscii('=', '"');
                        writer.WriteRawText(value);
                        writer.WriteAscii('"');
                    }
                }
            }
        }

        private string? GetStyle(int index)
        {
            return this.storage?.values[index];
        }

        private void SetStyle(int index, string? value)
        {
            var storage = this.storage;

            if (storage == null)
            {
                if (value == null)
                {
                    return;
                }

                this.storage = storage = new Storage();
            }
            else if (storage.isSealed)
            {
                if (storage.values[index] == value)
                {
                    return;
                }

                // Copy-on-write
                var copy = new Storage();
                Array.Copy(storage.values, copy.values, ValueCount);
                copy.firstIndex = storage.firstIndex;

                this.storage = storage = copy;
            }

            storage.values[index] = value;

            if (value == null)
            {
                if (storage.firstIndex == index)
                {
                    var allocatedMap = storage.values;
                    var firstIndex = storage.firstIndex;

                    while (
                        firstIndex < allocatedMap.Length &&
                        allocatedMap[firstIndex] is null)
                    {
                        firstIndex++;
                    }

                    storage.firstIndex = firstIndex;
                }
            }
            else if (storage.firstIndex > index)
            {
                storage.firstIndex = index;
            }
        }

        public static bool operator ==(SvgElementStyle a, SvgElementStyle b)
        {
            if (ReferenceEquals(a.storage, b.storage))
            {
                return true;
            }
            if (a.storage is null)
            {
                return b.IsEmpty;
            }
            if (b.storage is null)
            {
                return a.IsEmpty;
            }

            if (a.storage.firstIndex != b.storage.firstIndex)
            {
                return false;
            }

            var mapa = a.storage.values;
            var mapb = b.storage.values;

            for (var i = a.storage.firstIndex; i < mapa.Length; i++)
            {
                if (mapa[i] != mapb[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool operator !=(SvgElementStyle a, SvgElementStyle b)
        {
            return !(a == b);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is SvgElementStyle otherStyle && this == otherStyle;
        }
    }
}
