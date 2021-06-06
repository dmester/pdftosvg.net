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
    /// <summary>
    /// Helper class for creating Begin/End implementations of operations based on the <see cref="Task"/> based implementation.
    /// In addition to using <see cref="Task"/> as <see cref="IAsyncResult"/>, it verifies where the <see cref="IAsyncResult"/>
    /// passed to the end method came from.
    /// </summary>
    internal class TaskAsyncResult<TOwner, TValue> : IAsyncResult
    {
        private readonly Task<TValue> task;

        public TaskAsyncResult(Task<TValue> task, object state)
        {
            this.task = task;
            this.AsyncState = state;
        }

        public object AsyncState { get; }

        public WaitHandle AsyncWaitHandle => ((IAsyncResult)task).AsyncWaitHandle;

        public bool CompletedSynchronously => ((IAsyncResult)task).CompletedSynchronously;

        public bool IsCompleted => task.IsCompleted;

        public static IAsyncResult Begin(Task<TValue> task, AsyncCallback callback, object state)
        {
            var wrapper = new TaskAsyncResult<TOwner, TValue>(task, state);

            if (callback != null)
            {
                task.ContinueWith(_ => callback(wrapper), TaskScheduler.Default);
            }

            return wrapper;
        }

        public static TValue End(IAsyncResult asyncResult)
        {
            if (asyncResult == null) throw new ArgumentNullException(nameof(asyncResult));

            var wrapper = asyncResult as TaskAsyncResult<TOwner, TValue>;
            if (wrapper == null)
            {
                throw new ArgumentException($"The specified {nameof(IAsyncResult)} was not created by {nameof(TOwner)}.");
            }

            return wrapper.task.Result;
        }
    }
}
