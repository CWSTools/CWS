using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using AvaloniaFluentUI.Controls;
using AvaloniaFluentUI.Icons;
using AvaloniaFluentUI.Locale;
using AvaloniaFluentUI.Styling;
using AvaloniaFluentUI.Windowing;
using CommunityToolkit.Mvvm.Messaging;
using Gallery.Messages;
using Gallery.Messages.MainWindowMessages;
using Gallery.Models;
using Gallery.Services;
using Gallery.ViewModels;

namespace Gallery.Views;

public class MainWindowSplashScreen : IApplicationSplashScreen
{
    public string AppName => "Avalonia Fluent UI Gallery";
    public IImage AppIcon
    {
        get
        {
            using var stream = AssetLoader.Open(new Uri("avares://Gallery/Assets/app.ico"));
            return new Bitmap(stream);
        }
    }
    public object? SplashScreenContent => null;
    public Task RunTasks(CancellationToken cancellationToken)
    {
        return Task.Delay(600, cancellationToken);
    }

    public int MinimumShowTime => 1500;
}

public partial class MainWindow : FluentWindow
{
    private Bitmap? _backgroundImage;
    private readonly DispatcherTimer _backgroundOnlyTimer;
    
    public MainWindow()
    {
        Application.Current!.Resources["NavigationViewContentMargin"] = new Thickness(0, 55, 0, 0);
        SplashScreen = new MainWindowSplashScreen();
        InitializeComponent();
        _backgroundOnlyTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(5)
        };
        _backgroundOnlyTimer.Tick += OnBackgroundOnlyTimerTick;
        
        RegisterMessages();
        Loaded += OnLoaded;
        
