using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AvaloniaFluentUI.Controls;
using AvaloniaFluentUI.Locale;
using Gallery.Models;
using Gallery.Pages;
using Gallery.Services;
using Gallery.ViewModels;
using Gallery.Views;

namespace Gallery;

public class App : Application
{
    private AppConfig? _config;
    private IpcCommandServer? _ipcCommandServer;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeCulture()
    {
        var localeDirs = new[]
        {
            System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "Locale"),
            System.IO.Path.GetFullPath(System.IO.Path.Combine(System.AppContext.BaseDirectory, "..", "Assets", "Locale")),
            System.IO.Path.GetFullPath(System.IO.Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "Assets", "Locale")),
            System.IO.Path.GetFullPath(System.IO.Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", "Assets", "Locale")),
        };

        var localeDir = localeDirs.FirstOrDefault(System.IO.Directory.Exists);
        if (localeDir != null)
        {
            LocalizationService.Instance.LoadResxDirectory(localeDir);
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _config = ConfigService.LoadConfig();
        RuntimeConfigService.Initialize(_config);
        AppLoggerService.Initialize(_config?.IsBehaviorLoggingEnabled ?? false);
        InitializeCulture();
        LocalizationService.Instance.SetCulture(
            string.IsNullOrWhiteSpace(_config?.Language)
                ? LocalizationService.DefaultCultureInfo.Name
                : _config.Language);
        
        try
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var args = desktop.Args ?? [];
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                if (TryHandleFileLaunch(args))
                {
                    desktop.Shutdown();
                    base.OnFrameworkInitializationCompleted();
                    return;
                }

                StartIpcServer(desktop);

                if (args.Contains("--settings", StringComparer.OrdinalIgnoreCase))
                {
                    ShowSettings(desktop);
                }
                else if (args.Contains("--show", StringComparer.OrdinalIgnoreCase))
                {
                    ShowMainWindow(desktop);
                }
                else if (!args.Contains("--background", StringComparer.OrdinalIgnoreCase))
                {
                    EnsureMainWindow(desktop);
                }
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            {
                singleView.MainView = new MainView
                {
                    DataContext = new MainWindowViewModel(_config)
                };
            }
            else
            {
                Console.Error.WriteLine($"Unhandled ApplicationLifetime type: {ApplicationLifetime?.GetType()}");
                AppLoggerService.Info("app", $"Unhandled ApplicationLifetime type: {ApplicationLifetime?.GetType()}");
            }

            Frame.RegisterPage<FramePage1>();
            Frame.RegisterPage<FramePage2>();
            Frame.RegisterPage<FramePage3>();
            Frame.RegisterPage<FramePage4>();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FATAL: App initialization failed: {ex}");
            AppLoggerService.Error("app", ex, "App initialization failed.");
        }
        
        base.OnFrameworkInitializationCompleted();
    }

    private bool TryHandleFileLaunch(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        var fileArgs = args
            .Where(arg => !arg.StartsWith("--", StringComparison.OrdinalIgnoreCase))
            .Where(System.IO.File.Exists)
            .ToArray();

        if (fileArgs.Length == 0)
        {
            return false;
        }

        var router = new FileOpenRouterService();
        foreach (var filePath in fileArgs)
        {
            var result = router.OpenFile(filePath, RuntimeConfigService.Current.OpenMethodPreferences);
            AppLoggerService.Info("open-method", $"Command line open handled. File={filePath}, Status={result.Status}");
        }

        return true;
    }

    private void StartIpcServer(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_ipcCommandServer is not null || !OperatingSystem.IsWindows())
        {
            return;
        }

        _ipcCommandServer = new IpcCommandServer(async command =>
            await Dispatcher.UIThread.InvokeAsync(() => HandleIpcCommand(desktop, command)));
        _ipcCommandServer.Start();
        AppLoggerService.Info("ipc", "IPC command server started.");
    }

    private void HandleIpcCommand(IClassicDesktopStyleApplicationLifetime desktop, string command)
    {
        switch (command.Trim().ToLowerInvariant())
        {
            case "settings":
                ShowSettings(desktop);
                break;
            case "show":
                ShowMainWindow(desktop);
                break;
        }
    }

    private MainWindow EnsureMainWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (desktop.MainWindow is MainWindow window)
        {
            return window;
        }

        window = new MainWindow
        {
            DataContext = new MainWindowViewModel(_config)
        };

        desktop.MainWindow = window;
        return window;
    }

    private void ShowMainWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var window = EnsureMainWindow(desktop);
        window.RestoreFromTray();
    }

    private void ShowSettings(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var window = EnsureMainWindow(desktop);
        window.RestoreFromTray();

        if (window.DataContext is MainWindowViewModel viewModel)
        {
            viewModel.TogglePageCommand.Execute("Settings");
        }
    }

    private void OnClicked(object? sender, EventArgs e)
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow is MainWindow window)
            {
                _ipcCommandServer?.Dispose();
                _ipcCommandServer = null;
                window.RequestApplicationClose();
            }

            desktop.Shutdown();
        }
    }

    private void OnShowClicked(object? sender, EventArgs e)
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ShowMainWindow(desktop);
        }
    }

    private void OnSettingsClicked(object? sender, EventArgs e)
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ShowSettings(desktop);
        }
    }
}
