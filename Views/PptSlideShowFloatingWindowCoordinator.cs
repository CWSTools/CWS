using Gallery.Models;
using Gallery.Services;
using Gallery.ViewModels;

namespace Gallery.Views;

public static class PptSlideShowFloatingWindowCoordinator
{
    private static PptSlideShowFloatingWindow? s_window;

    public static bool IsOpen => s_window?.IsVisible == true;

    public static bool IsAutoSuppressed { get; private set; }

    public static void ShowManual(AppConfig? config = null)
    {
        IsAutoSuppressed = false;
        ShowOrActivate(config);
    }

    public static void ShowAuto(AppConfig? config = null)
    {
        if (IsAutoSuppressed)
        {
            return;
        }

        ShowOrActivate(config);
    }

    public static void ResetAutoSuppression()
    {
        IsAutoSuppressed = false;
    }

    private static void ShowOrActivate(AppConfig? config)
    {
        if (s_window is { IsVisible: true })
        {
            s_window.Activate();
            return;
        }

        s_window = new PptSlideShowFloatingWindow
        {
            DataContext = new PptSlideShowViewModel(config ?? RuntimeConfigService.Current)
        };
        s_window.Closed += (_, _) =>
        {
            s_window = null;
            IsAutoSuppressed = true;
        };
        s_window.Show();
    }
}
