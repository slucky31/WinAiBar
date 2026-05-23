using Microsoft.Extensions.Logging.Abstractions;
using WinAIBar.Core.Services.Anthropic;
using Xunit;

namespace WinAIBar.Tests.Services.Anthropic;

public sealed class AnthropicCredentialProviderTests
{
    private static AnthropicCredentialProvider CreateProvider(Func<string, string?> fileReader) =>
        new(fileReader, NullLogger<AnthropicCredentialProvider>.Instance);

    [Fact]
    public async Task GetAsyncReturnsNullWhenFileAbsent()
    {
        var provider = CreateProvider(_ => null);

        var result = await provider.GetAsync(TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsyncReturnsCredentialsWhenValidJson()
    {
        var json = await File.ReadAllTextAsync(
            "TestData/anthropic-credentials-valid.json",
            TestContext.Current.CancellationToken);
        var provider = CreateProvider(_ => json);

        var result = await provider.GetAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("sk-ant-oaut02-test-access-token-valid", result.AccessToken);
        Assert.NotNull(result.RefreshToken);
        Assert.NotNull(result.ExpiresAt);
    }

    [Fact]
    public async Task GetAsyncReturnsNullWhenInvalidJson()
    {
        var provider = CreateProvider(_ => "{ not: valid json {{{");

        var result = await provider.GetAsync(TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsyncReturnsNullWhenTokenExpired()
    {
        var json = await File.ReadAllTextAsync(
            "TestData/anthropic-credentials-expired.json",
            TestContext.Current.CancellationToken);
        var provider = CreateProvider(_ => json);

        var result = await provider.GetAsync(TestContext.Current.CancellationToken);

        Assert.Null(result);
    }
}
