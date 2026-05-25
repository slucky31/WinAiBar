using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;
using WinAIBar.Core.Infrastructure;

namespace WinAIBar.Views.Controls;

public sealed partial class UsageGauge : UserControl
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(UsageGauge),
            new PropertyMetadata(0.0, (d, e) => ((UsageGauge)d).OnValueChanged((double)e.NewValue)));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(UsageGauge),
            new PropertyMetadata(string.Empty, (d, e) => ((UsageGauge)d).OnLabelChanged((string)e.NewValue)));

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

    private readonly PieSeries<double> _valueSeries;
    private readonly PieSeries<double> _remainderSeries;

    public UsageGauge()
    {
        InitializeComponent();

        _valueSeries = new PieSeries<double>
        {
            Values = [0.0],
            InnerRadius = 60,
            Fill = new SolidColorPaint(GetColorForValue(0.0)),
        };

        _remainderSeries = new PieSeries<double>
        {
            Values = [1.0],
            InnerRadius = 60,
            Fill = new SolidColorPaint(new SKColor(211, 211, 211, 77)),
        };

        GaugeChart.Series = new ISeries[] { _valueSeries, _remainderSeries };
        GaugeChart.TooltipPosition = LiveChartsCore.Measure.TooltipPosition.Hidden;
        GaugeChart.LegendPosition = LiveChartsCore.Measure.LegendPosition.Hidden;
    }

    private void OnValueChanged(double value)
    {
        PercentText.Text = $"{value:P0}";
        UpdateSeries(value);
        UpdateAutomationName();
    }

    private void OnLabelChanged(string value)
    {
        LabelText.Text = value;
        UpdateAutomationName();
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
        UpdateAutomationName();
    }

    private void UpdateSeries(double value)
    {
        var clamped = Math.Clamp(value, 0.0, 1.0);
        _valueSeries.Values = [clamped];
        _valueSeries.Fill = new SolidColorPaint(GetColorForValue(value));
        _remainderSeries.Values = [Math.Max(0.0, 1.0 - clamped)];
    }

    private static SKColor GetColorForValue(double value) => value switch
    {
        < 0.50 => new SKColor(0x00, 0x78, 0xD4),
        < 0.75 => new SKColor(0x10, 0x7C, 0x10),
        < 0.90 => new SKColor(0xFF, 0xB9, 0x00),
        < 1.00 => new SKColor(0xD1, 0x34, 0x38),
        _ => new SKColor(0xA4, 0x26, 0x2C),
    };

    private void UpdateAutomationName()
    {
        var resetText = TimeFormatHelper.FormatReset(ResetsAt);
        var automationName = string.IsNullOrEmpty(resetText)
            ? $"{Label}: {Value:P0}"
            : $"{Label}: {Value:P0}, resets in {resetText}";
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(this, automationName);
    }
}
