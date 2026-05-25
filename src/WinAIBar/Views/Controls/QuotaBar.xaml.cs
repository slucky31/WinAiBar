using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinAIBar.UI;

namespace WinAIBar.Views.Controls;

public sealed partial class QuotaBar : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(QuotaBar),
            new PropertyMetadata(string.Empty, (d, e) => ((QuotaBar)d).LabelText.Text = (string)e.NewValue));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(QuotaBar),
            new PropertyMetadata(0.0, (d, e) => ((QuotaBar)d).OnValueChanged((double)e.NewValue)));

    public static readonly DependencyProperty ResetTextProperty =
        DependencyProperty.Register(nameof(ResetText), typeof(string), typeof(QuotaBar),
            new PropertyMetadata(string.Empty, (d, _) => ((QuotaBar)d).UpdateRightText()));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string ResetText
    {
        get => (string)GetValue(ResetTextProperty);
        set => SetValue(ResetTextProperty, value);
    }

    public QuotaBar() => InitializeComponent();

    private void OnValueChanged(double value)
    {
        Bar.Value = Math.Clamp(value, 0.0, 1.0);
        var (r, g, b) = UtilizationColors.GetRgb(value);
        Bar.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, r, g, b));
        UpdateRightText();
    }

    private void UpdateRightText()
    {
        var pct = $"{Value:P0}";
        var reset = ResetText;
        RightText.Text = string.IsNullOrEmpty(reset) ? pct : $"{pct} · {reset}";
    }
}
