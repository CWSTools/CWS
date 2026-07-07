using System.ComponentModel;
using System.Reflection;
using AvaloniaFluentUI.Locale;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gallery.ViewModels;

public partial class AboutViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _updateStatus = string.Empty;

    public override string Title => LocalizationService.Instance.GetString("About");

    public string AuthorLabel => LocalizationService.Instance.GetString("AV_Author");
    public string Author => Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "CWS";
    public string VersionLabel => LocalizationService.Instance.GetString("AV_Version");
    public string Version => Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
        ?? "0.0.0";
    public string UpdateTitle => LocalizationService.Instance.GetString("AV_CheckUpdate");
    public string UpdateDescription => LocalizationService.Instance.GetString("AV_UpdateDescription");
    public string CheckUpdateButton => LocalizationService.Instance.GetString("AV_CheckUpdateButton");

    public AboutViewModel()
    {
        UpdateStatus = LocalizationService.Instance.GetString("AV_UpdateIdle");
    }

    protected override void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
    {
        base.OnLanguageChanged(sender, e);
        OnPropertyChanged(nameof(AuthorLabel));
        OnPropertyChanged(nameof(VersionLabel));
        OnPropertyChanged(nameof(UpdateTitle));
        OnPropertyChanged(nameof(UpdateDescription));
        OnPropertyChanged(nameof(CheckUpdateButton));
        UpdateStatus = LocalizationService.Instance.GetString("AV_UpdateIdle");
    }

    [RelayCommand]
    private void CheckUpdate()
    {
        UpdateStatus = string.Format(LocalizationService.Instance.GetString("AV_UpdateCurrentVersion"), Version);
    }
}
