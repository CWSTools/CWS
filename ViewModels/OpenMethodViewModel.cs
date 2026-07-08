using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaFluentUI.Locale;
using AvaloniaFluentUI.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Gallery.Messages.MainWindowMessages;
using Gallery.Models;
using Gallery.Services;

namespace Gallery.ViewModels;

public partial class OpenMethodViewModel : ViewModelBase
{
    private readonly OpenMethodService _openMethodService = new();
    private readonly FileOpenRouterService _fileOpenRouterService = new();
    private readonly FileAssociationRegistrationService _associationRegistrationService = new();

    public override string Title => LocalizationService.Instance.GetString("OM_Title");

    public ObservableCollection<OpenMethodEntryModel> Entries { get; } = [];

    public OpenMethodViewModel(AppConfig? config)
    {
        IsBackgroundImageEnabled = config?.IsEnabledBackgroundImage ?? false;
        AvaloniaFluentTheme.Instance.ThemeChanged += OnThemeChanged;
        WeakReferenceMessenger.Default.Register<EnabledBackgroundImageMessage>(
            this,
            (_, message) => IsBackgroundImageEnabled = message.IsVisible);

        var preferences = config?.OpenMethodPreferences ?? new Dictionary<string, string>();

        foreach (var definition in _openMethodService.GetDefinitions())
        {
            Entries.Add(CreateEntry(definition, preferences));
        }

        RefreshDetectedCurrentTargets();
        RefreshSystemEntryStatus();
    }

    [ObservableProperty]
    private bool _isBackgroundImageEnabled;

    partial void OnIsBackgroundImageEnabledChanged(bool value) => RefreshChrome();

    private bool IsDarkTheme => AvaloniaFluentTheme.Instance.IsDarkTheme;

    public IBrush PageOverlayBrush => IsBackgroundImageEnabled
        ? Brush.Parse(IsDarkTheme ? "#760B0D12" : "#22F7F9FB")
        : Brush.Parse(IsDarkTheme ? "#00000000" : "#FFF7F9FC");

    public IBrush GlassPanelBackground => Brush.Parse(IsBackgroundImageEnabled
        ? IsDarkTheme ? "#781A1D24" : "#62FFFFFF"
        : IsDarkTheme ? "#B8262A32" : "#F2FFFFFF");

    public IBrush GlassPanelBorderBrush => Brush.Parse(IsBackgroundImageEnabled
        ? IsDarkTheme ? "#38FFFFFF" : "#3A142236"
        : IsDarkTheme ? "#34FFFFFF" : "#24000000");

    public IBrush MethodCardBackground => Brush.Parse(IsBackgroundImageEnabled
        ? IsDarkTheme ? "#64232931" : "#56FFFFFF"
        : IsDarkTheme ? "#8A2A2F38" : "#FAFFFFFF");

    public IBrush MethodCardBorderBrush => Brush.Parse(IsBackgroundImageEnabled
        ? IsDarkTheme ? "#2EFFFFFF" : "#34142236"
        : IsDarkTheme ? "#2AFFFFFF" : "#1E000000");

    public IBrush InnerPanelBackground => Brush.Parse(IsBackgroundImageEnabled
        ? IsDarkTheme ? "#402F3540" : "#4AFFFFFF"
        : IsDarkTheme ? "#59313740" : "#F7FFFFFF");

    public IBrush InnerPanelBorderBrush => Brush.Parse(IsBackgroundImageEnabled
        ? IsDarkTheme ? "#24FFFFFF" : "#2E142236"
        : IsDarkTheme ? "#24FFFFFF" : "#18000000");

    private void OnThemeChanged(object? sender, ThemeVariant? variant) => RefreshChrome();

    private void RefreshChrome()
    {
        OnPropertyChanged(nameof(PageOverlayBrush));
        OnPropertyChanged(nameof(GlassPanelBackground));
        OnPropertyChanged(nameof(GlassPanelBorderBrush));
        OnPropertyChanged(nameof(MethodCardBackground));
        OnPropertyChanged(nameof(MethodCardBorderBrush));
        OnPropertyChanged(nameof(InnerPanelBackground));
        OnPropertyChanged(nameof(InnerPanelBorderBrush));
    }

