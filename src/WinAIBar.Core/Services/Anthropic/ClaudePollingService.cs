using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WinAIBar.Core.Data.Abstractions;
using WinAIBar.Core.Infrastructure;
using WinAIBar.Core.Models;

namespace WinAIBar.Core.Services.Anthropic;

public sealed partial class ClaudePollingService : BackgroundService
{
    internal static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(5);
    internal static readonly TimeSpan NormalInterval = TimeSpan.FromMinutes(7);
    private static readonly TimeSpan IdleInterval = TimeSpan.FromMinutes(20);
    private static readonly TimeSpan UnauthorizedWait = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan IdleThreshold = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromHours(1);

    private readonly IAnthropicCredentialProvider _credentialProvider;
    private readonly IAnthropicUsageClient _usageClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClaudeStateService _stateService;
    private readonly IIdleDetector _idleDetector;
    private readonly ILogger<ClaudePollingService> _logger;
    private readonly TimeProvider _timeProvider;

    public ClaudePollingService(
        IAnthropicCredentialProvider credentialProvider,
        IAnthropicUsageClient usageClient,
        IServiceScopeFactory scopeFactory,
        IClaudeStateService stateService,
        IIdleDetector idleDetector,
        ILogger<ClaudePollingService> logger,
        TimeProvider timeProvider)
    {
        _credentialProvider = credentialProvider;
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
                if (!_credentialProvider.IsAvailable())
                {
                    HandleUnavailableCredentials();
                    await Task.Delay(UnauthorizedWait, _timeProvider, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var snapshot = await _usageClient.FetchAsync(stoppingToken).ConfigureAwait(false);
                sw.Stop();

                await PersistSnapshotAsync(snapshot, stoppingToken).ConfigureAwait(false);

                var health = new ProviderHealth(
                    ProviderId.Claude,
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
            catch (AnthropicUnauthorizedException)
            {
                consecutiveErrors++;
                var health = new ProviderHealth(
                    ProviderId.Claude, ProviderStatus.Unauthorized,
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
                    ProviderId.Claude, ProviderStatus.Failed,
                    _timeProvider.GetUtcNow(), null, ex.Message, null);
                _stateService.UpdateHealth(health);
                LogPollingError(_logger, ex, backoff);
                await Task.Delay(backoff, _timeProvider, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private void HandleUnavailableCredentials()
    {
        var now = _timeProvider.GetUtcNow();
        var snapshot = new ProviderSnapshot(ProviderId.Claude, now, [], null);
        var health = new ProviderHealth(
            ProviderId.Claude, ProviderStatus.Unauthorized,
            now, null, "Credentials not available", null);
        _stateService.UpdateSnapshot(snapshot);
        _stateService.UpdateHealth(health);
        WeakReferenceMessenger.Default.Send(snapshot);
        LogCredentialsUnavailable(_logger);
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

    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "Claude snapshot captured: {QuotaCount} quotas")]
    private static partial void LogSnapshotCaptured(ILogger logger, int quotaCount);

    [LoggerMessage(EventId = 11, Level = LogLevel.Warning, Message = "Claude polling skipped: credentials not available")]
    private static partial void LogCredentialsUnavailable(ILogger logger);

    [LoggerMessage(EventId = 12, Level = LogLevel.Warning, Message = "Claude polling skipped: unauthorized (401)")]
    private static partial void LogUnauthorized(ILogger logger);

    [LoggerMessage(EventId = 13, Level = LogLevel.Error, Message = "Claude polling error, next attempt in {Backoff}")]
    private static partial void LogPollingError(ILogger logger, Exception ex, TimeSpan backoff);
}
