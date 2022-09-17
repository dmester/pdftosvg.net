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
            public ManualResetEventSlim Return = new();
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
        // Get-<id>     - Calls GetResult.
        // Cancel-<id>  - Cancels a GetResult request, but does not immediately stops the worker thread.
        // Throw-<id>   - Makes a worker throw an unhandled exception.
        // Return-<id>  - Stops the worker thread.

        // Events within brackets may come in any order

        [TestCase(
            "Get-1a, Return-1",
            "Get-1a Start, Worker-1 Start, Worker-1 Success, Get-1a Success")]
        [TestCase(
            "Get-1a, Throw-1",
            "Get-1a Start, Worker-1 Start, Worker-1 Failed, Get-1a Failed")]
#if !NET40
        [TestCase(
            "GetAsync-1a, Get-1b, Return-1, Get-2a",
            "GetAsync-1a Start, Worker-1 Start, Get-1b Start, Worker-1 Success, [GetAsync-1a Success, Get-1b Success], Get-2a Start, Get-2a Success")]

        [TestCase(
            "GetAsync-1a, GetAsync-1b, Return-1, GetAsync-2a",
            "GetAsync-1a Start, Worker-1 Start, GetAsync-1b Start, Worker-1 Success, [GetAsync-1a Success, GetAsync-1b Success], GetAsync-2a Start, GetAsync-2a Success")]

        [TestCase(
            "Get-1a, GetAsync-1b, Return-1, GetAsync-2a",
            "Get-1a Start, Worker-1 Start, GetAsync-1b Start, Worker-1 Success, [Get-1a Success, GetAsync-1b Success], GetAsync-2a Start, GetAsync-2a Success")]

        [TestCase(
            "GetAsync-1a, Cancel-1a, GetAsync-2a, Return-2",
            "GetAsync-1a Start, Worker-1 Start, [Worker-1 Cancelled, GetAsync-1a Cancelled], GetAsync-2a Start, Worker-2 Start, Worker-2 Success, GetAsync-2a Success")]

        [TestCase(
            "GetAsync-1a, Throw-1, Return-1, GetAsync-1b",
            "GetAsync-1a Start, Worker-1 Start, Worker-1 Failed, GetAsync-1a Failed, GetAsync-1b Start, GetAsync-1b Failed")]

        [TestCase(
            "GetAsync-1a, Throw-1, GetAsync-1b, Return-1",
            "GetAsync-1a Start, Worker-1 Start, Worker-1 Failed, GetAsync-1a Failed, GetAsync-1b Start, GetAsync-1b Failed")]
#endif
        [TestCase(
            "Get-1a, Cancel-1a, Throw-1, Get-2a, Return-2",
            "Get-1a Start, Worker-1 Start, [Worker-1 Cancelled, Get-1a Cancelled], Get-2a Start, Worker-2 Start, Worker-2 Success, Get-2a Success")]
        [TestCase(
            "Get-1a, Get-1b, Cancel-1a, Return-1",
            "Get-1a Start, Worker-1 Start, Get-1b Start, [Worker-1 Cancelled, Get-1a Cancelled], Worker-2 Start, Worker-2 Success, Get-1b Success")]
        [TestCase(
            "Get-1a, Get-1b, Cancel-1b, Return-1",
            "Get-1a Start, Worker-1 Start, Get-1b Start, Get-1b Cancelled, Worker-1 Success, Get-1a Success")]
        [TestCase(
            "Get-1a, Cancel-1a, Get-2a, Return-2",
            "Get-1a Start, Worker-1 Start, Worker-1 Cancelled, Get-1a Cancelled, Get-2a Start, Worker-2 Start, Worker-2 Success, Get-2a Success")]
        [TestCase(
            "Get-1a, Cancel-1a, Return-1, Get-2a, Return-2",
            "Get-1a Start, Worker-1 Start, Worker-1 Cancelled, Get-1a Cancelled, Get-2a Start, Worker-2 Start, Worker-2 Success, Get-2a Success")]
        public void VerifyEventSequence(string commands, string expectedEvents)
        {
            var requests = new ConcurrentDictionary<string, Request>();
            var workers = new ConcurrentDictionary<string, Worker>();

            var events = new ConcurrentQueue<string>();

            var responded = new ManualResetEventSlim();

            try
            {
                var sharedFactory = new SharedFactory<int>(cancellationToken =>
                {
                    var worker = new Worker();
                    var workerId = (workers.Count + 1).ToString();

                    workers[workerId] = worker;

                    responded.Set();
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

                    responded.Set();

                    if (cancelled)
                    {
                        throw new OperationCanceledException();
                    }

                    worker.Return.Wait(5000, default);

                    responded.Set();

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

                    responded.Reset();

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
                                });
                                request.Thread.Start();
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
                                });
                                request.Thread.Start();
                            }
                            break;
#endif

                        case "Cancel":
                            requests[id].Cts.Cancel();
                            break;

                        case "Continue":
                            if (workers.TryGetValue(id, out var continueWorker))
                            {
                                continueWorker.DoComplete.Set();
                            }
                            else
                            {
                                throw new Exception("Worker " + id + " to continue not found");
                            }
                            break;

                        case "Return":
                            if (workers.TryGetValue(id, out var returnWorker))
                            {
                                returnWorker.DoComplete.Set();
                                returnWorker.Return.Set();
                            }
                            else
                            {
                                throw new Exception("Worker " + id + " to return not found");
                            }
                            break;

                        case "Throw":
                            if (workers.TryGetValue(id, out var throwWorker))
                            {
                                throwWorker.Throw = new Exception("Oops");
                                throwWorker.DoComplete.Set();
                                throwWorker.Return.Set();
                            }
                            else
                            {
                                throw new Exception("Worker " + id + " to throw not found");
                            }
                            break;

                        default:
                            throw new Exception("Unknown command " + commandName);
                    }

                    // Wait for any operations on the worker threads to complete before continuing
                    responded.Wait(100);

                    // There could be a few operations that should be completed after the responded event is set but before the thread has
                    // actually processed the command.
                    Thread.Sleep(50);
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
                    worker.Return.Set();
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
                    worker.Return.Dispose();
                    worker.DoComplete.Dispose();
                }

                responded.Dispose();
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
