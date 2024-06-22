// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using InputDictData = System.Collections.Generic.Dictionary<int, double[]>;
using OutputDictData = System.Collections.Generic.IList<System.Collections.Generic.KeyValuePair<int, double[]>>;

namespace PdfToSvg.Fonts.CompactFonts
{
    internal static class CompactFontDictSerializer
    {
        private static class Serializer
            <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TDict>
            where TDict : notnull
        {
            private class SerializedProperty
            {
                public readonly int Value;

                private readonly PropertyInfo propertyInfo;

                public SerializedProperty(int value, PropertyInfo propertyInfo)
                {
                    Value = value;
                    this.propertyInfo = propertyInfo;
                }

                public Type Type => propertyInfo.PropertyType;
                public object? GetValue(object obj) => propertyInfo.GetValue(obj);
                public void SetValue(object obj, object? value) => propertyInfo.SetValue(obj, value);
            }

            private static SerializedProperty[] properties = typeof(TDict).GetTypeInfo()
                .GetProperties()

                .Select(p => new
                {
                    Property = p,
                    Attribute = (CompactFontDictOperatorAttribute)p
                        .GetCustomAttributes(typeof(CompactFontDictOperatorAttribute), false)
                        .FirstOrDefault()!
                })
                .Where(p => p.Attribute != null)

                .OrderBy(p => p.Attribute.Order)
                .ThenBy(p => p.Attribute.Value)

                .Select(p => new SerializedProperty(p.Attribute.Value, p.Property))
                .ToArray();


            public static void Deserialize(TDict target, InputDictData dictData, CompactFontStringTable strings)
            {
                foreach (var property in properties)
                {
                    if (dictData.TryGetValue(property.Value, out var sourceValue))
                    {
                        var convertedValue = ConvertValue(sourceValue, strings, property.Type);
                        property.SetValue(target, convertedValue);
                    }
                }
            }

            private static object? ConvertValue(double[] sourceValue, CompactFontStringTable strings, Type targetType)
            {
                if (targetType.IsArray)
                {
                    var elementType = targetType.GetElementType();
                    if (elementType != null)
                    {
                        return ConvertArrayValue(sourceValue, strings, elementType);
                    }
                }

                if (sourceValue.Length > 0)
                {
                    return ConvertSingleValue(sourceValue[0], strings, targetType);
                }

                return GetDefaultValue(targetType);
            }

            private static object? GetDefaultValue(Type targetType)
            {
                if (targetType == typeof(bool))
                {
                    return false;
                }

                if (targetType == typeof(int))
                {
                    return 0;
                }

                if (targetType == typeof(double))
                {
                    return double.NaN;
                }

                if (!targetType.GetTypeInfo().IsValueType || Nullable.GetUnderlyingType(targetType) != null)
                {
                    return null;
                }

                throw new CompactFontException("Unsupported DICT property data type " + targetType.FullName + ".");
            }

            private static object? ConvertSingleValue(double sourceValue, CompactFontStringTable strings, Type targetType)
            {
                if (targetType == typeof(string))
                {
                    return strings.Lookup((int)sourceValue);
                }

                if (targetType == typeof(double))
                {
                    return sourceValue;
                }

                if (targetType == typeof(bool))
                {
                    return sourceValue != 0;
                }

                if (targetType == typeof(int))
                {
                    return (int)sourceValue;
                }

                var nonNullableTargetType = Nullable.GetUnderlyingType(targetType);
                if (nonNullableTargetType != null)
                {
                    return ConvertSingleValue(sourceValue, strings, nonNullableTargetType);
                }

                throw new CompactFontException("Unsupported DICT property data type " + targetType.FullName + ".");
            }

            private static Array ConvertArrayValue(double[] inputArray, CompactFontStringTable strings, Type targetElementType)
            {
                var outputArray = ArrayUtils.CreateInstance(targetElementType, inputArray.Length);

                for (var i = 0; i < inputArray.Length; i++)
                {
                    var convertedValue = ConvertSingleValue(inputArray[i], strings, targetElementType);
                    outputArray.SetValue(convertedValue, i);
                }

                return outputArray;
            }

            public static void Serialize(OutputDictData target,
                TDict dict, TDict defaultValues,
                CompactFontStringTable strings, bool readOnlyStrings)
            {
                foreach (var property in properties)
                {
                    var sourceValue = property.GetValue(dict);
                    var defaultValue = property.GetValue(defaultValues);

                    if (sourceValue != null && !ValueEquals(sourceValue, defaultValue))
                    {
                        var convertedValue = ConvertValue(sourceValue, strings, readOnlyStrings);
                        target.Add(new KeyValuePair<int, double[]>(property.Value, convertedValue));
                    }
                }
            }

            private static bool ValueEquals(object? a, object? b)
            {
                if (Equals(a, b))
                {
                    return true;
                }

                if (a is Array arrA && b is Array arrB && arrA.Length == arrB.Length)
                {
                    for (var i = 0; i < arrA.Length; i++)
                    {
                        if (!ValueEquals(arrA.GetValue(i), arrB.GetValue(i)))
                        {
                            return false;
                        }
                    }

                    return true;
                }

                return false;
            }

            private static double[] ConvertValue(object? value, CompactFontStringTable strings, bool readOnlyStrings)
            {
                if (value is Array arr)
                {
                    return ConvertArrayValue(arr, strings, readOnlyStrings);
                }
                else
                {
                    var convertedValue = ConvertSingleValue(value, strings, readOnlyStrings);
                    return new[] { convertedValue };
                }
            }

            private static double[] ConvertArrayValue(Array inputArray, CompactFontStringTable strings, bool readOnlyStrings)
            {
                var outputArray = new double[inputArray.Length];

                for (var i = 0; i < outputArray.Length; i++)
                {
                    var item = inputArray.GetValue(i);
                    outputArray[i] = ConvertSingleValue(item, strings, readOnlyStrings);
                }

                return outputArray;
            }

            private static double ConvertSingleValue(object? value, CompactFontStringTable strings, bool readOnlyStrings)
            {
                if (value is string strValue)
                {
                    return readOnlyStrings
                        ? strings.Lookup(strValue)
                        : strings.AddOrLookup(strValue);
                }

                if (value is bool boolValue)
                {
                    return boolValue ? 1 : 0;
                }

                if (value is int intValue)
                {
                    return intValue;
                }

                if (value is double dblValue)
                {
                    return dblValue;
                }

                throw new CompactFontException("Unsupported DICT property data type " + value?.GetType().FullName + ".");
            }

        }

        public static void Deserialize
            <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TDict>
            (
                TDict target,
                InputDictData dictData,
                CompactFontStringTable strings
            )
            where TDict : notnull
        {
            Serializer<TDict>.Deserialize(target, dictData, strings);
        }

        public static void Serialize
            <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TDict>
            (
                OutputDictData target,
                TDict dict, TDict defaultValues,
                CompactFontStringTable strings, bool readOnlyStrings
            )
            where TDict : notnull
        {
            Serializer<TDict>.Serialize(target, dict, defaultValues, strings, readOnlyStrings);
        }
    }
}
