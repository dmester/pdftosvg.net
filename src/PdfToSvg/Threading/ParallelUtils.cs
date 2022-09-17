// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Threading
{
    internal static class ParallelUtils
    {
        private static AggregateException GetFlatAggregateException(this IEnumerable<Exception> source)
        {
            var exceptions = new List<Exception>();
            var stack = new Stack<IEnumerator<Exception>>();

            try
            {
                stack.Push(source.GetEnumerator());

                do
                {
                    var enumerator = stack.Peek();
                    if (enumerator.MoveNext())
                    {
                        var exception = enumerator.Current;
                        if (exception is AggregateException aex)
                        {
                            stack.Push(aex.InnerExceptions.GetEnumerator());
                        }
                        else
                        {
                            exceptions.Add(exception);
                        }
                    }
                    else
                    {
                        enumerator.Dispose();
                        stack.Pop();
                    }
                }
                while (stack.Count > 0);
            }
            finally
            {
                while (stack.Count > 0)
                {
                    try
                    {
                        stack.Pop().Dispose();
                    }
                    catch
                    {
                        // Best effort
                    }
                }
            }

            return new AggregateException(exceptions);
        }

#if HAVE_ASYNC
        public static async Task ForEachAsync<T>(IEnumerable<T> items, Func<T, int, Task> body, int maxConcurrentTasks = 4, CancellationToken cancellationToken = default)
        {
            if (maxConcurrentTasks < 1) throw new ArgumentOutOfRangeException(nameof(maxConcurrentTasks));

            using var cts = new CancellationTokenSource();
            using var semaphore = new SemaphoreSlim(maxConcurrentTasks);
            using var cancellationRegistration = cancellationToken.Register(cts.Cancel);

            var exceptions = new List<Exception>();
            var tasks = new List<Task>();
            var index = 0;

            foreach (var item in items)
            {
                if (cts.IsCancellationRequested)
                {
                    break;
                }

                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                Task task;

                try
                {
                    task = body(item, index++);

                    lock (tasks)
                    {
                        tasks.Add(task);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    cts.Cancel();
                    break;
                }

                var _ = task.ContinueWith(t =>
                {
                    lock (tasks)
                    {
                        tasks.Remove(task);

                        if (t.IsCanceled)
                        {
                            exceptions.Add(new TaskCanceledException(task));
                            cts.Cancel();
                        }
                        else if (t.IsFaulted)
                        {
                            exceptions.Add(t.Exception);
                            cts.Cancel();
                        }

                        semaphore.Release();
                    }
                }, cts.Token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

            Task[] remainingTasks;

            lock (tasks)
            {
                remainingTasks = tasks.ToArray();
            }

            foreach (var task in remainingTasks)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch
                {
                    // Captured by ConfigureWith above
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if (exceptions.Count > 0)
            {
                throw GetFlatAggregateException(exceptions);
            }
        }
#endif
    }
}
