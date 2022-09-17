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
        private static readonly Task completedTask;
        private readonly ILookup<string, Handler> handlers;

        static OperationDispatcher()
        {
            var tcs = new TaskCompletionSource<bool>();
            tcs.SetResult(true);
            completedTask = tcs.Task;
        }

        private class Handler
        {
            public readonly string Operator;
            public readonly Action<object, object?[]>? Invoke;
            public readonly Func<object, object?[], Task>? InvokeAsync;
            public readonly ParameterInfo[] Parameters;

            public Handler(string op,
                Action<object, object?[]>? invoke,
                Func<object, object?[], Task>? invokeAsync,
                ParameterInfo[] parameters)
            {
                Operator = op;
                Invoke = invoke;
                InvokeAsync = invokeAsync;
                Parameters = parameters;
            }
        }

        public OperationDispatcher(Type instanceType)
        {
            handlers = instanceType
                .GetTypeInfo()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .SelectMany(method => GetHandlers(method))

                // Prefer matching methods with the longest parameter list.
                // This is not as advanced as the C# compilator, but a more advanced algorithm is also not needed here.
                // ToLookup preserves order within each group according to the docs.
                .OrderByDescending(handler => handler.Operator.Length)

                // Prefer the async version, if there is a sync and an async method.
                .ThenBy(handler => handler.Invoke == null ? 1 : 2)

                .ToLookup(x => x.Operator);
        }

        private static IEnumerable<Handler> GetHandlers(MethodInfo method)
        {
            using var operations = method
                .GetCustomAttributes(typeof(OperationAttribute), false)
                .OfType<OperationAttribute>()
                .GetEnumerator();

            if (operations.MoveNext())
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

                Action<object, object?[]>? invoke;
                Func<object, object?[], Task>? invokeAsync;

                if (method.ReturnType == typeof(void))
                {
                    invoke = Expression
                        .Lambda<Action<object, object?[]>>(body, instance, argumentArray)
                        .Compile();

                    var bodyWithReturn = Expression.Block(body, Expression.Constant(completedTask));
                    invokeAsync = Expression
                        .Lambda<Func<object, object?[], Task>>(bodyWithReturn, instance, argumentArray)
                        .Compile();
                }
                else if (method.ReturnType == typeof(Task))
                {
                    invoke = null;

                    invokeAsync = Expression
                        .Lambda<Func<object, object?[], Task>>(body, instance, argumentArray)
                        .Compile();
                }
                else
                {
                    throw new ArgumentException("Method " + method.Name + " cannot return a " + method.ReturnType + ".");
                }

                do
                {
                    yield return new Handler(operations.Current.Name, invoke, invokeAsync, parameters);
                }
                while (operations.MoveNext());
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
                var isNullableTargetType = !targetType.GetTypeInfo().IsValueType || Nullable.GetUnderlyingType(targetType) != null;
                if (isNullableTargetType)
                {
                    return true;
                }
            }

            else if (targetType.GetTypeInfo().IsAssignableFrom(value.GetType()))
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

        private static object?[]? CastArguments(Handler handler, object?[] operands)
        {
            var castedArguments = new object?[handler.Parameters.Length];

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
                var paramArrayAttributes = parameter.GetCustomAttributes(typeof(ParamArrayAttribute), true);
                if (paramArrayAttributes.Any())
                {
                    object rest = operands;

                    if (CastArray(ref rest, i, parameter.ParameterType))
                    {
                        castedArguments[i] = rest;
                    }
                    else
                    {
                        return null;
                    }

                    break;
                }

                // Has default value?
#if NET40
                if (!(parameter.DefaultValue is DBNull))
#else
                if (parameter.HasDefaultValue)
#endif
                {
                    castedArguments[i] = parameter.DefaultValue;
                    continue;
                }

                return null;
            }

            return castedArguments;
        }

        public bool Dispatch(object instance, string opName, object?[] operands)
        {
            foreach (var handler in handlers[opName])
            {
                if (handler.Invoke == null)
                {
                    continue;
                }

                var castedArguments = CastArguments(handler, operands);
                if (castedArguments != null)
                {
                    handler.Invoke(instance, castedArguments);
                    return true;
                }
            }

            return false;
        }

#if HAVE_ASYNC
        public async Task<bool> DispatchAsync(object instance, string opName, object?[] operands)
        {
            foreach (var handler in handlers[opName])
            {
                if (handler.InvokeAsync == null)
                {
                    continue;
                }

                var castedArguments = CastArguments(handler, operands);
                if (castedArguments != null)
                {
                    await handler.InvokeAsync(instance, castedArguments).ConfigureAwait(false);
                    return true;
                }
            }

            return false;
        }
#endif
    }
}