        ToolTip.SetTip(PinButton, LocalizationService.Instance.GetString("Pin"));
        LocalizationService.Instance.PropertyChanged += OnLocalizationChanged;
    }

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (PinButton.Tag?.ToString() == "isTopmost")
        {
            ToolTip.SetTip(PinButton, LocalizationService.Instance.GetString("UnPin"));
        }
        else
        {
            ToolTip.SetTip(PinButton, LocalizationService.Instance.GetString("Pin"));
        }
    }

    private void RegisterMessages()
    {
        WeakReferenceMessenger.Default.Register<JumpToControlMessage>(this, OnJumpToControl);
        WeakReferenceMessenger.Default.Register<EnabledWindowEffectMessage>(this, OnEnabledWindowEffect);
        WeakReferenceMessenger.Default.Register<EnabledBackgroundImageMessage>(this, OnEnabledBackgroundImage);
    }

    private Bitmap LoadImageResource()
    {
        using var stream = AssetLoader.Open(new Uri("avares://Gallery/Assets/Images/bg.jpg"));
        return Bitmap.DecodeToHeight(stream, 1024);
    }

    private void ReleaseBackgroundImage()
    {
        BackgroundImage.Source = null;
        _backgroundImage?.Dispose();
        _backgroundImage = null;
    }

    private void EnsureBackgroundImageLoaded()
    {
        if (DataContext is MainWindowViewModel viewModel && viewModel.SettingsViewModel.IsEnabledBackgroundImage)
        {
            BackgroundImage.IsVisible = true;
            if (_backgroundImage == null)
            {
                _backgroundImage = LoadImageResource();
                BackgroundImage.Source = _backgroundImage;
            }
        }
    }

    private void ReleaseForegroundResources()
    {
        ReleaseBackgroundImage();

        if (DataContext is MainWindowViewModel { CurrentViewModel: HomeViewModel homeViewModel })
        {
            homeViewModel.ReleaseImages();
        }

        GC.Collect(2, GCCollectionMode.Optimized, blocking: false);
    }

    private void RestoreForegroundResources()
    {
        EnsureBackgroundImageLoaded();
    }

    private void OnBackgroundOnlyTimerTick(object? sender, EventArgs e)
    {
        _backgroundOnlyTimer.Stop();
        if (WindowState == WindowState.Minimized)
        {
            Hide();
        }
    }

    private void OnEnabledBackgroundImage(object recipient, EnabledBackgroundImageMessage message)
    {
        if (WindowState == WindowState.Minimized)
        {
            ReleaseBackgroundImage();
            BackgroundImage.IsVisible = false;
            return;
        }

        if (message.IsVisible)
        {
            EnsureBackgroundImageLoaded();
            EnabledAcrylicBlue(false); 
            EnabledMica(false);
        }
        else
        {
            ReleaseBackgroundImage();
        }
        
        BackgroundImage.IsVisible = message.IsVisible;
    }

    private void OnEnabledWindowEffect(object recipient, EnabledWindowEffectMessage message)
    {
        if (message.IsEnabled)
        {
            switch (message.type)
            {
                case "Mica":
                    EnabledMica(true);
                    break;
                case "Acrylic":
                    EnabledAcrylicBlue(true);
                    break;
            }
            return;
        }
        EnabledAcrylicBlue(false);
        EnabledMica(false);
    }

    private NavigationViewItem? FindNavigationItem(IList<object> items, string tag)
    {
        foreach (var item in items)
        {
            if (item is NavigationViewItem nvi)
            {
                if (nvi.Tag?.ToString() == tag)
                    return nvi;

                if (nvi.MenuItems?.Count > 0)
                {
                    var found = FindNavigationItem(nvi.MenuItems, tag);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
        }
        return null;
    }

    private void OnJumpToControl(object recipient, JumpToControlMessage message)
    {
        var nvi = FindNavigationItem(NavigationView.MenuItems, message.Page);
        if (nvi != null)
        {
            NavigationView.SelectedItem = nvi;
            nvi.BringIntoView();
        }
    }

    private void SaveConfig()
    {
        try
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                var svm = viewModel.SettingsViewModel;
                var config = new AppConfig
                {
                    IsCustomAccentColor = svm.IsCustomColor,
                    Theme = AvaloniaFluentTheme.Instance.CurrentTheme.ToString(),
                    IsWindowEffectEnabled = svm.IsEnabledWindowEffect,
                    WindowEffect = svm.CurrentEffect,
                    IsEnabledBackgroundImage = svm.IsEnabledBackgroundImage,
                    Language = svm.CurrentLanguage,
                    ReleaseResourcesOnMinimize = svm.IsReleaseResourcesOnMinimize,
                    HideToTrayAfterMinimizeDelay = svm.IsHideToTrayAfterMinimizeDelay
                };
                if (svm.IsCustomColor)
                {
                    config.CustomAccentColor = svm.SelectedAccentColor.ToString();
                }
                ConfigService.SaveConfig(config);
                
#if DEBUG
                Debug.WriteLine("Save Config Success");
#endif
            }
        }
#if DEBUG 
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
#else
        catch
        {
        }
#endif
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        SaveConfig();
        _backgroundOnlyTimer.Stop();
        _backgroundOnlyTimer.Tick -= OnBackgroundOnlyTimerTick;
        ReleaseBackgroundImage();
        Loaded -= OnLoaded;
        LocalizationService.Instance.PropertyChanged -= OnLocalizationChanged;
        WeakReferenceMessenger.Default.UnregisterAll(this);
        base.OnClosing(e);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            bool visible = viewModel.SettingsViewModel.IsEnabledBackgroundImage;
            BackgroundImage.IsVisible = visible;

            if (visible)
            {
                EnsureBackgroundImageLoaded();
            }
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (Topmost && change.Property == WindowStateProperty)
        {
            Topmost = false;
            Topmost = true;
        }

        if (change.Property == WindowStateProperty)
        {
            if (WindowState == WindowState.Minimized)
            {
                if (DataContext is MainWindowViewModel { SettingsViewModel.IsReleaseResourcesOnMinimize: true })
                {
                    ReleaseForegroundResources();
                }
                _backgroundOnlyTimer.Stop();
                if (DataContext is MainWindowViewModel { SettingsViewModel.IsHideToTrayAfterMinimizeDelay: true })
                {
                    _backgroundOnlyTimer.Start();
                }
            }
            else
            {
                _backgroundOnlyTimer.Stop();
                RestoreForegroundResources();
            }
        }
    }

    private void OnToggleTopmost(object? sender, RoutedEventArgs e)
    {
        if (sender is ToolButton btn)
        {
            if (btn.Tag?.ToString() == "isTopmost")
            {
                btn.Tag = "noTopmost";
                btn.Content= FluentIcon.Pin;
                this.Topmost = false;
                ToolTip.SetTip(btn, LocalizationService.Instance.GetString("Pin"));
            }
            else
            {
                btn.Tag = "isTopmost";
                btn.Content = FluentIcon.Unpin;
                this.Topmost = true;
                ToolTip.SetTip(btn, LocalizationService.Instance.GetString("UnPin"));
            }
        }
    }
    
    private void OnPopupAvatarFlyout(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Avatar ct)
        {
            FlyoutBase.ShowAttachedFlyout(ct);
        }
    }

    private void OnPopupContextMenu(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Panel panel)
        {
            panel.ContextMenu?.Open();
        }
    }
}
