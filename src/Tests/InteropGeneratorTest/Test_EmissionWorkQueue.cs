// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
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
        InteropGenerationWorkQueue<ConcurrentQueue<string>>.Builder graph = new();
        var root = graph.Add(static completed => completed.Enqueue("root"));
        var first = graph.Add(static completed =>
        {
            Assert.IsTrue(completed.Contains("root"));
            completed.Enqueue("first");
        }, root);
        var second = graph.Add(static completed =>
        {
            Assert.IsTrue(completed.Contains("root"));
            completed.Enqueue("second");
        }, root);
        _ = graph.Add(static completed =>
        {
            Assert.IsTrue(completed.Contains("first"));
            Assert.IsTrue(completed.Contains("second"));
            completed.Enqueue("join");
        }, first, second);
        InteropGenerationWorkQueue<ConcurrentQueue<string>> queue = graph.Build();

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
        InteropGenerationWorkQueue<ConcurrentBag<object>>.Builder graph = new();
        var first = graph.Add(static context => context.Add(context));
        var second = graph.Add(static context => context.Add(context));
        _ = graph.Add(static context => context.Add(context), first, second);
        InteropGenerationWorkQueue<ConcurrentBag<object>> queue = graph.Build();

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
    [DataRow(1)]
    [DataRow(4)]
    public async Task ConcurrentRuns_KeepTheirDependencyCountersAndContextsSeparate(int parallelism)
    {
        using CountdownEvent started = new(2);
        InteropGenerationWorkQueue<ConcurrentQueue<string>>.Builder graph = new();
        var root = graph.Add(completed =>
        {
            completed.Enqueue("root");
            started.Signal();
            Assert.IsTrue(started.Wait(TimeSpan.FromSeconds(10)), "Both runs must overlap.");
        });
        var first = graph.Add(static completed =>
        {
            Assert.IsTrue(completed.Contains("root"));
            completed.Enqueue("first");
        }, root);
        var second = graph.Add(static completed =>
        {
            Assert.IsTrue(completed.Contains("root"));
            completed.Enqueue("second");
        }, root);
        _ = graph.Add(static completed =>
        {
            Assert.IsTrue(completed.Contains("first"));
            Assert.IsTrue(completed.Contains("second"));
            completed.Enqueue("join");
        }, first, second);
        InteropGenerationWorkQueue<ConcurrentQueue<string>> queue = graph.Build();
        ConcurrentQueue<string> firstContext = new();
        ConcurrentQueue<string> secondContext = new();

        await Task.WhenAll(
            Task.Run(() => queue.Run(firstContext, parallelism, CancellationToken.None)),
            Task.Run(() => queue.Run(secondContext, parallelism, CancellationToken.None)))
            .WaitAsync(TimeSpan.FromSeconds(15));

        foreach (ConcurrentQueue<string> completed in new[] { firstContext, secondContext })
        {
            Assert.AreEqual(4, completed.Count);
            Assert.AreEqual("root", completed.ToArray()[0]);
            Assert.AreEqual("join", completed.ToArray()[^1]);
        }
    }

    [TestMethod]
    public async Task ThrowingWorkItem_DoesNotStrandConsumers()
    {
        InvalidOperationException expected = new("queue failure");
        InteropGenerationWorkQueue<InvalidOperationException>.Builder graph = new();
        var failing = graph.Add(static exception => throw exception);
        _ = graph.Add(static _ => Assert.Fail("A failed prerequisite must not release its dependent."), failing);
        InteropGenerationWorkQueue<InvalidOperationException> queue = graph.Build();

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
        InteropGenerationWorkQueue<CancellationToken>.Builder graph = new();
        var root = graph.Add(token =>
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
        });
        _ = graph.Add(static _ => Assert.Fail("A canceled prerequisite must not release its dependent."), root);
        InteropGenerationWorkQueue<CancellationToken> queue = graph.Build();

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
    public void ForeignPrerequisite_IsRejectedWithoutChangingTheGraph()
    {
        int calls = 0;
        Action context = () => calls++;
        Action<Action> body = static action => action();
        InteropGenerationWorkQueue<Action>.Builder graph = new();
        InteropGenerationWorkQueue<Action>.Builder other = new();
        var foreign = other.Add(body);
        Assert.ThrowsExactly<ArgumentException>(() => graph.Add(body, foreign));

        var root = graph.Add(body);
        Assert.ThrowsExactly<ArgumentException>(() => graph.Add(body, root, foreign));
        Assert.AreEqual(0, calls);

        _ = graph.Add(body, root);
        graph.Build().Run(context, 2, CancellationToken.None);
        Assert.AreEqual(2, calls);
    }

    [TestMethod]
    public void BuiltGraph_RejectsFurtherNodes()
    {
        InteropGenerationWorkQueue<Action>.Builder graph = new();
        var root = graph.Add(static action => action());
        InteropGenerationWorkQueue<Action> queue = graph.Build();

        Assert.ThrowsExactly<InvalidOperationException>(() => graph.Add(static action => action(), root));

        int calls = 0;
        queue.Run(() => calls++, 1, CancellationToken.None);
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public void EmptyGraph_CompletesAndObservesCancellation()
    {
        InteropGenerationWorkQueue<Action> queue = new InteropGenerationWorkQueue<Action>.Builder().Build();
        queue.Run(static () => Assert.Fail(), -1, CancellationToken.None);

        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        OperationCanceledException exception = Assert.ThrowsExactly<OperationCanceledException>(
            () => queue.Run(static () => Assert.Fail(), -1, cancellation.Token));
        Assert.AreEqual(cancellation.Token, exception.CancellationToken);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-2)]
    public void InvalidParallelism_IsRejected(int parallelism)
    {
        InteropGenerationWorkQueue<Action> queue = new InteropGenerationWorkQueue<Action>.Builder().Build();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => queue.Run(static () => Assert.Fail(), parallelism, CancellationToken.None));
    }
}
