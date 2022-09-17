// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Common
{
    internal static class TaskExtensions
    {
#if HAVE_ASYNC
        public static async Task<T> OrCanceled<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var cancellationTsc = new TaskCompletionSource<bool>();

            using var cancellationRegistration = cancellationToken.Register(() =>
            {
                cancellationTsc.SetException(new TaskCanceledException());
            });

            var completedOrCancelled = await Task.WhenAny(cancellationTsc.Task, task).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return await task.ConfigureAwait(false);
        }
#endif

        public static T GetResult<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            // Deadlock safe implementation compatible with .NET 4.0
            using (var completedEvent = new ManualResetEventSlim())
            {
                task.ContinueWith(t =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        completedEvent.Set();
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);

                completedEvent.Wait(cancellationToken);
            }

            // We know that the task has completed by now
            return task.Result;
        }
    }
}
