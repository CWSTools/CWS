using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaFluentUI.Locale;
using AvaloniaFluentUI.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Gallery.Messages.MainWindowMessages;
using Gallery.Models;

namespace Gallery.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    public override string Title => LocalizationService.Instance.GetString("HV_Title");
    
    [ObservableProperty]
    private Vector _scrollViewerOffset =  new Vector();

    public Vector Vector => ScrollViewerOffset;

    public string HeroDescription => LocalizationService.Instance.GetString("HV_HeroDescription");
    
    public HomeViewModel(AppConfig? config = null)
    {
#if DEBUG
        Debug.WriteLine("HomeViewModel Init");
#endif
        IsBackgroundImageEnabled = config?.IsEnabledBackgroundImage ?? false;
        AvaloniaFluentTheme.Instance.ThemeChanged += OnThemeChanged;
        WeakReferenceMessenger.Default.Register<EnabledBackgroundImageMessage>(
            this,
            (_, message) => IsBackgroundImageEnabled = message.IsVisible);

        CoreWorkspaceItems = ButtonItemModel.CreateList(
            ("Button", LocalizationService.Instance.GetString("HV_OpenAssociationTitle"), "Icons", LocalizationService.Instance.GetString("HV_OpenAssociationDescription")),
            ("TextBox", LocalizationService.Instance.GetString("HV_CommentsTitle"), "BasicInput", LocalizationService.Instance.GetString("HV_CommentsDescription")),
            ("CommandBar", LocalizationService.Instance.GetString("HV_BatchToolsTitle"), "DialogBoxAndPopup", LocalizationService.Instance.GetString("HV_BatchToolsDescription")),
            ("StackPanel", LocalizationService.Instance.GetString("HV_DocumentCleanupTitle"), "Layout", LocalizationService.Instance.GetString("HV_DocumentCleanupDescription"))
        );
    }

    protected override void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
    {
        base.OnLanguageChanged(sender, e);
        OnPropertyChanged(nameof(HeroDescription));
    }

    [ObservableProperty]
    private bool _isBackgroundImageEnabled;

    partial void OnIsBackgroundImageEnabledChanged(bool value) => RefreshHomeChrome();

    private bool IsDarkTheme => AvaloniaFluentTheme.Instance.IsDarkTheme;

    public IBrush PageOverlayBrush => IsBackgroundImageEnabled
        ? Brush.Parse(IsDarkTheme ? "#8A0B0D12" : "#30F7F9FB")
        : Brushes.Transparent;

    public IBrush GlassPanelBackground => Brush.Parse(IsBackgroundImageEnabled
        ? IsDarkTheme ? "#781A1D24" : "#74FFFFFF"
        : IsDarkTheme ? "#E01F2024" : "#F5FFFFFF");

    public IBrush GlassPanelBorderBrush => Brush.Parse(IsBackgroundImageEnabled
        ? IsDarkTheme ? "#32FFFFFF" : "#66FFFFFF"
        : IsDarkTheme ? "#2EFFFFFF" : "#22000000");

    public IBrush MiniTileBackground => Brush.Parse(IsBackgroundImageEnabled
        ? IsDarkTheme ? "#5E232730" : "#62FFFFFF"
        : IsDarkTheme ? "#B82A2C31" : "#FAFFFFFF");

    public IBrush MiniTileBorderBrush => Brush.Parse(IsBackgroundImageEnabled
        ? IsDarkTheme ? "#28FFFFFF" : "#55FFFFFF"
        : IsDarkTheme ? "#22FFFFFF" : "#18000000");

    public IBrush PillBackground => Brush.Parse(IsBackgroundImageEnabled
        ? IsDarkTheme ? "#362C313A" : "#88FFFFFF"
        : IsDarkTheme ? "#2AFFFFFF" : "#ECFFFFFF");

    public IBrush IconTileBackground => Brush.Parse(IsDarkTheme ? "#243B42" : "#DCEFF2");

    private void OnThemeChanged(object? sender, ThemeVariant? variant) => RefreshHomeChrome();

    private void RefreshHomeChrome()
    {
        OnPropertyChanged(nameof(PageOverlayBrush));
        OnPropertyChanged(nameof(GlassPanelBackground));
        OnPropertyChanged(nameof(GlassPanelBorderBrush));
        OnPropertyChanged(nameof(MiniTileBackground));
        OnPropertyChanged(nameof(MiniTileBorderBrush));
        OnPropertyChanged(nameof(PillBackground));
        OnPropertyChanged(nameof(IconTileBackground));
    }

    public void ReleaseImages()
    {
        foreach (var item in AllSources)
        {
            item.ReleaseImage();
        }
    }

    public IEnumerable<ButtonItemModel> AllSources
    {
        get
        {
            return CoreWorkspaceItems;
        }
    }

    public List<ButtonItemModel> CoreWorkspaceItems { get; }

    public override void Dispose()
    {
        AvaloniaFluentTheme.Instance.ThemeChanged -= OnThemeChanged;
        WeakReferenceMessenger.Default.UnregisterAll(this);
        ReleaseImages();
        base.Dispose();
    }
}