    protected override void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
    {
        base.OnLanguageChanged(sender, e);
        RefreshLocalizedEntryText();
        OnPropertyChanged(nameof(PageDescription));
        OnPropertyChanged(nameof(SectionTitle));
        OnPropertyChanged(nameof(SectionDescription));
        OnPropertyChanged(nameof(ComboLabel));
        OnPropertyChanged(nameof(CurrentTargetLabel));
        OnPropertyChanged(nameof(ConfirmDialogTitle));
        OnPropertyChanged(nameof(ConfirmDialogDescription));
        OnPropertyChanged(nameof(ConfirmApplyButton));
        OnPropertyChanged(nameof(ConfirmCancelButton));
        OnPropertyChanged(nameof(RefreshCurrentButton));
        OnPropertyChanged(nameof(TestOpenButton));
        OnPropertyChanged(nameof(RegisterSystemEntryButton));
        OnPropertyChanged(nameof(SystemEntryStatusLabel));
        RefreshDetectedCurrentTargets();
        RefreshSystemEntryStatus();
    }

    public string PageDescription => LocalizationService.Instance.GetString("OM_PageDescription");

    public string SectionTitle => LocalizationService.Instance.GetString("OM_SectionTitle");

    public string SectionDescription => LocalizationService.Instance.GetString("OM_SectionDescription");

    public string ComboLabel => LocalizationService.Instance.GetString("OM_ComboLabel");

    public string CurrentTargetLabel => LocalizationService.Instance.GetString("OM_CurrentTargetLabel");

    public string ConfirmDialogTitle => LocalizationService.Instance.GetString("OM_ConfirmDialogTitle");

    public string ConfirmDialogDescription => LocalizationService.Instance.GetString("OM_ConfirmDialogDescription");

    public string ConfirmApplyButton => LocalizationService.Instance.GetString("OM_ConfirmApplyButton");

    public string ConfirmCancelButton => LocalizationService.Instance.GetString("OM_ConfirmCancelButton");

    public string RefreshCurrentButton => LocalizationService.Instance.GetString("OM_RefreshCurrentButton");

    public string TestOpenButton => LocalizationService.Instance.GetString("OM_TestOpenButton");

    public string RegisterSystemEntryButton => LocalizationService.Instance.GetString("OM_RegisterSystemEntryButton");

    public string SystemEntryStatusLabel => LocalizationService.Instance.GetString("OM_SystemEntryStatusLabel");

    [ObservableProperty]
    private string _systemEntryStatusMessage = string.Empty;

    public List<OpenMethodTargetOption> TargetOptions => BuildTargetOptions();

    public void ApplySelection(OpenMethodEntryModel entry)
    {
        if (!OperatingSystem.IsWindows())
        {
            entry.SetStatus(LocalizationService.Instance.GetString("OM_ResultUnsupportedPlatform"));
            AppLoggerService.Info("open-method", $"ApplySelection skipped on unsupported platform. Entry={entry.Key}");
            return;
        }

        entry.ConfirmSelection();
        var preferences = ExportPreferences();
        RuntimeConfigService.Update(config => config.OpenMethodPreferences = preferences);

        var result = _openMethodService.SaveSelection(entry.Key, entry.GetAppliedValue());
        var iconRefreshResult = _associationRegistrationService.RefreshFileIcons(preferences);
        if (iconRefreshResult.Status != FileAssociationRegistrationStatus.Registered)
        {
            AppLoggerService.Info(
                "open-method",
                $"File icon refresh skipped or failed. Entry={entry.Key}, Status={iconRefreshResult.Status}, Error={iconRefreshResult.ErrorMessage}");
        }

        entry.SetStatus(BuildResultMessage(result));
        AppLoggerService.Info("open-method", $"ApplySelection executed. Entry={entry.Key}, Target={entry.GetAppliedValue()}, Status={result.Status}");
    }

    public void RefreshDetectedCurrentTargets()
    {
        if (!OperatingSystem.IsWindows())
        {
            foreach (var entry in Entries)
            {
                entry.SetCurrentTargetLabel(LocalizationService.Instance.GetString("OM_ResultUnsupportedPlatform"));
            }
            AppLoggerService.Info("open-method", "RefreshDetectedCurrentTargets skipped on unsupported platform.");
            return;
        }

        var detected = _openMethodService.DetectCurrentTargets();
        foreach (var entry in Entries)
        {
            var target = detected.TryGetValue(entry.Key, out var value) ? value : OpenMethodTargets.Unknown;
            entry.SetCurrentTargetLabel(GetTargetLabel(target));
        }
        AppLoggerService.Info("open-method", "Refreshed current detected file association targets.");
    }

    public IReadOnlyList<OpenMethodAssociationPlanItem> BuildAssociationPlan()
    {
        return _openMethodService.BuildAssociationPlan(ExportPreferences());
    }

