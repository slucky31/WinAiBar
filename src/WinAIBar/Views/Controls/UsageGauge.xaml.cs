using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinAIBar.Core.Infrastructure;

namespace WinAIBar.Views.Controls;

public sealed partial class UsageGauge : UserControl
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(UsageGauge),
            new PropertyMetadata(0.0, (d, e) => ((UsageGauge)d).OnValueChanged((double)e.NewValue)));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(UsageGauge),
            new PropertyMetadata(string.Empty, (d, e) => ((UsageGauge)d).LabelText.Text = (string)e.NewValue));

    public static readonly DependencyProperty ResetsAtProperty =
        DependencyProperty.Register(nameof(ResetsAt), typeof(DateTimeOffset), typeof(UsageGauge),
            new PropertyMetadata(default(DateTimeOffset), (d, _) => ((UsageGauge)d).UpdateResetText()));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(UsageGauge),
            new PropertyMetadata(null, (d, e) => ((UsageGauge)d).OnSubtitleChanged((string?)e.NewValue)));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public DateTimeOffset? ResetsAt
    {
        get
        {
            var dt = (DateTimeOffset)GetValue(ResetsAtProperty);
            return dt == default(DateTimeOffset) ? null : dt;
        }
        set => SetValue(ResetsAtProperty, value ?? default(DateTimeOffset));
    }

    public string? Subtitle
    {
        get => (string?)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public UsageGauge() => InitializeComponent();

    private void OnValueChanged(double value)
    {
        Ring.Value = Math.Clamp(value, 0.0, 1.0);
        PercentText.Text = $"{value:P0}";
        UpdateResetText();
    }

    private void OnSubtitleChanged(string? value)
    {
        SubtitleText.Text = value ?? string.Empty;
        SubtitleText.Visibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateResetText()
    {
        var text = TimeFormatHelper.FormatReset(ResetsAt);
        ResetTextBlock.Text = text;
        ResetTextBlock.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
    }
}
