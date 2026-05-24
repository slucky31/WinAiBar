using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WinAIBar.Core.Models;
using WinAIBar.Core.Services.GitHub.Dto;

namespace WinAIBar.Core.Services.GitHub;

public sealed partial class CopilotUsageClient : ICopilotUsageClient
{
    private const string InternalUserEndpoint = "/copilot_internal/user";

    private readonly HttpClient _httpClient;
    private readonly IGitHubTokenStore _tokenStore;
    private readonly ILogger<CopilotUsageClient> _logger;

    public CopilotUsageClient(
        HttpClient httpClient,
        IGitHubTokenStore tokenStore,
        ILogger<CopilotUsageClient> logger)
    {
        _httpClient = httpClient;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    public async Task<ProviderSnapshot> FetchAsync(CancellationToken ct = default)
    {
        var token = await _tokenStore.LoadAsync(ct).ConfigureAwait(false);
        if (token is null)
            throw new CopilotUnauthorizedException("No GitHub token available.");

        using var internalRequest = new HttpRequestMessage(HttpMethod.Get, InternalUserEndpoint);
        internalRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage internalResponse;
        try
        {
            internalResponse = await _httpClient.SendAsync(internalRequest, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            LogRequestFailed(_logger, ex);
            throw;
        }

        using (internalResponse)
        {
            if (internalResponse.StatusCode == HttpStatusCode.Unauthorized)
                throw new CopilotUnauthorizedException($"HTTP 401 from {InternalUserEndpoint}");

            if (internalResponse.StatusCode == HttpStatusCode.NotFound)
            {
                LogEndpointMissing(_logger);
                return CreateEndpointMissingSnapshot();
            }

            internalResponse.EnsureSuccessStatusCode();

            var internalJson = await internalResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            LogInternalFetchSuccess(_logger, internalJson.Length);

            var internalDto = ParseCopilotInternalResponse(internalJson);
            var quotas = new List<UsageQuota>();

            if (internalDto.Quotas is not null)
            {
                foreach (var (key, utilization) in internalDto.Quotas)
                    quotas.Add(new UsageQuota(key, key, utilization, null, null, null, null, null));
            }

            string? premiumJson = null;
            if (internalDto.Login is not null)
            {
                List<UsageQuota> premiumQuotas;
                (premiumQuotas, premiumJson) = await FetchPremiumUsageAsync(internalDto.Login, token, ct).ConfigureAwait(false);
                quotas.AddRange(premiumQuotas);
            }

            // Combined payload so RawPayload corresponds to all quotas in the snapshot
            var rawPayload = premiumJson is not null
                ? $"{{\"internal\":{internalJson},\"premium\":{premiumJson}}}"
                : internalJson;

            return new ProviderSnapshot(ProviderId.Copilot, DateTimeOffset.UtcNow, quotas, rawPayload);
        }
    }

    private async Task<(List<UsageQuota> Quotas, string? Json)> FetchPremiumUsageAsync(string login, string token, CancellationToken ct)
    {
        var endpoint = $"/users/{login}/settings/billing/premium_request/usage";
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            LogRequestFailed(_logger, ex);
            return ([], null);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                LogPremiumUsageSkipped(_logger, (int)response.StatusCode);
                return ([], null);
            }

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            LogPremiumFetchSuccess(_logger, json.Length);
            var dto = ParsePremiumUsageResponse(json);
            return (MapToUsageQuotas(dto), json);
        }
    }

    private static CopilotInternalResponse ParseCopilotInternalResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string? login = root.TryGetProperty("login", out var loginEl) && loginEl.ValueKind == JsonValueKind.String
            ? loginEl.GetString()
            : null;

        int? chatEnabled = null;
        if (root.TryGetProperty("chat_enabled", out var chatEl) && chatEl.TryGetInt32(out var chatVal))
            chatEnabled = chatVal;

