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
    [DataRow(8)]
    public void Queue_RespectsDependencyBarriers(int parallelism)
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

        ConcurrentQueue<string> completed = new();
        queue.Run(completed, parallelism, CancellationToken.None);
        Assert.AreEqual(4, completed.Count);
        Assert.AreEqual("root", completed.ToArray()[0]);
        Assert.AreEqual("join", completed.ToArray()[^1]);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(8)]
    public void AllWorkItems_ReceiveTheInvocationContext(int parallelism)
    {
        InteropGenerationWorkQueue<ConcurrentBag<object>>.Builder graph = new();
        var first = graph.Add(static context => context.Add(context));
        var second = graph.Add(static context => context.Add(context));
        _ = graph.Add(static context => context.Add(context), first, second);
        InteropGenerationWorkQueue<ConcurrentBag<object>> queue = graph.Build();

        ConcurrentBag<object> context = [];

        queue.Run(context, parallelism, CancellationToken.None);

        Assert.AreEqual(3, context.Count);

        foreach (object actual in context)
        {
            Assert.AreSame(context, actual);
        }
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(4)]
    public async Task SeparateQueues_CanRunConcurrently(int parallelism)
    {
        using CountdownEvent started = new(2);
        InteropGenerationWorkQueue<ConcurrentQueue<string>> firstQueue = CreateQueue();
        InteropGenerationWorkQueue<ConcurrentQueue<string>> secondQueue = CreateQueue();
        ConcurrentQueue<string> firstContext = new();
        ConcurrentQueue<string> secondContext = new();

        await Task.WhenAll(
            Task.Run(() => firstQueue.Run(firstContext, parallelism, CancellationToken.None)),
            Task.Run(() => secondQueue.Run(secondContext, parallelism, CancellationToken.None)))
            .WaitAsync(TimeSpan.FromSeconds(15));

        foreach (ConcurrentQueue<string> completed in new[] { firstContext, secondContext })
        {
            Assert.AreEqual(4, completed.Count);
            Assert.AreEqual("root", completed.ToArray()[0]);
            Assert.AreEqual("join", completed.ToArray()[^1]);
        }

        InteropGenerationWorkQueue<ConcurrentQueue<string>> CreateQueue()
        {
            InteropGenerationWorkQueue<ConcurrentQueue<string>>.Builder graph = new();
            var root = graph.Add(completed =>
            {
                completed.Enqueue("root");
                started.Signal();
                Assert.IsTrue(started.Wait(TimeSpan.FromSeconds(10)), "Both queues must overlap.");
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

            return graph.Build();
        }
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(4)]
    public void CompletedQueue_RejectsAnotherRun(int parallelism)
    {
        InteropGenerationWorkQueue<Action>.Builder graph = new();
        var root = graph.Add(static action => action());
        _ = graph.Add(static action => action(), root);
        InteropGenerationWorkQueue<Action> queue = graph.Build();
        int calls = 0;
        queue.Run(() => calls++, parallelism, CancellationToken.None);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => queue.Run(() => calls++, parallelism, CancellationToken.None));
        Assert.AreEqual(2, calls);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(4)]
    public async Task RunningQueue_RejectsConcurrentRun(int parallelism)
    {
        using ManualResetEventSlim started = new();
        using ManualResetEventSlim release = new();
        int calls = 0;
        InteropGenerationWorkQueue<Action>.Builder graph = new();
        var root = graph.Add(action =>
        {
            action();
            started.Set();
            Assert.IsTrue(release.Wait(TimeSpan.FromSeconds(10)));
        });
        _ = graph.Add(static action => action(), root);
        InteropGenerationWorkQueue<Action> queue = graph.Build();
        Task execution = Task.Run(() => queue.Run(() => Interlocked.Increment(ref calls), parallelism, CancellationToken.None));

        try
        {
            Assert.IsTrue(started.Wait(TimeSpan.FromSeconds(10)));
            Assert.ThrowsExactly<InvalidOperationException>(
                () => queue.Run(() => Interlocked.Increment(ref calls), parallelism, CancellationToken.None));
        }
        finally
        {
            release.Set();
            await execution.WaitAsync(TimeSpan.FromSeconds(15));
        }

        Assert.AreEqual(2, calls);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(8)]
    public async Task ThrowingWorkItem_DoesNotStrandConsumers(int parallelism)
    {
        InvalidOperationException expected = new("queue failure");
        InteropGenerationWorkQueue<InvalidOperationException>.Builder graph = new();
        var failing = graph.Add(static exception => throw exception);
        _ = graph.Add(static _ => Assert.Fail("A failed prerequisite must not release its dependent."), failing);
        InteropGenerationWorkQueue<InvalidOperationException> queue = graph.Build();

        Task execution = Task.Run(() =>
        {
            InvalidOperationException actual = Assert.ThrowsExactly<InvalidOperationException>(
                () => queue.Run(expected, parallelism, CancellationToken.None));
            Assert.AreSame(expected, actual);
        });

        await execution.WaitAsync(TimeSpan.FromSeconds(15));
        InvalidOperationException repeated = Assert.ThrowsExactly<InvalidOperationException>(
            () => queue.Run(expected, parallelism, CancellationToken.None));
        Assert.AreNotSame(expected, repeated);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(4)]
    public async Task ExternalCancellation_PreservesOriginalTokenAndJoinsWorkers(int parallelism)
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
                () => queue.Run(cancellation.Token, parallelism, cancellation.Token));
            Assert.AreEqual(cancellation.Token, exception.CancellationToken);
        });
        bool began = started.Wait(TimeSpan.FromSeconds(10));
        cancellation.Cancel();
        await execution.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.IsTrue(began);
        Assert.AreEqual(1, finished, "Run must join all admitted work before returning.");
        Assert.ThrowsExactly<InvalidOperationException>(() => queue.Run(cancellation.Token, parallelism, cancellation.Token));
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
    public void BuiltGraph_RejectsFurtherNodesAndAnotherBuild()
    {
        InteropGenerationWorkQueue<Action>.Builder graph = new();
        var root = graph.Add(static action => action());
        InteropGenerationWorkQueue<Action> queue = graph.Build();

        Assert.ThrowsExactly<InvalidOperationException>(() => graph.Add(static action => action(), root));
        Assert.ThrowsExactly<InvalidOperationException>(() => graph.Build());

        int calls = 0;
        queue.Run(() => calls++, 1, CancellationToken.None);
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public void EmptyGraph_CompletesAndObservesCancellation()
    {
        InteropGenerationWorkQueue<Action> queue = new InteropGenerationWorkQueue<Action>.Builder().Build();
        queue.Run(static () => Assert.Fail(), 1, CancellationToken.None);
        Assert.ThrowsExactly<InvalidOperationException>(
            () => queue.Run(static () => Assert.Fail(), 1, CancellationToken.None));

        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        InteropGenerationWorkQueue<Action> canceledQueue = new InteropGenerationWorkQueue<Action>.Builder().Build();
        OperationCanceledException exception = Assert.ThrowsExactly<OperationCanceledException>(
            () => canceledQueue.Run(static () => Assert.Fail(), 1, cancellation.Token));
        Assert.AreEqual(cancellation.Token, exception.CancellationToken);
        Assert.ThrowsExactly<InvalidOperationException>(
            () => canceledQueue.Run(static () => Assert.Fail(), 1, CancellationToken.None));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(-2)]
    public void InvalidParallelism_IsRejected(int parallelism)
    {
        InteropGenerationWorkQueue<Action> queue = new InteropGenerationWorkQueue<Action>.Builder().Build();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => queue.Run(static () => Assert.Fail(), parallelism, CancellationToken.None));

        queue.Run(static () => Assert.Fail(), 1, CancellationToken.None);
    }
}
