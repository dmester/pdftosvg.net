// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PdfToSvg.Drawing
{
    internal class OperationDispatcher
    {
        private readonly ILookup<string, Handler> handlers;

        private class Handler
        {
            public readonly string Operator;
            public readonly Action<object, object?[]> Invoke;
            public readonly ParameterInfo[] Parameters;

            public Handler(string op, Action<object, object?[]> invoke, ParameterInfo[] parameters)
            {
                Operator = op;
                Invoke = invoke;
                Parameters = parameters;
            }
        }

        public OperationDispatcher(Type instanceType)
        {
            handlers = instanceType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SelectMany(method => GetHandler(method))

                // Prefer matching methods with the longest parameter list.
                // This is not as advanced as the C# compilator, but a more advanced algorithm is also not needed here.
                // ToLookup preserves order within each group according to the docs.
                .OrderByDescending(handler => handler.Operator.Length)

                .ToLookup(x => x.Operator);
        }

        private static IEnumerable<Handler> GetHandler(MethodInfo method)
        {
            var operations = method.GetCustomAttributes(typeof(OperationAttribute), false);
            if (operations.Length > 0)
            {
                var parameters = method.GetParameters();
                var instance = Expression.Parameter(typeof(object));
                var argumentArray = Expression.Parameter(typeof(object?[]));

                var callArguments = new List<Expression>();

                for (var i = 0; i < parameters.Length; i++)
                {
                    callArguments.Add(Expression.Convert(
                        Expression.ArrayIndex(argumentArray, Expression.Constant(i)),
                        parameters[i].ParameterType
                        ));
                }

                var body = Expression.Call(Expression.Convert(instance, method.DeclaringType), method, callArguments);
                var invoke = Expression.Lambda<Action<object, object?[]>>(body, instance, argumentArray).Compile();

                foreach (OperationAttribute operation in operations)
                {
                    yield return new Handler(operation.Name, invoke, parameters);
                }
            }
        }

        private static bool CastArray(ref object array, int startIndex, Type targetArrayType)
        {
            if (array is object[] sourceArray)
            {
                var elementType = targetArrayType.GetElementType();
                var targetArray = Array.CreateInstance(elementType, Math.Max(0, sourceArray.Length - startIndex));
                var success = true;

                for (var i = startIndex; i < sourceArray.Length; i++)
                {
                    var element = sourceArray[i];
                    if (Cast(ref element, elementType))
                    {
                        targetArray.SetValue(element, i - startIndex);
                    }
                    else
                    {
                        success = false;
                        break;
                    }
                }

                if (success)
                {
                    array = targetArray;
                    return true;
                }
            }

            return false;
        }

        private static bool Cast(ref object? value, Type targetType)
        {
            if (value == null)
            {
                var isNullableTargetType = !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;
                if (isNullableTargetType)
                {
                    return true;
                }
            }

            else if (targetType.IsAssignableFrom(value.GetType()))
            {
                return true;
            }

            else if (targetType == typeof(string))
            {
                value = value.ToString();
                return true;
            }

            else if (targetType == typeof(double))
            {
                if (value is int sourceValue)
                {
                    value = (double)sourceValue;
                    return true;
                }
            }

            else if (targetType == typeof(float))
            {
                if (value is double sourceDblValue)
                {
                    value = (float)sourceDblValue;
                    return true;
                }
                if (value is int sourceIntValue)
                {
                    value = (float)sourceIntValue;
                    return true;
                }
            }

            else if (targetType == typeof(int))
            {
                if (value is double sourceValue)
                {
                    value = (int)sourceValue;
                    return true;
                }
            }

            else if (targetType.IsArray && targetType != typeof(object[]))
            {
                if (CastArray(ref value, 0, targetType))
                {
                    return true;
                }
            }

            return false;
        }

        public bool Dispatch(object instance, string opName, object?[] operands)
        {
            foreach (var handler in handlers[opName])
            {
                var castedArguments = new object?[handler.Parameters.Length];
                var success = true;

                for (var i = 0; i < handler.Parameters.Length; i++)
                {
                    var parameter = handler.Parameters[i];

                    // Value supplied?
                    if (i < operands.Length)
                    {
                        var operand = operands[i];

                        if (Cast(ref operand, parameter.ParameterType))
                        {
                            castedArguments[i] = operand;
                            continue;
                        }
                    }

                    // Is params ...[] parameter?
                    if (parameter.GetCustomAttribute<ParamArrayAttribute>(false) != null)
                    {
                        object rest = operands;

                        if (CastArray(ref rest, i, parameter.ParameterType))
                        {
                            castedArguments[i] = rest;
                        }
                        else
                        {
                            success = false;
                        }

                        break;
                    }

                    // Has default value?
                    if (parameter.HasDefaultValue)
                    {
                        castedArguments[i] = parameter.DefaultValue;
                        continue;
                    }

                    success = false;
                    break;
                }

                if (success)
                {
                    handler.Invoke(instance, castedArguments);
                    return true;
                }
            }

            return false;
        }
    }
}
