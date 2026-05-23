using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WinAIBar.Core.Data.Abstractions;
using WinAIBar.Core.Infrastructure;
using WinAIBar.Core.Models;
using WinAIBar.Core.Services.Anthropic;
using Xunit;

namespace WinAIBar.Tests.Services.Anthropic;

public sealed class ClaudePollingServiceTests
{
    private static readonly ProviderSnapshot ValidSnapshot = new(
        ProviderId.Claude,
        DateTimeOffset.UtcNow,
        [new UsageQuota("session-5h", "Session 5h", 0.42, null, 210000, 500000, "tokens", "sonnet")],
        """{"data":{}}""");

    private static ClaudePollingService CreateService(
        IAnthropicCredentialProvider credProvider,
        IAnthropicUsageClient usageClient,
        IHistoryRepository historyRepo,
        IClaudeStateService stateService,
        FakeTimeProvider timeProvider)
    {
        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IHistoryRepository)).Returns(historyRepo);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(sp);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var idleDetector = Substitute.For<IIdleDetector>();
        idleDetector.GetIdleTime().Returns(TimeSpan.Zero);

        return new ClaudePollingService(
            credProvider,
            usageClient,
            scopeFactory,
            stateService,
            idleDetector,
            NullLogger<ClaudePollingService>.Instance,
            timeProvider);
    }

    [Fact]
    public async Task ThreeSuccessfulFetchesSaveThreeSnapshots()
    {
        var ct = TestContext.Current.CancellationToken;
        var fakeTime = new FakeTimeProvider();

        var credProvider = Substitute.For<IAnthropicCredentialProvider>();
        credProvider.IsAvailable().Returns(true);

        var historyRepo = Substitute.For<IHistoryRepository>();
        historyRepo.SaveAsync(Arg.Any<ProviderSnapshot>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var stateService = Substitute.For<IClaudeStateService>();
        var usageClient = Substitute.For<IAnthropicUsageClient>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var callCount = 0;

        usageClient.FetchAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (Interlocked.Increment(ref callCount) == 3)
                cts.Cancel();
            return Task.FromResult(ValidSnapshot);
        });

        using var service = CreateService(credProvider, usageClient, historyRepo, stateService, fakeTime);
        await service.StartAsync(cts.Token);
        await service.StopAsync(CancellationToken.None);

        await historyRepo.Received(3).SaveAsync(ValidSnapshot, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConsecutiveErrorsApplyExponentialBackoff()
    {
        var ct = TestContext.Current.CancellationToken;
        var fakeTime = new FakeTimeProvider();

        var credProvider = Substitute.For<IAnthropicCredentialProvider>();
        credProvider.IsAvailable().Returns(true);

        var historyRepo = Substitute.For<IHistoryRepository>();
        historyRepo.SaveAsync(Arg.Any<ProviderSnapshot>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var stateService = Substitute.For<IClaudeStateService>();
        var usageClient = Substitute.For<IAnthropicUsageClient>();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var callCount = 0;

        usageClient.FetchAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            var n = Interlocked.Increment(ref callCount);
            if (n == 1) throw new HttpRequestException("Server error");
            if (n == 2) throw new HttpRequestException("Server error");
            cts.Cancel();
            return Task.FromResult(ValidSnapshot);
        });

        using var service = CreateService(credProvider, usageClient, historyRepo, stateService, fakeTime);
        await service.StartAsync(cts.Token);
        await service.StopAsync(CancellationToken.None);

        // delays: [0]=InitialDelay, [1]=backoff after error 1, [2]=backoff after error 2
        Assert.True(fakeTime.RecordedDelays.Count >= 3);
        Assert.Equal(ClaudePollingService.InitialDelay, fakeTime.RecordedDelays[0]);
        Assert.Equal(ClaudePollingService.ComputeBackoff(1), fakeTime.RecordedDelays[1]);
        Assert.Equal(ClaudePollingService.ComputeBackoff(2), fakeTime.RecordedDelays[2]);
        Assert.True(fakeTime.RecordedDelays[2] > fakeTime.RecordedDelays[1]);
    }

    [Fact]
    public void BackoffGrowsExponentiallyAndCappsAtOneHour()
    {
        var b1 = ClaudePollingService.ComputeBackoff(1);
        var b2 = ClaudePollingService.ComputeBackoff(2);
        var b3 = ClaudePollingService.ComputeBackoff(3);
        var b10 = ClaudePollingService.ComputeBackoff(10);

        Assert.Equal(ClaudePollingService.NormalInterval, b1);
        Assert.Equal(ClaudePollingService.NormalInterval * 2, b2);
        Assert.Equal(ClaudePollingService.NormalInterval * 4, b3);
        Assert.Equal(TimeSpan.FromHours(1), b10);
    }

    // Fires timers instantly and records all requested delays
    internal sealed class FakeTimeProvider : TimeProvider
    {
        private readonly List<TimeSpan> _delays = [];
        public IReadOnlyList<TimeSpan> RecordedDelays => _delays;

        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            _delays.Add(dueTime);
            callback(state);
            return new NoOpTimer();
        }

        private sealed class NoOpTimer : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => true;
            public void Dispose() { }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
