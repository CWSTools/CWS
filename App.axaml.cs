using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaloniaFluentUI.Controls;
using AvaloniaFluentUI.Locale;
using Gallery.Pages;
using Gallery.Services;
using Gallery.ViewModels;
using Gallery.Views;

namespace Gallery;

public class App : Application
{
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
        var config = ConfigService.LoadConfig();
        InitializeCulture();
        LocalizationService.Instance.SetCulture(
            string.IsNullOrWhiteSpace(config?.Language)
                ? LocalizationService.DefaultCultureInfo.Name
                : config.Language);
        
        try
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(config)
                };
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            {
                singleView.MainView = new MainView
                {
                    DataContext = new MainWindowViewModel(config)
                };
            }
            else
            {
                Console.Error.WriteLine($"Unhandled ApplicationLifetime type: {ApplicationLifetime?.GetType()}");
            }

            Frame.RegisterPage<FramePage1>();
            Frame.RegisterPage<FramePage2>();
            Frame.RegisterPage<FramePage3>();
            Frame.RegisterPage<FramePage4>();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FATAL: App initialization failed: {ex}");
        }
        
        base.OnFrameworkInitializationCompleted();
    }

    private void OnClicked(object? sender, EventArgs e)
    {
        // Environment.Exit(0);
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow?.Close();
        }
    }

    private void OnShowClicked(object? sender, EventArgs e)
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is { } window)
        {
            window.Show();
            window.WindowState = WindowState.Normal;
            window.Activate();
        }
    }

    private void OnSettingsClicked(object? sender, EventArgs e)
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is { } window)
        {
            window.Show();
            window.WindowState = WindowState.Normal;
            window.Activate();

            if (window.DataContext is MainWindowViewModel viewModel)
            {
                viewModel.TogglePageCommand.Execute("Settings");
            }
        }
    }
}
