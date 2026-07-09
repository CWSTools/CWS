using Gallery.Models;
using Gallery.Services;

namespace Gallery.Views;

public static class MiniWhiteboardWindowCoordinator
{
    private static MiniWhiteboardWindow? s_window;

    public static void Show(AppConfig? config = null)
    {
        if (s_window is { IsVisible: true })
        {
            s_window.Activate();
            return;
        }

        s_window = new MiniWhiteboardWindow(config ?? RuntimeConfigService.Current);
        s_window.Closed += (_, _) => s_window = null;
        s_window.Show();
    }
}
