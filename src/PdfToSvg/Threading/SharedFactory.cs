// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using PdfToSvg.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
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
        public static SharedFactory<T> Create<T>(Func<CancellationToken, T> factory, Func<CancellationToken, Task<T>> factoryAsync)
        {
            return new SharedFactory<T>(factory, factoryAsync);
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
    ///         Async factories are cancelled when all callers to <see cref="GetResult(CancellationToken)"/> have been
    ///         cancelled.
    ///     </item>
    /// </list>
    /// 
    /// <para>
    ///     This class is thread-safe.
    /// </para>
    /// </remarks>
    internal class SharedFactory<T>
    {
        private readonly object stateLock = new object();

        private CancellationTokenSource? cts;
        private int awaiters;
        private Task<T>? task;

        private Func<CancellationToken, Task<T>> factoryAsync;
        private Func<CancellationToken, T> factory;

        public SharedFactory(Func<CancellationToken, T> factory)
        {
            this.factory = factory;
            this.factoryAsync = cancellationToken => Task.Factory.StartNew(() => factory(cancellationToken));
        }

        public SharedFactory(Func<CancellationToken, T> factory, Func<CancellationToken, Task<T>> factoryAsync)
        {
            this.factory = factory;
            this.factoryAsync = factoryAsync;
        }

        private class SyncFactoryCanceledException : OperationCanceledException { }

        private class FactoryContext : IDisposable
        {
            private readonly SharedFactory<T> owner;

            public FactoryContext(SharedFactory<T> owner)
            {
                this.owner = owner;

                lock (owner.stateLock)
                {
                    if (owner.awaiters++ == 0)
                    {
                        owner.cts = new CancellationTokenSource();
                    }
                }
            }

            public bool RequestWasCancelled { get; set; }

            public void Dispose()
            {
                lock (owner.stateLock)
                {
                    if (--owner.awaiters == 0)
                    {
                        if (owner.cts != null)
                        {
                            owner.cts.Cancel();
                            owner.cts.Dispose();
                            owner.cts = null;
                        }

                        if (RequestWasCancelled)
                        {
                            // Cancelled requests are allowed to be run again
                            owner.task = null;
                        }
                    }
                }
            }
        }

        public T GetResult(CancellationToken cancellationToken)
        {
            // The synchronous implementation will invoke the factory on the first thread calling GetResult. Other
            // threads requesting the result, both synchronous and asynchronous ones, will wait for the synchronous
            // factory to finish. Running the factory on a thread pool thread is not a good option since it increases
            // the risk of thread starvation.
            //
            // If the first synchronous caller cancels its request, one of the other callers will rerun the factory.
            // This is a small performance hit, but we can assume the factories will almost never be cancelled.

            using var context = new FactoryContext(this);

        Retry:
            Task<T> localTask;
            TaskCompletionSource<T>? tcs = null;

            lock (stateLock)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (task == null)
                {
                    tcs = new TaskCompletionSource<T>();
                    task = tcs.Task;
                }

                localTask = task;
            }

            try
            {
                T result;

                if (tcs == null)
                {
                    result = localTask.GetResult(cancellationToken);
                }
                else
                {
                    result = factory(cancellationToken);
                    tcs.TrySetResult(result);
                }

                return result;
            }
            catch (Exception ex)
            {
                if (ex is AggregateException aex)
                {
                    ex = aex.InnerException ?? ex;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    // If we are owning the task, then reset it
                    if (tcs != null)
                    {
                        lock (stateLock)
                        {
                            task = null;
                        }
                    }

                    context.RequestWasCancelled = true;
                    tcs?.TrySetException(new SyncFactoryCanceledException());
                }
                else
                {
                    tcs?.TrySetException(ex);

                    if (ex is SyncFactoryCanceledException)
                    {
                        goto Retry;
                    }
                }

#if !NET40
                ExceptionDispatchInfo.Capture(ex).Throw();
#endif
                throw ex;
            }
        }

#if HAVE_ASYNC
        public async Task<T> GetResultAsync(CancellationToken cancellationToken)
        {
            // The asynchronous implementation will invoke the factory on a thread pool thread upon the first request to
            // GetResultAsync. Other threads requesting the result, both synchronous and asynchronous ones, will wait
            // for the asynchronous factory to finish.
            //
            // The factory is cancelled when all callers to GetResult and GetResultAsync has been cancelled.

            using var context = new FactoryContext(this);

        Retry:
            Task<T> localTask;

            lock (stateLock)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (task == null)
                {
                    task = Task.Run(() => factoryAsync(cts!.Token));
                }

                localTask = task;
            }

            try
            {
                return await localTask.OrCanceled(cancellationToken).ConfigureAwait(false);
            }
            catch (SyncFactoryCanceledException)
            {
                goto Retry;
            }
            catch when (cancellationToken.IsCancellationRequested)
            {
                context.RequestWasCancelled = true;
                throw;
            }
        }
#endif
    }
}
