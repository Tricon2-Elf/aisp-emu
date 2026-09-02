using aisp.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public class SessionWorkSchedulerTests
{
    [Fact]
    public async Task SlowSession_DoesNotBlockOtherSession()
    {
        var slowId = Guid.NewGuid();
        var fastId = Guid.NewGuid();
        var slowStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var slowRelease = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var fastDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var scheduler = new SessionWorkScheduler<(Guid Id, string Name)>(
            queueCapacity: 16,
            async (work, ct) =>
            {
                if (work.Name == "slow")
                {
                    slowStarted.TrySetResult();
                    await slowRelease.Task.WaitAsync(ct);
                    return;
                }

                fastDone.TrySetResult();
            },
            TestContext.Current.CancellationToken,
            NullLogger.Instance
        );

        try
        {
            Assert.True(await scheduler.EnqueueAsync(slowId, (slowId, "slow")));
            await slowStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

            Assert.True(await scheduler.EnqueueAsync(fastId, (fastId, "fast")));
            var completed = await Task.WhenAny(
                fastDone.Task,
                Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)
            );

            Assert.Same(fastDone.Task, completed);
            Assert.True(fastDone.Task.IsCompletedSuccessfully);
        }
        finally
        {
            slowRelease.TrySetResult();
        }
    }

    [Fact]
    public async Task SameSession_PacketsStayInOrder()
    {
        var sessionId = Guid.NewGuid();
        var seen = new List<int>();
        var firstStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var firstRelease = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var secondDone = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var gate = new object();

        await using var scheduler = new SessionWorkScheduler<int>(
            queueCapacity: 16,
            async (work, ct) =>
            {
                if (work == 1)
                {
                    firstStarted.TrySetResult();
                    await firstRelease.Task.WaitAsync(ct);
                }

                lock (gate)
                    seen.Add(work);

                if (work == 2)
                    secondDone.TrySetResult();
            },
            TestContext.Current.CancellationToken,
            NullLogger.Instance
        );

        try
        {
            Assert.True(await scheduler.EnqueueAsync(sessionId, 1));
            await firstStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
            Assert.True(await scheduler.EnqueueAsync(sessionId, 2));
            firstRelease.TrySetResult();
            await secondDone.Task.WaitAsync(TestContext.Current.CancellationToken);

            Assert.Equal([1, 2], seen);
        }
        finally
        {
            firstRelease.TrySetResult();
        }
    }

    [Fact]
    public async Task EnqueueAsync_WaitsWhenSessionQueueIsFull()
    {
        var sessionId = Guid.NewGuid();
        var firstStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thirdDispatched = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        await using var scheduler = new SessionWorkScheduler<int>(
            queueCapacity: 1,
            async (work, ct) =>
            {
                if (work == 1)
                    firstStarted.TrySetResult();
                if (work == 3)
                    thirdDispatched.TrySetResult();
                await release.Task.WaitAsync(ct);
            },
            TestContext.Current.CancellationToken,
            NullLogger.Instance
        );

        try
        {
            Assert.True(await scheduler.EnqueueAsync(sessionId, 1));
            await firstStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
            Assert.True(await scheduler.EnqueueAsync(sessionId, 2));

            var waiting = scheduler.EnqueueAsync(sessionId, 3).AsTask();
            var raced = await Task.WhenAny(
                waiting,
                Task.Delay(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken)
            );
            Assert.NotSame(waiting, raced);

            release.TrySetResult();
            Assert.True(await waiting.WaitAsync(TestContext.Current.CancellationToken));
            await thirdDispatched.Task.WaitAsync(TestContext.Current.CancellationToken);
        }
        finally
        {
            release.TrySetResult();
        }
    }

    [Fact]
    public async Task DispatchException_DoesNotStopLaterPacketsForSameSession()
    {
        var sessionId = Guid.NewGuid();
        var secondDone = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        await using var scheduler = new SessionWorkScheduler<int>(
            queueCapacity: 16,
            (work, _) =>
            {
                if (work == 1)
                    throw new InvalidOperationException("boom");

                secondDone.TrySetResult();
                return Task.CompletedTask;
            },
            TestContext.Current.CancellationToken,
            NullLogger.Instance
        );

        Assert.True(await scheduler.EnqueueAsync(sessionId, 1));
        Assert.True(await scheduler.EnqueueAsync(sessionId, 2));
        await secondDone.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.True(secondDone.Task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task CompleteSession_StopsFurtherDispatch()
    {
        var sessionId = Guid.NewGuid();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatched = 0;

        await using var scheduler = new SessionWorkScheduler<int>(
            queueCapacity: 16,
            async (work, ct) =>
            {
                Interlocked.Increment(ref dispatched);
                started.TrySetResult();
                await release.Task.WaitAsync(ct);
            },
            TestContext.Current.CancellationToken,
            NullLogger.Instance
        );

        try
        {
            Assert.True(await scheduler.EnqueueAsync(sessionId, 1));
            await started.Task.WaitAsync(TestContext.Current.CancellationToken);
            scheduler.CompleteSession(sessionId);
            Assert.False(await scheduler.EnqueueAsync(sessionId, 2));
        }
        finally
        {
            release.TrySetResult();
        }

        await scheduler.DisposeAsync();
        Assert.Equal(1, Volatile.Read(ref dispatched));
    }

    [Fact]
    public async Task CompleteSession_WithoutEnqueue_DoesNotRetainState()
    {
        await using var scheduler = new SessionWorkScheduler<int>(
            queueCapacity: 16,
            (_, _) => Task.CompletedTask,
            TestContext.Current.CancellationToken,
            NullLogger.Instance
        );

        scheduler.CompleteSession(Guid.NewGuid());
        scheduler.CompleteSession(Guid.NewGuid());

        Assert.Equal(0, scheduler.TrackedSessionCount);
    }

    [Fact]
    public async Task CompleteSession_ReclaimsStateAfterRunnerFinishes()
    {
        var sessionId = Guid.NewGuid();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var scheduler = new SessionWorkScheduler<int>(
            queueCapacity: 16,
            async (_, ct) =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(ct);
            },
            TestContext.Current.CancellationToken,
            NullLogger.Instance
        );

        Assert.True(await scheduler.EnqueueAsync(sessionId, 1));
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, scheduler.TrackedSessionCount);

        scheduler.CompleteSession(sessionId);
        Assert.False(await scheduler.EnqueueAsync(sessionId, 2));
        release.TrySetResult();

        await WaitUntilAsync(
            () => scheduler.TrackedSessionCount == 0,
            TestContext.Current.CancellationToken
        );
        Assert.Equal(0, scheduler.TrackedSessionCount);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
                throw new TimeoutException(
                    "Timed out waiting for scheduler to reclaim session state"
                );
            await Task.Delay(10, ct);
        }
    }
}
