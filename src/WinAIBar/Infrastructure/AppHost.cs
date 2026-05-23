using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Serilog;
using System.Globalization;
using System.Net;
using System.Reflection;
using WinAIBar.Core.Data;
using WinAIBar.Core.Data.Abstractions;
using WinAIBar.Core.Services.Navigation;
using WinAIBar.Core.ViewModels;
using WinAIBar.Services.Navigation;
using WinAIBar.Views;

namespace WinAIBar.Infrastructure;

public static partial class AppHost
{
    private static IHost? _current;

    public static IHost Current =>
        _current ?? throw new InvalidOperationException("AppHost has not been started yet. Call AppHost.StartAsync() first.");

    public static async Task StartAsync()
    {
        // Single-threaded startup: OnLaunched is always called once on the UI thread.
        if (_current is not null)
            return;

        var builder = Host.CreateApplicationBuilder();

        builder.Configuration
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        builder.Logging.ClearProviders();
        ConfigureLogging();
        builder.Logging.AddSerilog(Log.Logger, dispose: true);

        ConfigureServices(builder.Services);

        var host = builder.Build();
        await host.StartAsync().ConfigureAwait(false);

        try
        {
            using var scope = host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<WinAIBarDbContext>();
            await dbContext.Database.MigrateAsync().ConfigureAwait(false);
        }
        catch
        {
            await host.StopAsync().ConfigureAwait(false);
            host.Dispose();
            throw;
        }

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
            return;

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

    private static void ConfigureLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(
                path: Path.Combine(PathProvider.Instance.LogsDirectory, "winaibar-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IPathProvider>(PathProvider.Instance);

        services.AddSingleton<IPageRouter, PageRouter>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<INavigationFrame>(sp =>
            (INavigationFrame)sp.GetRequiredService<INavigationService>());

        services.AddTransient<ShellViewModel>();
        services.AddTransient<Shell>();
        services.AddTransient<MainWindow>();

        services.AddDbContext<WinAIBarDbContext>((sp, options) =>
        {
            var paths = sp.GetRequiredService<IPathProvider>();
            options.UseSqlite($"Data Source={Path.Combine(paths.DataDirectory, "history.db")}");
        });
        services.AddScoped<IHistoryRepository, HistoryRepository>();

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

    private sealed class HostMarker { }
}
