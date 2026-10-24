// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WindowsRuntime.InteropGenerator.Generation;

namespace InteropGeneratorTest;

[TestClass]
public sealed class Test_EmissionWorkQueue
{
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(-1)]
    public void ReusedQueue_RespectsDependencyBarriers(int parallelism)
    {
        InteropGenerationWorkQueue<string, ConcurrentQueue<string>> queue = new(
            new("root", static completed => completed.Enqueue("root")),
            new("first", static completed =>
            {
                Assert.IsTrue(completed.Contains("root"));
                completed.Enqueue("first");
            }, "root"),
            new("second", static completed =>
            {
                Assert.IsTrue(completed.Contains("root"));
                completed.Enqueue("second");
            }, "root"),
            new("join", static completed =>
            {
                Assert.IsTrue(completed.Contains("first"));
                Assert.IsTrue(completed.Contains("second"));
                completed.Enqueue("join");
            }, "first", "second"));

        for (int run = 0; run < 3; run++)
        {
            ConcurrentQueue<string> completed = new();
            queue.Run(completed, parallelism, CancellationToken.None);
            Assert.AreEqual(4, completed.Count);
            Assert.AreEqual("root", completed.ToArray()[0]);
            Assert.AreEqual("join", completed.ToArray()[^1]);
        }
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(8)]
    public void AllWorkItems_ReceiveTheCurrentRunContext(int parallelism)
    {
        InteropGenerationWorkQueue<int, ConcurrentBag<object>> queue = new(
            new(0, static context => context.Add(context)),
            new(1, static context => context.Add(context)),
            new(2, static context => context.Add(context), 0, 1));

        for (int run = 0; run < 3; run++)
        {
            ConcurrentBag<object> context = [];

            queue.Run(context, parallelism, CancellationToken.None);

            Assert.AreEqual(3, context.Count);

            foreach (object actual in context)
            {
                Assert.AreSame(context, actual);
            }
        }
    }

    [TestMethod]
    public async Task ThrowingWorkItem_DoesNotStrandConsumers()
    {
        InvalidOperationException expected = new("queue failure");
        InteropGenerationWorkQueue<int, InvalidOperationException> queue = new(
            new(0, static exception => throw exception),
            new(1, static _ => Assert.Fail("A failed prerequisite must not release its dependent."), 0));

        Task execution = Task.Run(() =>
        {
            InvalidOperationException actual = Assert.ThrowsExactly<InvalidOperationException>(
                () => queue.Run(expected, 8, CancellationToken.None));
            Assert.AreSame(expected, actual);
        });

        await execution.WaitAsync(TimeSpan.FromSeconds(15));
    }

    [TestMethod]
    public async Task ExternalCancellation_PreservesOriginalTokenAndJoinsWorkers()
    {
        using CancellationTokenSource cancellation = new();
        using ManualResetEventSlim started = new();
        int finished = 0;
        InteropGenerationWorkQueue<int, CancellationToken> queue = new(new(0, token =>
        {
            started.Set();
            try
            {
                token.WaitHandle.WaitOne();
                token.ThrowIfCancellationRequested();
            }
            finally
            {
                Interlocked.Increment(ref finished);
            }
        }), new(1, static _ => Assert.Fail("A canceled prerequisite must not release its dependent."), 0));

        Task execution = Task.Run(() =>
        {
            OperationCanceledException exception = Assert.ThrowsExactly<OperationCanceledException>(
                () => queue.Run(cancellation.Token, 4, cancellation.Token));
            Assert.AreEqual(cancellation.Token, exception.CancellationToken);
        });
        bool began = started.Wait(TimeSpan.FromSeconds(10));
        cancellation.Cancel();
        await execution.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.IsTrue(began);
        Assert.AreEqual(1, finished, "Run must join all admitted work before returning.");
    }

    [TestMethod]
    public void InvalidGraphs_AreRejectedBeforeAnyBodyRuns()
    {
        int calls = 0;
        Action context = () => calls++;
        Action<Action> body = static action => action();
        Assert.ThrowsExactly<ArgumentException>(() =>
            new InteropGenerationWorkQueue<int, Action>(new(0, body), new(0, body)).Run(context, 1, CancellationToken.None));
        Assert.ThrowsExactly<KeyNotFoundException>(() =>
            new InteropGenerationWorkQueue<int, Action>(new InteropGenerationWorkQueue<int, Action>.WorkItem(0, body, 99)).Run(context, 1, CancellationToken.None));
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            new InteropGenerationWorkQueue<int, Action>(new(0, body, 1), new(1, body, 0)).Run(context, 1, CancellationToken.None));
        Assert.AreEqual(0, calls);
    }
}
