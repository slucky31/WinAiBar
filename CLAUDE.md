# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

```bash
# Restore dependencies
dotnet restore WinAIBar.slnx

# Build (CLI requires the platform flag)
dotnet build WinAIBar.slnx -p:Platform=x64

# Run all tests
dotnet test tests/WinAIBar.Tests/WinAIBar.Tests.csproj -p:Platform=x64

# Run a single test class
dotnet test tests/WinAIBar.Tests/WinAIBar.Tests.csproj -p:Platform=x64 --filter "HistoryRepositoryTests"

# Run the app
dotnet run --project src/WinAIBar -p:Platform=x64
```

> **WinUI 3 CLI note:** `EnableCoreMrtTooling=false` is required in the WinAIBar project to allow `dotnet build` from the CLI (already set).

`Directory.Build.props` sets `TreatWarningsAsErrors=true` and `EnforceCodeStyleInBuild=true` — the build will fail on any warning or style violation.

## Solution Structure

```
WinAIBar.slnx
├── src/WinAIBar/           — WinUI 3 desktop app (net9.0-windows10.0.19041.0, win-x64)
├── src/WinAIBar.Core/      — Business logic library (net9.0, no Windows dependency)
└── tests/WinAIBar.Tests/   — xUnit v3 test suite (net9.0, references Core only)
```

Only `WinAIBar.Core` is testable without a Windows App Runtime. The UI project (`WinAIBar`) is not referenced by tests.

## Architecture

### Dependency Injection & Hosting
`AppHost` (in `src/WinAIBar/`) centralizes all DI registration and is started from `App.OnLaunched()` via `Microsoft.Extensions.Hosting`.

### MVVM
CommunityToolkit.Mvvm — ViewModels extend `ObservableObject`, commands use `[RelayCommand]`. `INavigationService` is injected into ViewModels.

### Data (EF Core + SQLite)
- `WinAIBarDbContext` with `DbSet<ProviderSnapshot>` and `DbSet<UsageQuota>`
- `IHistoryRepository` abstraction over EF Core
- `DateTimeOffset` stored as `long` (Unix timestamp ticks) — do not change this convention
- EF migrations live in `src/WinAIBar.Core/Data/Migrations/`

### Background Polling
`ClaudePollingService` extends `BackgroundService`. It polls the Anthropic API on a timer, backs off on errors, and reduces frequency when the user is idle (`IIdleDetector`). Cross-component events use `WeakReferenceMessenger`.

### HTTP & Resilience
Named `HttpClient` instances (Anthropic, GitHub) registered via `IHttpClientFactory`. Polly retry policies with jitter are applied — retry on 5xx / 408 / 429.

### Auth & Credentials
- `GitHubDeviceCodeAuthenticator` — GitHub OAuth device flow
- `IGitHubTokenStore` — persists GitHub token via DPAPI
- `IAnthropicCredentialProvider` — reads the Claude API key

### Logging
Serilog with Debug and File (rolling daily) sinks. Use `[LoggerMessage]` source-generated attributes for hot-path log calls. Logs land in `%LOCALAPPDATA%\WinAIBar\logs\`.

## Testing Conventions

- **Framework:** xUnit v3 + NSubstitute
- **In-memory SQLite:** Repository tests use `DataSource=:memory:` and `IAsyncLifetime` for setup/teardown
- **HTTP mocking:** Custom `HttpMessageHandler` subclasses, not mocked interfaces
- **Test fixtures:** JSON files in `tests/WinAIBar.Tests/TestData/` (copied to output)

## Package Management

All NuGet versions are centrally pinned in `Directory.Packages.props`. Add new packages there with `<PackageVersion>`, then reference with `<PackageReference>` (no version attribute) in the `.csproj`.

## CI

GitHub Actions (`.github/workflows/build.yml`) runs on `windows-latest` for every push/PR to `main`: restore → build Release x64 → test. PR titles must follow conventional commits (`semantic.yml`).
