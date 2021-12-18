// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using DictData = System.Collections.Generic.Dictionary<int, double[]>;

namespace PdfToSvg.Fonts.CompactFonts
{
    internal static class CompactFontDictSerializer
    {
        private static object lockObject = new object();

        private static MethodInfo objectEqualsMethod = typeof(object)
            .GetTypeInfo()
            .GetMethod(nameof(Equals), new[] { typeof(object), typeof(object) });

        private static MethodInfo arrayEqualsMethod = typeof(CompactFontDictSerializer)
            .GetTypeInfo()
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(method => method.Name == nameof(EqualArrays));

        private static bool EqualArrays<T>(T[]? a, T[]? b)
        {
            if (a == null) return b == null;
            if (b == null) return false;

            if (a.Length != b.Length) return false;

            for (var i = 0; i < a.Length; i++)
            {
                if (!Equals(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static class Deserializer<TDict>
        {
            private static Action<TDict, DictData, CompactFontStringTable>? deserializer;

            public static void Deserialize(TDict target, DictData dictData, CompactFontStringTable strings)
            {
                if (deserializer == null)
                {
                    lock (lockObject)
                    {
                        if (deserializer == null)
                        {
                            deserializer = Compile();
                        }
                    }
                }

                deserializer(target, dictData, strings);
            }

            private static Action<TDict, DictData, CompactFontStringTable> Compile()
            {
                var targetParam = Expression.Parameter(typeof(TDict), "target");
                var sourceParam = Expression.Parameter(typeof(DictData), "source");
                var stringsParam = Expression.Parameter(typeof(CompactFontStringTable), "strings");

                var tryGetValue = typeof(DictData).GetTypeInfo().GetMethod(nameof(DictData.TryGetValue));

                var sourceElementValue = Expression.Variable(typeof(double[]));
                var body = new List<Expression>();

                foreach (var property in typeof(TDict).GetTypeInfo().GetProperties())
                {
                    var attribute = (CompactFontDictOperatorAttribute?)property
                        .GetCustomAttributes(typeof(CompactFontDictOperatorAttribute), false)
                        .FirstOrDefault();

                    if (attribute == null)
                    {
                        continue;
                    }

                    body.Add(Expression.IfThen(
                        Expression.Call(sourceParam, tryGetValue, Expression.Constant(attribute.Value), sourceElementValue),
                        Expression.Call(targetParam, property.GetSetMethod(true), ConvertValue(sourceElementValue, stringsParam, property.PropertyType))
                        ));
                }

                var lambda = Expression
                    .Lambda<Action<TDict, DictData, CompactFontStringTable>>(
                        Expression.Block(new[] { sourceElementValue }, body),
                        targetParam, sourceParam, stringsParam);

                return lambda.Compile();
            }

            private static Expression ConvertValue(Expression sourceValue, Expression strings, Type targetType)
            {
                if (targetType.IsArray)
                {
                    return ConvertArrayValue(sourceValue, strings, targetType.GetElementType());
                }
                else
                {
                    var firstOrDefault = Expression.Condition(
                        Expression.GreaterThan(Expression.Property(sourceValue, nameof(Array.Length)), Expression.Constant(0)),
                        ConvertSingleValue(
                            Expression.ArrayAccess(sourceValue, Expression.Constant(0)),
                            strings, targetType),
                        GetDefaultValue(targetType));

                    return firstOrDefault;
                }
            }

            private static Expression GetDefaultValue(Type targetType)
            {
                if (targetType == typeof(bool))
                {
                    return Expression.Constant(false, targetType);
                }

                if (targetType == typeof(int))
                {
                    return Expression.Constant(0, targetType);
                }

                if (targetType == typeof(double))
                {
                    return Expression.Constant(double.NaN, targetType);
                }

                if (!targetType.GetTypeInfo().IsValueType || Nullable.GetUnderlyingType(targetType) != null)
                {
                    return Expression.Constant(null, targetType);
                }

                throw new CompactFontException("Unsupported DICT property data type " + targetType.FullName + ".");
            }

            private static Expression ConvertSingleValue(Expression sourceValue, Expression strings, Type targetType)
            {
                if (sourceValue.Type != typeof(double))
                {
                    throw new ArgumentException("Expected " + nameof(sourceValue) + " of type double.", nameof(sourceValue));
                }

                if (targetType == typeof(string))
                {
                    return Expression.Call(strings,
                        methodName: nameof(CompactFontStringTable.Lookup),
                        typeArguments: null,
                        arguments: Expression.Convert(sourceValue, typeof(int)));
                }

                if (targetType == typeof(double))
                {
                    return sourceValue;
                }

                if (targetType == typeof(bool))
                {
                    return Expression.NotEqual(sourceValue, Expression.Constant(0d));
                }

                if (targetType == typeof(int))
                {
                    return Expression.Convert(sourceValue, typeof(int));
                }

                var nonNullableTargetType = Nullable.GetUnderlyingType(targetType);
                if (nonNullableTargetType != null)
                {
                    return Expression.Convert(ConvertSingleValue(sourceValue, strings, nonNullableTargetType), targetType);
                }

                throw new CompactFontException("Unsupported DICT property data type " + targetType.FullName + ".");
            }

            private static Expression ConvertArrayValue(Expression inputArray, Expression strings, Type targetElementType)
            {
                var breakLabel = Expression.Label("LoopBreak");

                var outputArray = Expression.Variable(targetElementType.MakeArrayType(), "output");
                var i = Expression.Variable(typeof(int), "i");
                var length = Expression.Variable(typeof(int), "length");

                return Expression.Block(new[] { outputArray, i, length },

                    Expression.Assign(i, Expression.Constant(-1)),
                    Expression.Assign(length, Expression.Property(inputArray, nameof(Array.Length))),
                    Expression.Assign(outputArray, Expression.NewArrayBounds(targetElementType, length)),

                    Expression.Loop(
                        Expression.IfThenElse(
                            Expression.LessThan(Expression.PreIncrementAssign(i), length),
                            Expression.Assign(
                                Expression.ArrayAccess(outputArray, i),
                                ConvertSingleValue(Expression.ArrayAccess(inputArray, i), strings, targetElementType)
                                ),
                            Expression.Break(breakLabel)
                            ),
                        breakLabel
                    ),

                    outputArray
                    );
            }
        }

        private static class Serializer<TDict>
        {
            private static Action<DictData, TDict, TDict, CompactFontStringTable>? serializer;

            public static void Serialize(DictData target, TDict dict, TDict defaultValues, CompactFontStringTable strings)
            {
                if (serializer == null)
                {
                    lock (lockObject)
                    {
                        if (serializer == null)
                        {
                            serializer = Compile();
                        }
                    }
                }

                serializer(target, dict, defaultValues, strings);
            }

            private static Expression Equals(Expression a, Expression b)
            {
                if (a.Type != b.Type)
                {
                    throw new ArgumentException("Expected expressions of the same type.");
                }

                var type = a.Type;

                MethodInfo comparer;

                if (type.IsArray)
                {
                    comparer = arrayEqualsMethod.MakeGenericMethod(type.GetElementType());
                }
                else
                {
                    comparer = objectEqualsMethod;
                    a = Expression.Convert(a, typeof(object));
                    b = Expression.Convert(b, typeof(object));
                }

                return Expression.Call(comparer, a, b);
            }

            private static Expression ConvertValue(Expression value, Expression strings)
            {
                if (value.Type.IsArray)
                {
                    return ConvertArrayValue(value, strings);
                }
                else
                {
                    return Expression.NewArrayInit(typeof(double), ConvertSingleValue(value, strings));
                }
            }

            private static Expression ConvertArrayValue(Expression inputArray, Expression strings)
            {
                var breakLabel = Expression.Label("LoopBreak");

                var outputArray = Expression.Variable(typeof(double[]), "output");
                var i = Expression.Variable(typeof(int), "i");
                var length = Expression.Variable(typeof(int), "length");

                return Expression.Block(new[] { outputArray, i, length },

                    Expression.Assign(i, Expression.Constant(-1)),
                    Expression.Assign(length, Expression.Property(inputArray, nameof(Array.Length))),
                    Expression.Assign(outputArray, Expression.NewArrayBounds(typeof(double), length)),

                    Expression.Loop(
                        Expression.IfThenElse(
                            Expression.LessThan(Expression.PreIncrementAssign(i), length),
                            Expression.Assign(
                                Expression.ArrayAccess(outputArray, i),
                                ConvertSingleValue(Expression.ArrayAccess(inputArray, i), strings)
                                ),
                            Expression.Break(breakLabel)
                            ),
                        breakLabel
                    ),

                    outputArray
                    );
            }

            private static Expression ConvertSingleValue(Expression value, Expression strings)
            {
                if (value.Type == typeof(string))
                {
                    var stringIndex = Expression.Call(strings,
                        methodName: nameof(CompactFontStringTable.Lookup),
                        typeArguments: null,
                        arguments: Expression.Convert(value, typeof(string)));

                    return Expression.Convert(stringIndex, typeof(double));
                }

                var nullableType = Nullable.GetUnderlyingType(value.Type);
                if (nullableType != null)
                {
                    value = Expression.Property(value, nameof(Nullable<int>.Value));
                }

                if (value.Type == typeof(bool))
                {
                    return Expression.Condition(value, Expression.Constant(1d), Expression.Constant(0d));
                }

                if (value.Type == typeof(int))
                {
                    return Expression.Convert(value, typeof(double));
                }

                if (value.Type == typeof(double))
                {
                    return value;
                }

                throw new CompactFontException("Unsupported DICT property data type " + value.Type + ".");
            }

            private static Expression ShouldSerialize(PropertyInfo property, Expression sourceInstance, Expression defaultInstance, Expression strings)
            {

                Expression notEqual = Expression.Not(Equals(
                    Expression.Property(sourceInstance, property),
                    Expression.Property(defaultInstance, property)));

                if (property.PropertyType.GetTypeInfo().IsValueType)
                {
                    var nullableType = Nullable.GetUnderlyingType(property.PropertyType);
                    if (nullableType == null)
                    {
                        return notEqual;
                    }
                    else
                    {
                        return Expression.AndAlso(
                            Expression.Property(Expression.Property(sourceInstance, property), nameof(Nullable<int>.HasValue)),
                            notEqual
                            );
                    }
                }
                else
                {
                    return Expression.AndAlso(
                        Expression.ReferenceNotEqual(
                            Expression.Property(sourceInstance, property),
                            Expression.Constant(null, property.PropertyType)),
                        notEqual
                        );
                }
            }

            private static Action<DictData, TDict, TDict, CompactFontStringTable> Compile()
            {
                var targetParam = Expression.Parameter(typeof(DictData), "target");
                var sourceParam = Expression.Parameter(typeof(TDict), "source");
                var defaultParam = Expression.Parameter(typeof(TDict), "defaultValues");
                var stringsParam = Expression.Parameter(typeof(CompactFontStringTable), "strings");

                var addMethod = typeof(DictData).GetTypeInfo().GetMethod(nameof(DictData.Add), new[] { typeof(int), typeof(double[]) });

                var sourceElementValue = Expression.Variable(typeof(double[]));
                var body = new List<Expression>();

                foreach (var property in typeof(TDict).GetTypeInfo().GetProperties())
                {
                    var attribute = (CompactFontDictOperatorAttribute?)property
                        .GetCustomAttributes(typeof(CompactFontDictOperatorAttribute), false)
                        .FirstOrDefault();

                    if (attribute == null)
                    {
                        continue;
                    }

                    body.Add(Expression.IfThen(
                        ShouldSerialize(property, sourceParam, defaultParam, stringsParam),
                        Expression.Call(targetParam, addMethod,
                            Expression.Constant(attribute.Value),
                            ConvertValue(Expression.Property(sourceParam, property), stringsParam))
                        ));
                }

                var lambda = Expression
                    .Lambda<Action<DictData, TDict, TDict, CompactFontStringTable>>(
                        Expression.Block(new[] { sourceElementValue }, body),
                        targetParam, sourceParam, defaultParam, stringsParam);

                return lambda.Compile();
            }

        }

        public static void Deserialize<TDict>(TDict target, DictData dictData, CompactFontStringTable strings)
        {
            Deserializer<TDict>.Deserialize(target, dictData, strings);
        }

        public static void Serialize<TDict>(DictData target, TDict dict, TDict defaultValues, CompactFontStringTable strings)
        {
            Serializer<TDict>.Serialize(target, dict, defaultValues, strings);
        }
    }
}
