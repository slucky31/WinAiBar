using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WinAIBar.Core.Models;
using WinAIBar.Core.Services.Anthropic.Dto;

namespace WinAIBar.Core.Services.Anthropic;

public sealed partial class AnthropicUsageClient : IAnthropicUsageClient
{
    private const string UsageEndpoint = "/api/oauth/usage";

    private readonly HttpClient _httpClient;
    private readonly IAnthropicCredentialProvider _credentialProvider;
    private readonly ILogger<AnthropicUsageClient> _logger;

    public AnthropicUsageClient(
        HttpClient httpClient,
        IAnthropicCredentialProvider credentialProvider,
        ILogger<AnthropicUsageClient> logger)
    {
        _httpClient = httpClient;
        _credentialProvider = credentialProvider;
        _logger = logger;
    }

    public async Task<ProviderSnapshot> FetchAsync(CancellationToken ct = default)
    {
        var credentials = await _credentialProvider.GetAsync(ct).ConfigureAwait(false);
        if (credentials is null)
            throw new AnthropicUnauthorizedException("No Anthropic credentials available.");

        using var request = new HttpRequestMessage(HttpMethod.Get, UsageEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.AccessToken);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            LogRequestFailed(_logger, ex);
            throw;
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new AnthropicUnauthorizedException($"HTTP 401 from {UsageEndpoint}");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            LogFetchSuccess(_logger, json.Length);
            return ParseSnapshot(json);
        }
    }

    private static ProviderSnapshot ParseSnapshot(string json)
    {
        var dto = ParseUsageResponse(json);
        var quotas = dto.Quotas
            .Select(kvp => MapToUsageQuota(kvp.Key, kvp.Value))
            .ToList();

        return new ProviderSnapshot(ProviderId.Claude, DateTimeOffset.UtcNow, quotas, json);
    }

    private static AnthropicUsageResponse ParseUsageResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var rawData = doc.RootElement.Clone();
        var quotas = new Dictionary<string, AnthropicQuotaDto>();

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var dto = ParseQuotaDto(prop.Value);
            if (dto is not null)
                quotas[prop.Name] = dto;
        }

        return new AnthropicUsageResponse { RawData = rawData, Quotas = quotas };
    }

    private static AnthropicQuotaDto? ParseQuotaDto(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        double utilization = 0;
        if (element.TryGetProperty("utilization", out var utilizationEl) &&
            utilizationEl.ValueKind == JsonValueKind.Number &&
            utilizationEl.TryGetDouble(out var u))
            utilization = u / 100.0;

        DateTimeOffset? resetsAt = null;
        if (element.TryGetProperty("resets_at", out var resetsAtEl))
        {
            if (resetsAtEl.ValueKind == JsonValueKind.String)
                resetsAt = resetsAtEl.TryGetDateTimeOffset(out var dt) ? dt : null;
            else if (resetsAtEl.ValueKind == JsonValueKind.Number && resetsAtEl.TryGetInt64(out var ms))
                resetsAt = DateTimeOffset.FromUnixTimeMilliseconds(ms);
        }

        long? used = null;
        if (element.TryGetProperty("used", out var usedEl) &&
            usedEl.ValueKind == JsonValueKind.Number &&
            usedEl.TryGetInt64(out var usedVal))
            used = usedVal;

        long? limit = null;
        if (element.TryGetProperty("limit", out var limitEl) &&
            limitEl.ValueKind == JsonValueKind.Number &&
            limitEl.TryGetInt64(out var limitVal))
            limit = limitVal;

        string? label = element.TryGetProperty("label", out var labelEl) ? labelEl.GetString() : null;

        return new AnthropicQuotaDto { Utilization = utilization, ResetsAt = resetsAt, Used = used, Limit = limit, Label = label };
    }

    private static UsageQuota MapToUsageQuota(string key, AnthropicQuotaDto dto) =>
        new(key, dto.Label ?? key, dto.Utilization, dto.ResetsAt, dto.Used, dto.Limit, null, null);

    [LoggerMessage(Level = LogLevel.Error, Message = "Anthropic usage request failed")]
    private static partial void LogRequestFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Anthropic usage response received ({Length} bytes)")]
    private static partial void LogFetchSuccess(ILogger logger, int length);
}
