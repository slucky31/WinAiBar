using WinAIBar.Core.Infrastructure;
using Xunit;

namespace WinAIBar.Tests.Infrastructure;

public sealed class TimeFormatHelperTests
{
    private static readonly DateTimeOffset Now = new(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NullResetsAtReturnsEmpty()
    {
        Assert.Equal(string.Empty, TimeFormatHelper.FormatReset(null, Now));
    }

    [Fact]
    public void PastResetsAtReturnsExpired()
    {
        Assert.Equal("expired", TimeFormatHelper.FormatReset(Now.AddMinutes(-1), Now));
    }

    [Fact]
    public void ExactlyNowResetsAtReturnsExpired()
    {
        Assert.Equal("expired", TimeFormatHelper.FormatReset(Now, Now));
    }

    [Fact]
    public void LessThanOneHourReturnsMinutes()
    {
        Assert.Equal("resets in 42m", TimeFormatHelper.FormatReset(Now.AddMinutes(42), Now));
    }

    [Fact]
    public void MoreThanOneHourReturnsHoursAndMinutes()
    {
        Assert.Equal("resets in 4h 48m", TimeFormatHelper.FormatReset(Now.AddHours(4).AddMinutes(48), Now));
    }

    [Fact]
    public void MoreThanOneDayReturnsDaysAndHours()
    {
        Assert.Equal("resets in 3d 12h", TimeFormatHelper.FormatReset(Now.AddDays(3).AddHours(12), Now));
    }
}
