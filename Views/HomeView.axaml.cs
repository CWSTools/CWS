using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using AvaloniaFluentUI.Locale;
using Gallery.Services;

namespace Gallery.Views;

public partial class HomeView : UserControl
{

    public HomeView()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        var window = TopLevel.GetTopLevel(this) as Window;
        if (window != null)
        {
            window.PropertyChanged += (_, e) =>
            {
                if (e.Property == Window.WindowStateProperty)
                {
                    bool isMax = window.WindowState == WindowState.Maximized || window.WindowState == WindowState.FullScreen;
                    Thickness value = isMax ? new Thickness(0, 0, 10, 20) : new Thickness(0, 0, 0, 20);
                    SmoothScrollViewer.Margin = value;
                }
            };
        }
    }

    private static void NavigateTo(string page, string cardTitle)
    {
        JumpService.InvokeJumpEvent(new JumpModel
        {
            Page = page,
            ControlName = cardTitle
        });
    }

    private void OnOpenAssociationsClicked(object? sender, RoutedEventArgs e)
    {
        NavigateTo("OpenMethod", LocalizationService.Instance.GetString("HV_OpenAssociationTitle"));
    }

    private void OnOpenCommentsClicked(object? sender, RoutedEventArgs e)
    {
        NavigateTo("BasicInput", LocalizationService.Instance.GetString("HV_CommentsTitle"));
    }

    private void OnOpenBatchToolsClicked(object? sender, RoutedEventArgs e)
    {
        NavigateTo("DialogBoxAndPopup", LocalizationService.Instance.GetString("HV_BatchToolsTitle"));
    }
}