    public string[] GetExtensionsForEntry(string entryKey)
    {
        return _openMethodService.GetExtensions(entryKey);
    }

    public void OpenFile(OpenMethodEntryModel entry, string filePath)
    {
        var result = _fileOpenRouterService.OpenFile(filePath, ExportPreferences());
        entry.SetStatus(BuildOpenFileMessage(result));
        AppLoggerService.Info(
            "open-method",
            $"OpenFile executed. Entry={entry.Key}, File={filePath}, Target={entry.GetAppliedValue()}, Status={result.Status}");
    }

    public void RegisterSystemEntry()
    {
        if (!OperatingSystem.IsWindows())
        {
            SystemEntryStatusMessage = LocalizationService.Instance.GetString("OM_ResultUnsupportedPlatform");
            return;
        }

        var registerResult = _associationRegistrationService.RegisterForCurrentUser(ExportPreferences());
        if (registerResult.Status != FileAssociationRegistrationStatus.Registered)
        {
            SystemEntryStatusMessage = BuildRegistrationMessage(registerResult);
            return;
        }

        var settingsResult = _associationRegistrationService.OpenDefaultAppsSettings();
        SystemEntryStatusMessage = settingsResult.Status == FileAssociationRegistrationStatus.OpenedSettings
            ? LocalizationService.Instance.GetString("OM_ResultRegisteredAndOpenedSettings")
            : BuildRegistrationMessage(settingsResult);

        RefreshSystemEntryStatus();
    }

    public void RefreshSystemEntryStatus()
    {
        if (!OperatingSystem.IsWindows())
        {
            SystemEntryStatusMessage = LocalizationService.Instance.GetString("OM_ResultUnsupportedPlatform");
            return;
        }

        var state = _associationRegistrationService.GetState();
        SystemEntryStatusMessage = state.IsRegistered
            ? string.Format(
                LocalizationService.Instance.GetString("OM_SystemEntryStatusRegistered"),
                state.AssociatedExtensionCount,
                state.SupportedExtensionCount)
            : LocalizationService.Instance.GetString("OM_SystemEntryStatusNotRegistered");
    }

    private OpenMethodEntryModel CreateEntry(
        OpenMethodDefinition definition,
        IReadOnlyDictionary<string, string> preferences)
    {
        var selected = preferences.TryGetValue(definition.Key, out var target)
            ? NormalizeTarget(target)
            : OpenMethodTargets.System;

        return new OpenMethodEntryModel(
            definition.Key,
            LocalizationService.Instance.GetString(definition.TitleResourceKey),
            definition.Extensions,
            LocalizationService.Instance.GetString(definition.DescriptionResourceKey),
            selected,
            BuildTargetOptions(),
            RefreshStatus);
    }

    private void RefreshStatus()
    {
        OnPropertyChanged(nameof(TargetOptions));
    }

    private void RefreshLocalizedEntryText()
    {
        var targetOptions = BuildTargetOptions();
        var definitions = _openMethodService.GetDefinitions().ToDictionary(item => item.Key);
        foreach (var entry in Entries)
        {
            if (!definitions.TryGetValue(entry.Key, out var definition))
            {
                continue;
            }

            var title = LocalizationService.Instance.GetString(definition.TitleResourceKey);
            var description = LocalizationService.Instance.GetString(definition.DescriptionResourceKey);

            entry.RefreshLocalizedText(title, description, targetOptions);
        }
    }

    public Dictionary<string, string> ExportPreferences()
    {
        return Entries.ToDictionary(entry => entry.Key, entry => NormalizeTarget(entry.GetAppliedValue()));
    }

    public string BuildConfirmMessage(OpenMethodEntryModel entry)
    {
        return string.Format(
            LocalizationService.Instance.GetString("OM_ConfirmChangeMessage"),
            entry.Title,
            entry.CurrentTargetLabel,
            entry.SelectedOption.Label);
    }

    private string BuildResultMessage(OpenMethodSaveResult result)
    {
        return result.Status switch
        {
            OpenMethodSaveStatus.Saved when !string.IsNullOrWhiteSpace(result.TargetAppDisplayName)
                => string.Format(
                    LocalizationService.Instance.GetString("OM_ResultSavedWithTarget"),
                    result.TargetAppDisplayName,
                    string.Join(", ", result.Extensions ?? [])),
            OpenMethodSaveStatus.Saved
                => LocalizationService.Instance.GetString("OM_ResultSaved"),
            OpenMethodSaveStatus.SavedToSystem
                => LocalizationService.Instance.GetString("OM_ResultSavedToSystem"),
            OpenMethodSaveStatus.SavedWithoutDetectedApp
                => string.Format(
                    LocalizationService.Instance.GetString("OM_ResultSavedWithoutDetectedApp"),
                    result.TargetAppDisplayName ?? LocalizationService.Instance.GetString("OM_SelectedApp")),
            _ => LocalizationService.Instance.GetString("OM_ResultUnsupportedPlatform")
        };
    }

