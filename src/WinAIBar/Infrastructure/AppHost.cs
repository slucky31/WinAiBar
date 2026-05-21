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
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Serilog;

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

        var builder = Host.CreateApplicationBuilder();

        builder.Configuration
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger, dispose: true);

        ConfigureServices(builder.Services);

        var host = builder.Build();
        await host.StartAsync().ConfigureAwait(false);
        _current = host;

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
        LogStarted(host.Services.GetRequiredService<ILogger<HostMarker>>(), version);
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

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient(string.Empty)
            .AddPolicyHandler((_, _) => BuildRetryPolicy());
    }

    private static AsyncRetryPolicy<HttpResponseMessage> BuildRetryPolicy()
    {
        var jitter = Random.Shared;

        return Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(response =>
                (int)response.StatusCode >= 500 ||
                response.StatusCode == HttpStatusCode.RequestTimeout ||
                response.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: (attempt, outcome, _) =>
                {
                    var retryAfter = outcome?.Result?.Headers?.RetryAfter;
                    if (retryAfter is not null)
                    {
                        if (retryAfter.Delta is { } delta && delta > TimeSpan.Zero)
                        {
                            return delta;
                        }

                        if (retryAfter.Date is { } date)
                        {
                            var wait = date - DateTimeOffset.UtcNow;
                            if (wait > TimeSpan.Zero)
                            {
                                return wait;
                            }
                        }
                    }

                    var baseMs = Math.Pow(2, attempt) * 200;
                    var jitterMs = jitter.Next(0, 200);
                    return TimeSpan.FromMilliseconds(baseMs + jitterMs);
                },
                onRetryAsync: (_, _, _, _) => Task.CompletedTask);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "WinAIBar started v{Version}")]
    private static partial void LogStarted(Microsoft.Extensions.Logging.ILogger logger, string version);

    private sealed class HostMarker
    {
    }
}
