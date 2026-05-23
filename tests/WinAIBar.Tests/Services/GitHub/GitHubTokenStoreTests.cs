using System.Runtime.Versioning;
using Microsoft.Extensions.Logging.Abstractions;
using WinAIBar.Core.Services.GitHub;
using Xunit;

namespace WinAIBar.Tests.Services.GitHub;

[SupportedOSPlatform("windows")]
public sealed class GitHubTokenStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly GitHubTokenStore _store;

    public GitHubTokenStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"WinAIBarTests{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _store = new GitHubTokenStore(
            Path.Combine(_tempDir, "github.token"),
            NullLogger<GitHubTokenStore>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task SaveThenLoadReturnsOriginalToken()
    {
        const string token = "gho_testtoken1234567890abcdef";

        await _store.SaveAsync(token, TestContext.Current.CancellationToken);
        var loaded = await _store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(token, loaded);
    }

    [Fact]
    public void HasTokenReturnsFalseWhenNoTokenSaved()
    {
        Assert.False(_store.HasToken());
    }

    [Fact]
    public async Task HasTokenReturnsTrueAfterSave()
    {
        await _store.SaveAsync("gho_sometoken", TestContext.Current.CancellationToken);

        Assert.True(_store.HasToken());
    }

    [Fact]
    public async Task ClearAsyncRemovesToken()
    {
        await _store.SaveAsync("gho_tobedeleted", TestContext.Current.CancellationToken);
        Assert.True(_store.HasToken());

        await _store.ClearAsync(TestContext.Current.CancellationToken);

        Assert.False(_store.HasToken());
        var loaded = await _store.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Null(loaded);
    }

    [Fact]
    public async Task LoadAsyncReturnsNullWhenNoToken()
    {
        var result = await _store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsyncOverwritesExistingToken()
    {
        await _store.SaveAsync("gho_first", TestContext.Current.CancellationToken);
        await _store.SaveAsync("gho_second", TestContext.Current.CancellationToken);

        var loaded = await _store.LoadAsync(TestContext.Current.CancellationToken);
        Assert.Equal("gho_second", loaded);
    }

    [Fact]
    public async Task ClearAsyncIsNoOpWhenNoToken()
    {
        await _store.ClearAsync(TestContext.Current.CancellationToken);
        Assert.False(_store.HasToken());
    }
}
