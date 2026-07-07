namespace Gallery.Models;

public sealed class ToolboxSummaryItemModel
{
    public ToolboxSummaryItemModel(string label, string value, string caption)
    {
        Label = label;
        Value = value;
        Caption = caption;
    }

    public string Label { get; }

    public string Value { get; }

    public string Caption { get; }
}
