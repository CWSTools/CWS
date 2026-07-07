using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaFluentUI.Locale;
using AvaloniaFluentUI.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Gallery.Messages.MainWindowMessages;
using Gallery.Models;
using Gallery.Services;

namespace Gallery.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    public override string Title => LocalizationService.Instance.GetString("Settings");
    
    public SettingsViewModel(AppConfig? config)
    {
        AvaloniaFluentTheme.Instance.ThemeChanged += OnThemeChanged;
        CurrentLanguage = LocalizationService.Instance.CurrentLanguage;
        
        LoadSetting(config);
    }

    protected override void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
    {
        base.OnLanguageChanged(sender, e);
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(WindowEffect));
        OnPropertyChanged(nameof(WindowEffectDescription));
        OnPropertyChanged(nameof(WindowEffectNone));
        OnPropertyChanged(nameof(WindowEffectMica));
        OnPropertyChanged(nameof(WindowEffectAcrylic));
        OnPropertyChanged(nameof(AppearanceDescription));
        OnPropertyChanged(nameof(AppThemeDescription));
        OnPropertyChanged(nameof(ThemeColor));
        OnPropertyChanged(nameof(ThemeColorDescription));
        OnPropertyChanged(nameof(CustomColor));
        OnPropertyChanged(nameof(DefaultColor));
        OnPropertyChanged(nameof(SelectColor));
        OnPropertyChanged(nameof(PinToTop));
        OnPropertyChanged(nameof(PinToTopDescription));
        OnPropertyChanged(nameof(Language));
        OnPropertyChanged(nameof(LanguageDescription));
        OnPropertyChanged(nameof(BackgroundImage));
        OnPropertyChanged(nameof(EnableBackgroundImage));
        OnPropertyChanged(nameof(BackgroundBehavior));
        OnPropertyChanged(nameof(ReleaseResourcesOnMinimize));
        OnPropertyChanged(nameof(ReleaseResourcesOnMinimizeDescription));
        OnPropertyChanged(nameof(HideToTrayAfterMinimizeDelay));
        OnPropertyChanged(nameof(HideToTrayAfterMinimizeDelayDescription));
        OnPropertyChanged(nameof(Light));
        OnPropertyChanged(nameof(Dark));
        OnPropertyChanged(nameof(FollowSystem));
    }

    private void LoadSetting (AppConfig? config)
    {
        if (config != null)
        {
            CurrentLanguage =  config.Language;
            ToggleTheme(config.Theme);
            
            if (config.IsCustomAccentColor)
            {
                IsCustomColor = true;
                IsDefaultAccentColor = false;
                SelectedAccentColor = Color.Parse(config.CustomAccentColor);
            }
            
            string effect = config.WindowEffect;
            if (effect == "Mica" && IsWindows11)
            {
                EnabledWindowEffect(effect);
            }
            else if  (effect == "Acrylic")
            {
                EnabledWindowEffect(effect);
            }
            else
            {
                CurrentEffect = "Null";
                IsEnabledWindowEffect = false;
            }
            
            
            IsEnabledBackgroundImage = config.IsEnabledBackgroundImage;
            IsReleaseResourcesOnMinimize = config.ReleaseResourcesOnMinimize;
            IsHideToTrayAfterMinimizeDelay = config.HideToTrayAfterMinimizeDelay;
            
            Console.WriteLine("Loaded Settings");
        }
    }

    private void OnThemeChanged(object? sender, ThemeVariant? variant)
    {
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(IsAutoTheme));
        
        WeakReferenceMessenger.Default.Send(new EnabledWindowEffectMessage(IsEnabledWindowEffect, CurrentEffect)); 
    }
    
    [ObservableProperty]
    private bool _isDefaultAccentColor = true;

    partial void OnIsDefaultAccentColorChanged(bool value)
    {
        if (value)
        {
            AvaloniaFluentTheme.Instance.CustomAccentColor = null;
        }
    }

    [ObservableProperty]
    private Color _selectedAccentColor = Colors.Transparent;

    partial void OnSelectedAccentColorChanged(Color value)
    {
        AvaloniaFluentTheme.Instance.CustomAccentColor = value;
    }

    [RelayCommand]
    private void ToggleTheme(string value)
    {
        AvaloniaFluentTheme.Instance.CurrentTheme = value switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    // public bool IsLightTheme => Application.Current?.RequestedThemeVariant == ThemeVariant.Light;
    public bool IsDarkTheme => ConfigService.IsDarkTheme();
    public bool IsAutoTheme => Application.Current?.RequestedThemeVariant == ThemeVariant.Default;
    
    [ObservableProperty]
    private bool _isEnabledWindowEffect;

    public bool WindowEffectCardIsEnabled => !IsEnabledBackgroundImage && RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public string[] Languages => ["en-US", "zh-CN", "ja-JP"];

    // Localized string properties
    public string WindowEffect => LocalizationService.Instance.GetString("SV_WindowEffect");
    public string WindowEffectDescription => LocalizationService.Instance.GetString("SV_WindowEffectDescription");
    public string WindowEffectNone => LocalizationService.Instance.GetString("SV_WindowEffectNone");
    public string WindowEffectMica => LocalizationService.Instance.GetString("SV_WindowEffectMica");
    public string WindowEffectAcrylic => LocalizationService.Instance.GetString("SV_WindowEffectAcrylic");
    public string AppearanceDescription => LocalizationService.Instance.GetString("SV_Appearance");
    public string AppThemeDescription => LocalizationService.Instance.GetString("SV_AppTheme");
    public string ThemeColor => LocalizationService.Instance.GetString("SV_ThemeColor");
    public string ThemeColorDescription => LocalizationService.Instance.GetString("SV_ThemeColorDescription");
    public string CustomColor => LocalizationService.Instance.GetString("SV_CustomColor");
    public string DefaultColor => LocalizationService.Instance.GetString("SV_DefaultColor");
    public string SelectColor => LocalizationService.Instance.GetString("SV_SelectColor");
    public string PinToTop => LocalizationService.Instance.GetString("SV_PinToTop");
    public string PinToTopDescription => LocalizationService.Instance.GetString("SV_PinToTopDescription");
    public string Language => LocalizationService.Instance.GetString("SV_Language");
    public string LanguageDescription => LocalizationService.Instance.GetString("SV_LanguageDescription");
    public string BackgroundImage => LocalizationService.Instance.GetString("SV_BackgroundImage");
    public string EnableBackgroundImage => LocalizationService.Instance.GetString("SV_EnableBackgroundImage");
    public string BackgroundBehavior => LocalizationService.Instance.GetString("SV_BackgroundBehavior");
    public string ReleaseResourcesOnMinimize => LocalizationService.Instance.GetString("SV_ReleaseResourcesOnMinimize");
    public string ReleaseResourcesOnMinimizeDescription => LocalizationService.Instance.GetString("SV_ReleaseResourcesOnMinimizeDescription");
    public string HideToTrayAfterMinimizeDelay => LocalizationService.Instance.GetString("SV_HideToTrayAfterMinimizeDelay");
    public string HideToTrayAfterMinimizeDelayDescription => LocalizationService.Instance.GetString("SV_HideToTrayAfterMinimizeDelayDescription");
    public string Light => LocalizationService.Instance.GetString("LV_Light");
    public string Dark => LocalizationService.Instance.GetString("LV_Dark");
    public string FollowSystem => LocalizationService.Instance.GetString("LV_FollowSystem");

    public bool IsWindows11 => IsWindows && Environment.OSVersion.Version.Build >= 22000;
    public bool IsWindows => OperatingSystem.IsWindows();

    public string CurrentEffect { get; set; } = "Null";

    public bool IsMicaRadioChecked => CurrentEffect == "Mica";
    public bool IsAcrylicRadioChecked => CurrentEffect == "Acrylic";

    [RelayCommand]
    private void EnabledWindowEffect(object value)
    {
        if (value is string effect && CurrentEffect != effect)
        {
            CurrentEffect = effect;
            IsEnabledWindowEffect = !effect.Equals("Null");
            OnPropertyChanged(nameof(IsMicaRadioChecked));
            OnPropertyChanged(nameof(IsAcrylicRadioChecked));
            WeakReferenceMessenger.Default.Send(new EnabledWindowEffectMessage(IsEnabledWindowEffect, effect));

            Console.WriteLine("Effect: " + effect + $"IsEnabledWindowEffect: {IsEnabledWindowEffect}");
        }
    }

    [ObservableProperty]
    private string _currentLanguage = LocalizationService.Instance.CurrentLanguage;

    partial void OnCurrentLanguageChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == LocalizationService.Instance.CurrentLanguage)
        {
            return;
        }
        LocalizationService.Instance.SetCulture(value);
    }

    [ObservableProperty]
    private bool _isCustomColor;

    partial void OnIsCustomColorChanged(bool value)
    {
        if (value)
        {
            AvaloniaFluentTheme.Instance.CustomAccentColor = SelectedAccentColor;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowEffectCardIsEnabled))]
    private bool _isEnabledBackgroundImage;

    [ObservableProperty]
    private bool _isReleaseResourcesOnMinimize = true;

    [ObservableProperty]
    private bool _isHideToTrayAfterMinimizeDelay = true;

    partial void OnIsEnabledBackgroundImageChanged(bool value)
    {
        if (value)
        {
            IsEnabledWindowEffect = false;
            CurrentEffect = "Null";
            OnPropertyChanged(nameof(IsMicaRadioChecked));
            OnPropertyChanged(nameof(IsAcrylicRadioChecked));
        }
        WeakReferenceMessenger.Default.Send(new EnabledBackgroundImageMessage(value));
    }

    public override void Dispose()
    {
        AvaloniaFluentTheme.Instance.ThemeChanged -= OnThemeChanged;
        base.Dispose();
    }
}
