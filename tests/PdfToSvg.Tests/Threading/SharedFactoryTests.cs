// Copyright (c) PdfToSvg.NET contributors.
// https://github.com/dmester/pdftosvg.net
// Licensed under the MIT License.

using NUnit.Framework;
using PdfToSvg.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PdfToSvg.Tests.Threading
{
    internal class SharedFactoryTests
    {
        private class Worker
        {
            public ManualResetEventSlim DoComplete = new();
            public Exception Throw;
        }

        private class Request
        {
            public Thread Thread;
            public CancellationTokenSource Cts = new();
        }

        public enum ExpectedOutcome
        {
            Succeed,
            Fail,
            Cancel,
        }

        [TestCase(ExpectedOutcome.Succeed)]
        [TestCase(ExpectedOutcome.Fail)]
        [TestCase(ExpectedOutcome.Cancel)]
        public void LoadTest(ExpectedOutcome expectedOutcome)
        {
            const int Factories = 50;
            const int ThreadsPerFactory = 50;

            const string ExpectedException = "Oops";
            const int ExpectedResult = 42;

            var startEvent = new ManualResetEventSlim();
            var threads = new List<Thread>();
            var unexpectedErrors = new ConcurrentQueue<string>();

            var cancelledThreadCount = 0;
            var succeededThreadCount = 0;
            var failedThreadCount = 0;

            var cts = new CancellationTokenSource();

            for (var i = 0; i < Factories; i++)
            {
                var factory = new SharedFactory<int>(cancellationToken =>
                {
                    switch (i % 3)
                    {
                        case 0:
                            Thread.Sleep(10);
                            break;

                        case 1:
                            Thread.Yield();
                            break;
                    }

                    if (expectedOutcome == ExpectedOutcome.Cancel)
                    {
                        cancellationToken.WaitHandle.WaitOne(2000);
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    if (expectedOutcome == ExpectedOutcome.Fail)
                    {
                        throw new Exception(ExpectedException);
                    }

                    return ExpectedResult;
                });

                for (var j = 0; j < ThreadsPerFactory; j++)
                {
                    var thread = new Thread(() =>
                    {
                        startEvent.Wait();

                        try
                        {
                            var result = factory.GetResult(cts.Token);

                            if (result == ExpectedResult)
                            {
                                Interlocked.Increment(ref succeededThreadCount);
                            }
                            else
                            {
                                unexpectedErrors.Enqueue("Wrong result from factory");
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            Interlocked.Increment(ref cancelledThreadCount);
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message == ExpectedException)
                            {
                                Interlocked.Increment(ref failedThreadCount);
                            }
                            else
                            {
                                unexpectedErrors.Enqueue(ex.ToString());
                            }
                        }
                    });
                    thread.Start();

                    threads.Add(thread);
                }
            }

            startEvent.Set();
            Thread.Sleep(50);

            if (expectedOutcome == ExpectedOutcome.Cancel)
            {
                cts.Cancel();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            foreach (var unexpectedError in unexpectedErrors)
            {
                Assert.Fail(unexpectedError);
            }

            Assert.AreEqual(expectedOutcome == ExpectedOutcome.Cancel ? threads.Count : 0, cancelledThreadCount, "Cancelled thread count");
            Assert.AreEqual(expectedOutcome == ExpectedOutcome.Succeed ? threads.Count : 0, succeededThreadCount, "Succeeded thread count");
            Assert.AreEqual(expectedOutcome == ExpectedOutcome.Fail ? threads.Count : 0, failedThreadCount, "Failed thread count");
        }

        // Commands
        // Get-<id>      - Calls GetResult.
        // Cancel-<id>   - Cancels a GetResult request, but does not immediately stops the worker thread.
        // Throw-<id>    - Makes a worker throw an unhandled exception.
        // Continue-<id> - Finishes the simulated factory work delay

        // Events within brackets may come in any order

        [TestCase(
            "Get-1a, Continue-1",
            "Get-1a Start, Worker-1 Start, Worker-1 Success, Get-1a Success")]
        [TestCase(
            "Get-1a, Throw-1",
            "Get-1a Start, Worker-1 Start, Worker-1 Failed, Get-1a Failed")]
        [TestCase(
            "Get-1a, Cancel-1a, Get-2a, Continue-2",
            "Get-1a Start, Worker-1 Start, [Worker-1 Cancelled, Get-1a Cancelled], Get-2a Start, Worker-2 Start, Worker-2 Success, Get-2a Success")]
        [TestCase(
            "Get-1a, Get-1b, Cancel-1a, Continue-2",
            "Get-1a Start, Worker-1 Start, Get-1b Start, [Worker-1 Cancelled, Get-1a Cancelled], Worker-2 Start, Worker-2 Success, Get-1b Success")]
        [TestCase(
            "Get-1a, Get-1b, Cancel-1b, Continue-1",
            "Get-1a Start, Worker-1 Start, Get-1b Start, Get-1b Cancelled, Worker-1 Success, Get-1a Success")]
#if !NET40
        [TestCase(
            "GetAsync-1a, Get-1b, Continue-1, Get-2a",
            "GetAsync-1a Start, Worker-1 Start, Get-1b Start, Worker-1 Success, [GetAsync-1a Success, Get-1b Success], Get-2a Start, Get-2a Success")]
        [TestCase(
            "GetAsync-1a, GetAsync-1b, Continue-1, GetAsync-2a",
            "GetAsync-1a Start, Worker-1 Start, GetAsync-1b Start, Worker-1 Success, [GetAsync-1a Success, GetAsync-1b Success], GetAsync-2a Start, GetAsync-2a Success")]
        [TestCase(
            "Get-1a, GetAsync-1b, Continue-1, GetAsync-2a",
            "Get-1a Start, Worker-1 Start, GetAsync-1b Start, Worker-1 Success, [Get-1a Success, GetAsync-1b Success], GetAsync-2a Start, GetAsync-2a Success")]
        [TestCase(
            "GetAsync-1a, Cancel-1a, GetAsync-2a, Continue-2",
            "GetAsync-1a Start, Worker-1 Start, [Worker-1 Cancelled, GetAsync-1a Cancelled], GetAsync-2a Start, Worker-2 Start, Worker-2 Success, GetAsync-2a Success")]
        [TestCase(
            "GetAsync-1a, Throw-1, GetAsync-1b",
            "GetAsync-1a Start, Worker-1 Start, Worker-1 Failed, GetAsync-1a Failed, GetAsync-1b Start, GetAsync-1b Failed")]
#endif
        public void VerifyEventSequence(string commands, string expectedEvents)
        {
            var requests = new ConcurrentDictionary<string, Request>();
            var workers = new ConcurrentDictionary<string, Worker>();

            var events = new ConcurrentQueue<string>();

            var workerResponded = new ManualResetEventSlim();
            var callerResponded = new ManualResetEventSlim();

            try
            {
                var sharedFactory = new SharedFactory<int>(cancellationToken =>
                {
                    var worker = new Worker();
                    var workerId = (workers.Count + 1).ToString();

                    workers[workerId] = worker;

                    workerResponded.Set();
                    events.Enqueue("Worker-" + workerId + " Start");

                    try
                    {
                        worker.DoComplete.Wait(5000, cancellationToken);
                    }
                    catch { }

                    var cancelled = cancellationToken.IsCancellationRequested;

                    events.Enqueue("Worker-" + workerId + (
                        cancelled ? " Cancelled" :
                        worker.Throw == null ? " Success" :
                        " Failed"
                        ));

                    workerResponded.Set();

                    if (cancelled)
                    {
                        throw new OperationCanceledException();
                    }

                    if (worker.Throw != null)
                    {
                        throw worker.Throw;
                    }

                    return 42;
                });

                foreach (var command in commands.Split(','))
                {
                    var commandParts = command.Trim().Split('-');

                    var commandName = commandParts[0];
                    var id = commandParts[1];

                    workerResponded.Reset();
                    callerResponded.Reset();

                    switch (commandName)
                    {
                        case "Get":
                            {
                                var request = new Request();

                                requests[id] = request;

                                request.Thread = new Thread(() =>
                                {
                                    try
                                    {
                                        events.Enqueue("Get-" + id + " Start");
                                        callerResponded.Set();

                                        sharedFactory.GetResult(request.Cts.Token);

                                        events.Enqueue("Get-" + id + " Success");
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        events.Enqueue("Get-" + id + " Cancelled");
                                    }
                                    catch (Exception ex) when (ex.Message == "Oops")
                                    {
                                        events.Enqueue("Get-" + id + " Failed");
                                    }

                                    callerResponded.Set();
                                });
                                request.Thread.Start();

                                callerResponded.Wait(5000);
                                workerResponded.Wait(100);
                            }
                            break;

#if !NET40
                        case "GetAsync":
                            {
                                var request = new Request();

                                requests[id] = request;

                                request.Thread = new Thread(() =>
                                {
                                    try
                                    {
                                        events.Enqueue("GetAsync-" + id + " Start");
                                        callerResponded.Set();

                                        if (!sharedFactory.GetResultAsync(request.Cts.Token).Wait(5000))
                                        {
                                            Assert.Fail("GetResultAsync never returned");
                                        }

                                        events.Enqueue("GetAsync-" + id + " Success");
                                    }
                                    catch (AggregateException aex) when (aex.InnerException is OperationCanceledException)
                                    {
                                        events.Enqueue("GetAsync-" + id + " Cancelled");
                                    }
                                    catch (AggregateException aex) when (aex.InnerException.Message == "Oops")
                                    {
                                        events.Enqueue("GetAsync-" + id + " Failed");
                                    }

                                    callerResponded.Set();
                                });
                                request.Thread.Start();

                                callerResponded.Wait(5000);
                                workerResponded.Wait(100);
                            }
                            break;
#endif

                        case "Cancel":
                            requests[id].Cts.Cancel();

                            callerResponded.Wait(5000);
                            break;

                        case "Continue":
                            if (workers.TryGetValue(id, out var continueWorker))
                            {
                                continueWorker.DoComplete.Set();

                                workerResponded.Wait(5000);
                            }
                            else
                            {
                                throw new Exception("Worker " + id + " to continue not found");
                            }
                            break;

                        case "Throw":
                            if (workers.TryGetValue(id, out var throwWorker))
                            {
                                throwWorker.Throw = new Exception("Oops");
                                throwWorker.DoComplete.Set();

                                callerResponded.Wait(5000);
                            }
                            else
                            {
                                throw new Exception("Worker " + id + " to throw not found");
                            }
                            break;

                        default:
                            throw new Exception("Unknown command " + commandName);
                    }

                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
            finally
            {
                foreach (var worker in workers.Values)
                {
                    worker.DoComplete.Set();
                }

                foreach (var request in requests)
                {
                    if (!request.Value.Thread.Join(5000))
                    {
                        Assert.Fail("SharedFactory request " + request.Key + " still not returned.");
                    }
                }

                foreach (var worker in workers.Values)
                {
                    worker.DoComplete.Dispose();
                }

                workerResponded.Dispose();
            }

            var actualEventsString = SortUnorderedEvents(expectedEvents, events);
            Assert.AreEqual(expectedEvents, actualEventsString, "Entire actual string: " + actualEventsString);
        }

        private static string SortUnorderedEvents(string expectedEvents, IEnumerable<string> actualEvents)
        {
            var unorderedEventsMatches = Regex.Matches(expectedEvents, "\\[([^\\]]+)\\]");
            var result = actualEvents.ToList();

            foreach (Match unorderedEventsMatch in unorderedEventsMatches)
            {
                var unorderedEvents = unorderedEventsMatch.Groups[1].Value
                    .Split(',')
                    .Select(x => x.Trim())
                    .ToList();

                var indexes = unorderedEvents
                    .Select(x => result.IndexOf(x))
                    .OrderBy(x => x)
                    .ToList();

                if (indexes.First() + unorderedEvents.Count - 1 == indexes.Last())
                {
                    for (var i = 0; i <= indexes.Last() - indexes.First(); i++)
                    {
                        result[indexes.First() + i] = unorderedEvents[i];
                    }

                    // Add brackets
                    result[indexes.First()] = "[" + result[indexes.First()];
                    result[indexes.Last()] = result[indexes.Last()] + "]";
                }
            }

            return string.Join(", ", result);
        }
    }
}
