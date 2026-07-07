using Avalonia;
using Avalonia.Controls;

namespace Gallery.Controls;

public class ToolboxStatCard : ContentControl
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<ToolboxStatCard, string?>(nameof(Label));

    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<ToolboxStatCard, string?>(nameof(Value));

    public static readonly StyledProperty<string?> CaptionProperty =
        AvaloniaProperty.Register<ToolboxStatCard, string?>(nameof(Caption));

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string? Caption
    {
        get => GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }
}