        long? accessTypeSku = null;
        if (root.TryGetProperty("access_type_sku", out var skuEl) && skuEl.TryGetInt64(out var skuVal))
            accessTypeSku = skuVal;

        Dictionary<string, double>? quotas = null;
        if (root.TryGetProperty("quotas", out var quotasEl) && quotasEl.ValueKind == JsonValueKind.Object)
        {
            quotas = [];
            foreach (var prop in quotasEl.EnumerateObject())
            {
                if (prop.Value.TryGetDouble(out var q))
                    quotas[prop.Name] = q;
            }
        }

        return new CopilotInternalResponse
        {
            Login = login,
            ChatEnabled = chatEnabled,
            AccessTypeSku = accessTypeSku,
            Quotas = quotas,
            RawData = root.Clone()
        };
    }

    private static CopilotPremiumUsageResponse ParsePremiumUsageResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var items = new List<CopilotPremiumUsageItem>();

        if (root.TryGetProperty("usageItems", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in itemsEl.EnumerateArray())
            {
                string key = item.TryGetProperty("key", out var keyEl) && keyEl.ValueKind == JsonValueKind.String
                    ? keyEl.GetString() ?? "unknown" : "unknown";
                string label = item.TryGetProperty("label", out var labelEl) && labelEl.ValueKind == JsonValueKind.String
                    ? labelEl.GetString() ?? key : key;

                double utilization = 0;
                if (item.TryGetProperty("utilization", out var utilizEl) && utilizEl.TryGetDouble(out var u))
                    utilization = u;

                DateTimeOffset? resetsAt = null;
                if (item.TryGetProperty("resets_at", out var resetsEl) && resetsEl.ValueKind == JsonValueKind.String)
                    resetsAt = resetsEl.TryGetDateTimeOffset(out var dt) ? dt : null;

                long? used = null;
                if (item.TryGetProperty("used", out var usedEl) && usedEl.TryGetInt64(out var usedVal))
                    used = usedVal;

                long? limit = null;
                if (item.TryGetProperty("limit", out var limitEl) && limitEl.TryGetInt64(out var limitVal))
                    limit = limitVal;

                string? unit = item.TryGetProperty("unit", out var unitEl) && unitEl.ValueKind == JsonValueKind.String
                    ? unitEl.GetString() : null;

                items.Add(new CopilotPremiumUsageItem
                {
                    Key = key,
                    Label = label,
                    Utilization = utilization,
                    ResetsAt = resetsAt,
                    Used = used,
                    Limit = limit,
                    Unit = unit
                });
            }
        }

        return new CopilotPremiumUsageResponse { UsageItems = items, RawData = root.Clone() };
    }

    private static List<UsageQuota> MapToUsageQuotas(CopilotPremiumUsageResponse dto) =>
        dto.UsageItems
            .Select(i => new UsageQuota(i.Key ?? "unknown", i.Label ?? i.Key ?? "unknown", i.Utilization, i.ResetsAt, i.Used, i.Limit, i.Unit, null))
            .ToList();

    private static ProviderSnapshot CreateEndpointMissingSnapshot()
    {
        var quota = new UsageQuota("endpoint_missing", "Endpoint Missing", 0.0, null, null, null, null, null);
        return new ProviderSnapshot(ProviderId.Copilot, DateTimeOffset.UtcNow, [quota], null);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Copilot usage request failed")]
    private static partial void LogRequestFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Copilot internal endpoint returned 404 — may be obsolete")]
    private static partial void LogEndpointMissing(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Copilot internal user response received ({Length} bytes)")]
    private static partial void LogInternalFetchSuccess(ILogger logger, int length);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Copilot premium usage response received ({Length} bytes)")]
    private static partial void LogPremiumFetchSuccess(ILogger logger, int length);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Copilot premium usage request returned {StatusCode}, skipping")]
    private static partial void LogPremiumUsageSkipped(ILogger logger, int statusCode);
}
