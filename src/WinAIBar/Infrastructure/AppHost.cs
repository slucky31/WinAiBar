using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Serilog;
using WinAIBar.Services.Navigation;
using WinAIBar.ViewModels;
using WinAIBar.Views;

namespace WinAIBar.Infrastructure;

public static partial class AppHost
{
    private static IHost? _current;

    public static IHost Current =>
        _current ?? throw new InvalidOperationException("AppHost has not been started yet. Call AppHost.StartAsync() first.");

    public static async Task StartAsync()
    {
        if (_current is not null)
        {
            return;
        }

        var builder = Host.CreateApplicationBuilder();

        builder.Configuration
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        ConfigureLogging(builder);

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger, dispose: true);

        ConfigureServices(builder.Services);

        var host = builder.Build();
        await host.StartAsync().ConfigureAwait(false);
        _current = host;

        var logger = host.Services.GetRequiredService<ILogger<HostMarker>>();
        if (logger.IsEnabled(LogLevel.Information))
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
            LogStarted(logger, version);
        }
    }

    public static async Task StopAsync()
    {
        if (_current is null)
        {
            return;
        }

        try
        {
            await _current.StopAsync().ConfigureAwait(false);
        }
        finally
        {
            _current.Dispose();
            _current = null;
            await Log.CloseAndFlushAsync().ConfigureAwait(false);
        }
    }

    private static void ConfigureLogging(HostApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(
                path: Path.Combine(PathProvider.LogsDirectory, "winaibar-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddTransient<ShellViewModel>();
        services.AddTransient<Shell>();

        services.AddHttpClient(Options.DefaultName)
            .AddResilienceHandler("default", static builder =>
            {
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .HandleResult(r =>
                            (int)r.StatusCode >= 500 ||
                            r.StatusCode == HttpStatusCode.RequestTimeout ||
                            r.StatusCode == HttpStatusCode.TooManyRequests),
                    DelayGenerator = static args =>
                    {
                        var retryAfter = args.Outcome.Result?.Headers?.RetryAfter;
                        if (retryAfter?.Delta is { } delta && delta > TimeSpan.Zero)
                            return new ValueTask<TimeSpan?>(delta);
                        if (retryAfter?.Date is { } date)
                        {
                            var wait = date - DateTimeOffset.UtcNow;
                            if (wait > TimeSpan.Zero) return new ValueTask<TimeSpan?>(wait);
                        }
                        return new ValueTask<TimeSpan?>((TimeSpan?)null);
                    }
                });
            });
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "WinAIBar started v{Version}")]
    private static partial void LogStarted(Microsoft.Extensions.Logging.ILogger logger, string version);

    private sealed class HostMarker
    {
    }
}
