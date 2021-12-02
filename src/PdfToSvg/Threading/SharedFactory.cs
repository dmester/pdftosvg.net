// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Threading
{
    /// <summary>
    /// Shortcut for creating instances of <see cref="SharedFactory{T}"/>.
    /// </summary>
    internal static class SharedFactory
    {
        public static SharedFactory<T> Create<T>(Func<CancellationToken, Task<T>> factory)
        {
            return new SharedFactory<T>(factory);
        }
    }

    /// <summary>
    /// A lazily started reference counted task that is cancellable and shared by multiple callers.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     This can be used as a replacement for a <see cref="Task"/>, with some differences in behavior:
    /// </para>
    /// 
    /// <list type="bullet">
    ///     <item>
    ///         The factory is lazily invoked upon the first call to <see cref="GetResult(CancellationToken)"/>
    ///     </item>
    ///     <item>
    ///         If multiple calls are made to <see cref="GetResult(CancellationToken)"/>, they will await the same
    ///         factory invokation to complete.
    ///     </item>
    ///     <item>
    ///         The factory is cancelled when all callers to <see cref="GetResult(CancellationToken)"/> have been
    ///         cancelled.
    ///     </item>
    /// </list>
    /// 
    /// <para>
    ///     This class is thread safe.
    /// </para>
    /// </remarks>
    internal class SharedFactory<T>
    {
        private readonly object stateLock = new object();

        private CancellationTokenSource? cts;
        private int awaiters;

        private bool completed;
        private Exception? error;
        private T? result;

        private ManualResetEventSlim? completedEvent;
        private Func<CancellationToken, Task<T>> factory;

        public SharedFactory(Func<CancellationToken, Task<T>> factory)
        {
            this.factory = factory;
        }

        public T GetResult(CancellationToken cancellationToken)
        {
            if (completed)
            {
                return GetResultNow();
            }

            lock (stateLock)
            {
                if (completed)
                {
                    return GetResultNow();
                }

                if (awaiters++ == 0)
                {
                    cts = new CancellationTokenSource();
                    completedEvent = new ManualResetEventSlim();

                    Run(cts);
                }
            }

            try
            {
                completedEvent!.Wait(cancellationToken);
            }
            finally
            {
                lock (stateLock)
                {
                    if (--awaiters == 0)
                    {
                        cts?.Cancel();

                        completedEvent!.Dispose();
                        completedEvent = null;
                    }
                }
            }

            return GetResultNow();
        }

        private T GetResultNow()
        {
            if (error != null)
            {
                throw error;
            }

            return result!;
        }

        private void Run(CancellationTokenSource runCts)
        {
            // Use the .NET 4.0 version of TPL instead of async-await to avoid writing two separate implementations for
            // .NET 4.0 and .NET 4.5+.

            Task.Factory
                .StartNew(() => factory(runCts.Token))
                .Unwrap()
                .ContinueWith(t =>
                {
                    lock (stateLock)
                    {
                        if (this.cts == runCts)
                        {
                            this.cts = null;

                            // Neither the result nor any exception can be trusted if the operation was cancelled
                            if (!runCts.IsCancellationRequested)
                            {
                                switch (t.Status)
                                {
                                    case TaskStatus.Canceled:
                                        // The task was not cancelled by SharedFactory => publish the exception
                                        this.error = new OperationCanceledException();
                                        break;

                                    case TaskStatus.Faulted:
                                        var aggregateException = t.Exception;

                                        // Unpack AggregateException
                                        if (aggregateException == null)
                                        {
                                            this.error = new Exception(nameof(SharedFactory) + " failed.");
                                        }
                                        else if (aggregateException.InnerExceptions.Count == 1)
                                        {
                                            this.error = aggregateException.InnerException;
                                        }
                                        else
                                        {
                                            this.error = aggregateException;
                                        }

                                        break;

                                    case TaskStatus.RanToCompletion:
                                        this.result = t.Result;
                                        break;
                                }

                                this.completed = true;
                                this.completedEvent?.Set();
                            }
                        }

                        runCts.Dispose();
                    }

                }, TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}
