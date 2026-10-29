// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WindowsRuntime.InteropGenerator;

namespace InteropGeneratorTest;

[TestClass]
public sealed class Test_ParallelExtensions
{
    [TestMethod]
    [DataRow(1)]
    [DataRow(4)]
    [DataRow(-1)]
    public void UnbufferedLoop_ProcessesEveryItemExactlyOnce(int parallelism)
    {
        int[] counts = new int[128];

        ParallelLoopResult result = Parallel.ForEachUnbuffered(
            source: Enumerable.Range(0, counts.Length),
            token: CancellationToken.None,
            maxDegreesOfParallelism: parallelism,
            body: index => Interlocked.Increment(ref counts[index]));

        Assert.IsTrue(result.IsCompleted);
        Assert.IsTrue(counts.All(static count => count == 1));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(128)]
    public void FirstFailureLoop_ReturnsCompletionStatus(int count)
    {
        int completed = 0;

        ParallelLoopResult result = Parallel.ForEachUnbufferedUntilFirstFailure(
            source: _ => Enumerable.Range(0, count),
            token: CancellationToken.None,
            maxDegreesOfParallelism: 4,
            body: (_, _) => Interlocked.Increment(ref completed));

        Assert.IsTrue(result.IsCompleted);
        Assert.IsNull(result.LowestBreakIteration);
        Assert.AreEqual(count, completed);
    }

    [TestMethod]
    public void StandardLoop_BodyFailureRemainsAggregated()
    {
        InvalidOperationException expected = new("body failure");

        AggregateException actual = Assert.ThrowsExactly<AggregateException>(() => Parallel.ForEachUnbuffered(
            source: new[] { 0 },
            token: CancellationToken.None,
            maxDegreesOfParallelism: 4,
            body: _ => throw expected));

        Assert.AreSame(expected, actual.InnerExceptions.Single());
    }

    [TestMethod]
    public void StandardLoop_CancellationPreservesOriginalToken()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        OperationCanceledException actual = Assert.ThrowsExactly<OperationCanceledException>(() => Parallel.ForEachUnbuffered(
            source: new[] { 0 },
            token: cancellation.Token,
            maxDegreesOfParallelism: 4,
            body: static _ => Assert.Fail("Cancelled work must not start.")));

        Assert.AreEqual(cancellation.Token, actual.CancellationToken);
    }

    [TestMethod]
    [DataRow(-2)]
    [DataRow(-1)]
    [DataRow(0)]
    public void FirstFailureLoop_RequiresABoundedWorkerCount(int parallelism)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            Parallel.ForEachUnbufferedUntilFirstFailure<int>(
                source: static _ => throw new InvalidOperationException("Invalid worker counts must fail before creating the source."),
                token: CancellationToken.None,
                maxDegreesOfParallelism: parallelism,
                body: static (_, _) => Assert.Fail("Invalid worker counts must fail before running any work.")));
    }

    [TestMethod]
    public async Task FirstFailureLoop_UnblocksTheSourceAndJoinsSiblings()
    {
        InvalidOperationException expected = new("first body failure");
        using BlockingCollection<int> source = new(new ConcurrentQueue<int>([0, 1]));
        using ManualResetEventSlim siblingStarted = new();
        int finished = 0;

        Task execution = Task.Run(() =>
        {
            InvalidOperationException actual = Assert.ThrowsExactly<InvalidOperationException>(() =>
                Parallel.ForEachUnbufferedUntilFirstFailure(
                    source: source.GetConsumingEnumerable,
                    token: CancellationToken.None,
                    maxDegreesOfParallelism: 4,
                    body: (item, token) =>
                    {
                        if (item == 0)
                        {
                            Assert.IsTrue(siblingStarted.Wait(TimeSpan.FromSeconds(10)));

                            throw expected;
                        }

                        try
                        {
                            siblingStarted.Set();
                            Assert.IsTrue(token.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)));
                            token.ThrowIfCancellationRequested();
                        }
                        finally
                        {
                            Interlocked.Increment(ref finished);
                        }
                    }));

            Assert.AreSame(expected, actual);
            Assert.AreEqual(1, finished, "The sibling must finish before the initiating exception is rethrown.");
        });

        await execution.WaitAsync(TimeSpan.FromSeconds(20));
    }

    [TestMethod]
    public void FirstFailureLoop_InvocationCancellationTakesPrecedence()
    {
        using CancellationTokenSource cancellation = new();
        InvalidOperationException failure = new("body failure racing cancellation");

        OperationCanceledException actual = Assert.ThrowsExactly<OperationCanceledException>(() =>
            Parallel.ForEachUnbufferedUntilFirstFailure(
                source: static _ => new[] { 0 },
                token: cancellation.Token,
                maxDegreesOfParallelism: 4,
                body: (_, _) =>
                {
                    cancellation.Cancel();

                    throw failure;
                }));

        Assert.AreEqual(cancellation.Token, actual.CancellationToken);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void EnumerationFailures_RetainParallelAggregation(bool cancelOnFirstFailure)
    {
        InvalidOperationException expected = new("source failure");
        var source = Enumerable.Range(0, 1).Select<int, int>(_ => throw expected);

        AggregateException actual = Assert.ThrowsExactly<AggregateException>(() =>
        {
            if (cancelOnFirstFailure)
            {
                _ = Parallel.ForEachUnbufferedUntilFirstFailure(
                    source: _ => source,
                    token: CancellationToken.None,
                    maxDegreesOfParallelism: 4,
                    body: static (_, _) => Assert.Fail("The source failed before producing any items."));
            }
            else
            {
                _ = Parallel.ForEachUnbuffered(
                    source: source,
                    token: CancellationToken.None,
                    maxDegreesOfParallelism: 4,
                    body: static _ => Assert.Fail("The source failed before producing any items."));
            }
        });

        Assert.AreSame(expected, actual.Flatten().InnerExceptions.Single());
    }
}
