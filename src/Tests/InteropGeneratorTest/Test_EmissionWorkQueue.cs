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
        ConcurrentQueue<string> completed = new();
        InteropGenerationWorkQueue<string> queue = new(
            new("root", () => completed.Enqueue("root")),
            new("first", () =>
            {
                Assert.IsTrue(completed.Contains("root"));
                completed.Enqueue("first");
            }, "root"),
            new("second", () =>
            {
                Assert.IsTrue(completed.Contains("root"));
                completed.Enqueue("second");
            }, "root"),
            new("join", () =>
            {
                Assert.IsTrue(completed.Contains("first"));
                Assert.IsTrue(completed.Contains("second"));
                completed.Enqueue("join");
            }, "first", "second"));

        for (int run = 0; run < 3; run++)
        {
            completed.Clear();
            queue.Run(parallelism, CancellationToken.None);
            Assert.AreEqual(4, completed.Count);
            Assert.AreEqual("root", completed.ToArray()[0]);
            Assert.AreEqual("join", completed.ToArray()[^1]);
        }
    }

    [TestMethod]
    public async Task ThrowingWorkItem_DoesNotStrandConsumers()
    {
        InvalidOperationException expected = new("queue failure");
        InteropGenerationWorkQueue<int> queue = new(
            new(0, () => throw expected),
            new(1, () => Assert.Fail("A failed prerequisite must not release its dependent."), 0));

        Task execution = Task.Run(() =>
        {
            InvalidOperationException actual = Assert.ThrowsExactly<InvalidOperationException>(
                () => queue.Run(8, CancellationToken.None));
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
        InteropGenerationWorkQueue<int> queue = new(new(0, () =>
        {
            started.Set();
            try
            {
                cancellation.Token.WaitHandle.WaitOne();
                cancellation.Token.ThrowIfCancellationRequested();
            }
            finally
            {
                Interlocked.Increment(ref finished);
            }
        }), new(1, () => Assert.Fail("A canceled prerequisite must not release its dependent."), 0));

        Task execution = Task.Run(() =>
        {
            OperationCanceledException exception = Assert.ThrowsExactly<OperationCanceledException>(
                () => queue.Run(4, cancellation.Token));
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
        Action body = () => calls++;
        Assert.ThrowsExactly<ArgumentException>(() =>
            new InteropGenerationWorkQueue<int>(new(0, body), new(0, body)).Run(1, CancellationToken.None));
        Assert.ThrowsExactly<KeyNotFoundException>(() =>
            new InteropGenerationWorkQueue<int>(new InteropGenerationWorkQueue<int>.WorkItem(0, body, 99)).Run(1, CancellationToken.None));
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            new InteropGenerationWorkQueue<int>(new(0, body, 1), new(1, body, 0)).Run(1, CancellationToken.None));
        Assert.AreEqual(0, calls);
    }
}
