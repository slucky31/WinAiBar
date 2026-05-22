using WinAIBar.Core.Models;
using Xunit;

namespace WinAIBar.Tests.Models;

public sealed class UsageQuotaTests
{
    [Fact]
    public void ConstructsWithExpectedProperties()
    {
        var resetsAt = DateTimeOffset.UtcNow.AddHours(4);
        var quota = new UsageQuota(
            Key: "session-5h",
            Label: "Session 5h",
            Utilization: 0.64,
            ResetsAt: resetsAt,
            Used: 32000,
            Limit: 50000,
            Unit: "tokens",
            Model: "sonnet");

        Assert.Equal("session-5h", quota.Key);
        Assert.Equal("Session 5h", quota.Label);
        Assert.Equal(0.64, quota.Utilization);
        Assert.Equal(resetsAt, quota.ResetsAt);
        Assert.Equal(32000L, quota.Used);
        Assert.Equal(50000L, quota.Limit);
        Assert.Equal("tokens", quota.Unit);
        Assert.Equal("sonnet", quota.Model);
    }

    [Fact]
    public void NullableFieldsAreNullWhenOmitted()
    {
        var quota = new UsageQuota(
            Key: "weekly-all",
            Label: "Weekly · All",
            Utilization: 0.0,
            ResetsAt: null,
            Used: null,
            Limit: null,
            Unit: null,
            Model: null);

        Assert.Null(quota.ResetsAt);
        Assert.Null(quota.Used);
        Assert.Null(quota.Limit);
        Assert.Null(quota.Unit);
        Assert.Null(quota.Model);
    }

    [Fact]
    public void OverageUtilizationExceedsOne()
    {
        var quota = new UsageQuota(
            Key: "session-5h",
            Label: "Session 5h",
            Utilization: 1.2,
            ResetsAt: null,
            Used: 60000,
            Limit: 50000,
            Unit: "tokens",
            Model: null);

        Assert.True(quota.Utilization > 1.0);
    }
}
