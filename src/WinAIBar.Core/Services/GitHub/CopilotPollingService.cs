using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WinAIBar.Core.Data.Abstractions;
using WinAIBar.Core.Infrastructure;
using WinAIBar.Core.Models;

namespace WinAIBar.Core.Services.GitHub;

public sealed partial class CopilotPollingService : BackgroundService
{
    internal static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(5);
    internal static readonly TimeSpan NormalInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan IdleInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan UnauthorizedWait = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan IdleThreshold = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromHours(1);

    private readonly IGitHubTokenStore _tokenStore;
    private readonly ICopilotUsageClient _usageClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICopilotStateService _stateService;
    private readonly IIdleDetector _idleDetector;
    private readonly ILogger<CopilotPollingService> _logger;
    private readonly TimeProvider _timeProvider;

    public CopilotPollingService(
        IGitHubTokenStore tokenStore,
        ICopilotUsageClient usageClient,
        IServiceScopeFactory scopeFactory,
        ICopilotStateService stateService,
        IIdleDetector idleDetector,
        ILogger<CopilotPollingService> logger,
        TimeProvider timeProvider)
    {
        _tokenStore = tokenStore;
        _usageClient = usageClient;
        _scopeFactory = scopeFactory;
        _stateService = stateService;
        _idleDetector = idleDetector;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(InitialDelay, _timeProvider, stoppingToken).ConfigureAwait(false);

        var consecutiveErrors = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_tokenStore.HasToken())
                {
                    HandleNoToken();
                    await Task.Delay(UnauthorizedWait, _timeProvider, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var snapshot = await _usageClient.FetchAsync(stoppingToken).ConfigureAwait(false);
                sw.Stop();

                await PersistSnapshotAsync(snapshot, stoppingToken).ConfigureAwait(false);

                var health = new ProviderHealth(
                    ProviderId.Copilot,
                    ProviderStatus.Healthy,
                    _timeProvider.GetUtcNow(),
                    200, null, sw.Elapsed);

                _stateService.UpdateSnapshot(snapshot);
                _stateService.UpdateHealth(health);
                WeakReferenceMessenger.Default.Send(snapshot);

                LogSnapshotCaptured(_logger, snapshot.Quotas.Count);
                consecutiveErrors = 0;

                var interval = GetPollingInterval();
                await Task.Delay(interval, _timeProvider, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (CopilotUnauthorizedException)
            {
                consecutiveErrors++;
                var health = new ProviderHealth(
                    ProviderId.Copilot, ProviderStatus.Unauthorized,
                    _timeProvider.GetUtcNow(), 401, "Unauthorized", null);
                _stateService.UpdateHealth(health);
                LogUnauthorized(_logger);
                await Task.Delay(UnauthorizedWait, _timeProvider, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                consecutiveErrors++;
                var backoff = ComputeBackoff(consecutiveErrors);
                var health = new ProviderHealth(
                    ProviderId.Copilot, ProviderStatus.Failed,
                    _timeProvider.GetUtcNow(), null, ex.Message, null);
                _stateService.UpdateHealth(health);
                LogPollingError(_logger, ex, backoff);
                await Task.Delay(backoff, _timeProvider, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private void HandleNoToken()
    {
        var now = _timeProvider.GetUtcNow();
        var snapshot = new ProviderSnapshot(ProviderId.Copilot, now, [], null);
        var health = new ProviderHealth(
            ProviderId.Copilot, ProviderStatus.Unauthorized,
            now, null, "No token available", null);
        _stateService.UpdateSnapshot(snapshot);
        _stateService.UpdateHealth(health);
        WeakReferenceMessenger.Default.Send(snapshot);
        LogNoToken(_logger);
    }

    private async Task PersistSnapshotAsync(ProviderSnapshot snapshot, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IHistoryRepository>();
        await repo.SaveAsync(snapshot, ct).ConfigureAwait(false);
    }

    private TimeSpan GetPollingInterval()
        => _idleDetector.GetIdleTime() >= IdleThreshold ? IdleInterval : NormalInterval;

    internal static TimeSpan ComputeBackoff(int consecutiveErrors)
    {
        var minutes = NormalInterval.TotalMinutes * Math.Pow(2, consecutiveErrors - 1);
        return TimeSpan.FromMinutes(Math.Min(MaxBackoff.TotalMinutes, minutes));
    }

    [LoggerMessage(EventId = 20, Level = LogLevel.Information, Message = "Copilot snapshot captured: {QuotaCount} quotas")]
    private static partial void LogSnapshotCaptured(ILogger logger, int quotaCount);

    [LoggerMessage(EventId = 21, Level = LogLevel.Warning, Message = "Copilot polling skipped: no token")]
    private static partial void LogNoToken(ILogger logger);

    [LoggerMessage(EventId = 22, Level = LogLevel.Warning, Message = "Copilot polling skipped: unauthorized (401)")]
    private static partial void LogUnauthorized(ILogger logger);

    [LoggerMessage(EventId = 23, Level = LogLevel.Error, Message = "Copilot polling error, next attempt in {Backoff}")]
    private static partial void LogPollingError(ILogger logger, Exception ex, TimeSpan backoff);
}