    private string BuildOpenFileMessage(FileOpenResult result)
    {
        return result.Status switch
        {
            FileOpenStatus.OpenedWithTarget when !string.IsNullOrWhiteSpace(result.TargetAppDisplayName)
                => string.Format(
                    LocalizationService.Instance.GetString("OM_ResultOpenedWithTarget"),
                    result.TargetAppDisplayName,
                    result.Extension ?? string.Empty),
            FileOpenStatus.OpenedWithSystem
                => string.Format(
                    LocalizationService.Instance.GetString("OM_ResultOpenedWithSystem"),
                    result.Extension ?? string.Empty),
            FileOpenStatus.TargetAppNotFound
                => string.Format(
                    LocalizationService.Instance.GetString("OM_ResultTargetNotFound"),
                    result.Target ?? LocalizationService.Instance.GetString("OM_SelectedApp")),
            FileOpenStatus.FileNotFound
                => LocalizationService.Instance.GetString("OM_ResultFileNotFound"),
            FileOpenStatus.LaunchFailed
                => string.Format(
                    LocalizationService.Instance.GetString("OM_ResultLaunchFailed"),
                    result.ErrorMessage ?? LocalizationService.Instance.GetString("COM_Error")),
            FileOpenStatus.SystemDefaultRoutesToCwsTool
                => LocalizationService.Instance.GetString("OM_ResultSystemDefaultRoutesToCwsTool"),
            _ => LocalizationService.Instance.GetString("OM_ResultUnsupportedPlatform")
        };
    }

    private string BuildRegistrationMessage(FileAssociationRegistrationResult result)
    {
        return result.Status switch
        {
            FileAssociationRegistrationStatus.Registered
                => LocalizationService.Instance.GetString("OM_ResultRegistered"),
            FileAssociationRegistrationStatus.OpenedSettings
                => LocalizationService.Instance.GetString("OM_ResultOpenedDefaultAppsSettings"),
            FileAssociationRegistrationStatus.ExecutableNotFound
                => LocalizationService.Instance.GetString("OM_ResultExecutableNotFound"),
            FileAssociationRegistrationStatus.SettingsLaunchFailed
                => LocalizationService.Instance.GetString("OM_ResultSettingsLaunchFailed"),
            FileAssociationRegistrationStatus.Failed
                => string.Format(
                    LocalizationService.Instance.GetString("OM_ResultRegistrationFailed"),
                    result.ErrorMessage ?? LocalizationService.Instance.GetString("COM_Error")),
            _ => LocalizationService.Instance.GetString("OM_ResultUnsupportedPlatform")
        };
    }

    private List<OpenMethodTargetOption> BuildTargetOptions()
    {
        return
        [
            new OpenMethodTargetOption { Value = OpenMethodTargets.System, Label = LocalizationService.Instance.GetString("OM_System") },
            new OpenMethodTargetOption { Value = OpenMethodTargets.Office, Label = LocalizationService.Instance.GetString("OM_Office") },
            new OpenMethodTargetOption { Value = OpenMethodTargets.Wps, Label = LocalizationService.Instance.GetString("OM_Wps") }
        ];
    }

    private static string NormalizeTarget(string? target)
    {
        return target switch
        {
            OpenMethodTargets.Office => OpenMethodTargets.Office,
            OpenMethodTargets.Wps => OpenMethodTargets.Wps,
            _ => OpenMethodTargets.System
        };
    }

    private string GetTargetLabel(string target)
    {
        return target switch
        {
            OpenMethodTargets.Office => LocalizationService.Instance.GetString("OM_Office"),
            OpenMethodTargets.Wps => LocalizationService.Instance.GetString("OM_Wps"),
            OpenMethodTargets.Mixed => LocalizationService.Instance.GetString("OM_Mixed"),
            OpenMethodTargets.Unknown => LocalizationService.Instance.GetString("OM_Unknown"),
            _ => LocalizationService.Instance.GetString("OM_System")
        };
    }

    public override void Dispose()
    {
        AvaloniaFluentTheme.Instance.ThemeChanged -= OnThemeChanged;
        WeakReferenceMessenger.Default.UnregisterAll(this);
        base.Dispose();
    }
}
