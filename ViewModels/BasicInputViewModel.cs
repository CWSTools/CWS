using System;
using AvaloniaFluentUI.Locale;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallery.Models;
using Gallery.Services;
using Gallery.Services.Iccce;
using Gallery.Views;

namespace Gallery.ViewModels;

public partial class BasicInputViewModel : ViewModelBase
{
    private readonly AppConfig _config;
    private readonly IccceBridgeService _iccceBridgeService;
    private bool _isLoading;

    public override string Title => LocalizationService.Instance.GetString("MW_NavCommentsTitle");

    public string PageDescription => LocalizationService.Instance.GetString("AE_PageDescription");
    public string PptSectionTitle => LocalizationService.Instance.GetString("SV_ICCCE_PptSettings");
    public string PptSectionDescription => LocalizationService.Instance.GetString("SV_ICCCE_PptSettingsDescription");
    public string WhiteboardSectionTitle => LocalizationService.Instance.GetString("SV_ICCCE_AnnotationSettings");
    public string WhiteboardSectionDescription => LocalizationService.Instance.GetString("AE_WhiteboardDescription");
    public string OpenMiniWhiteboard => LocalizationService.Instance.GetString("AE_OpenMiniWhiteboard");
    public string OpenPptFloating => LocalizationService.Instance.GetString("PPT_FloatingWindowButton");
    public string PptSupportPowerPoint => LocalizationService.Instance.GetString("SV_ICCCE_PptSupportPowerPoint");
    public string PptSupportWps => LocalizationService.Instance.GetString("SV_ICCCE_PptSupportWps");
    public string PptAutoFloatingWindow => LocalizationService.Instance.GetString("SV_ICCCE_PptAutoFloatingWindow");
    public string PptAutoCloseIccceAfterSlideShow => "放映结束自动关闭 ICCCE";
    public string PptKeepTopmost => LocalizationService.Instance.GetString("SV_ICCCE_PptKeepTopmost");
    public string PptEnhancedPreview => LocalizationService.Instance.GetString("SV_ICCCE_PptEnhancedPreview");
    public string PptPreloadPreview => LocalizationService.Instance.GetString("SV_ICCCE_PptPreloadPreview");
    public string PptLongPressPageTurn => LocalizationService.Instance.GetString("SV_ICCCE_PptLongPressPageTurn");
    public string PptOptimisticPageNumber => LocalizationService.Instance.GetString("SV_ICCCE_PptOptimisticPageNumber");
    public string PptOptimisticPageHold => LocalizationService.Instance.GetString("SV_ICCCE_PptOptimisticPageHold");
    public string MiniWhiteboardEnabled => LocalizationService.Instance.GetString("SV_ICCCE_MiniWhiteboardEnabled");
    public string MiniWhiteboardSyncWithPpt => LocalizationService.Instance.GetString("SV_ICCCE_MiniWhiteboardSyncWithPpt");
    public string CanvasAdvancedSmoothing => LocalizationService.Instance.GetString("SV_ICCCE_CanvasAdvancedSmoothing");
    public string GestureFingerSlide => LocalizationService.Instance.GetString("SV_ICCCE_GestureFingerSlide");
    public string IccceShowToolbar => "展开 ICCCE";
    public string IccceBoard => "白板";
    public string IcccePreviousPage => "上一页";
    public string IccceNextPage => "下一页";
    public string IccceClearInk => "清空墨迹";
    public string IccceUndo => "撤销";
    public string IccceRedo => "重做";
    public string IcccePen => "画笔";
    public string IccceEraser => "橡皮";

    public BasicInputViewModel(AppConfig? config = null)
    {
        _config = config ?? RuntimeConfigService.Current;
        _iccceBridgeService = new IccceBridgeService(_config);
        LoadFeatureSettings();
    }

    [ObservableProperty]
    private bool _isIccceEnabled = true;

    [ObservableProperty]
    private bool _isPptSupportEnabled = true;

    [ObservableProperty]
    private bool _isPptSupportWpsEnabled = true;

    [ObservableProperty]
    private bool _isPptAutoFloatingWindowEnabled = true;

    [ObservableProperty]
    private bool _isPptAutoCloseIccceAfterSlideShowEnabled;

    [ObservableProperty]
    private bool _isPptKeepTopmostEnabled = true;

    [ObservableProperty]
    private bool _isPptEnhancedPreviewEnabled = true;

    [ObservableProperty]
    private bool _isPptPreloadPreviewEnabled;

    [ObservableProperty]
    private bool _isPptLongPressPageTurnEnabled = true;

    [ObservableProperty]
    private bool _isPptOptimisticPageNumberEnabled = true;

    [ObservableProperty]
    private double _pptOptimisticPageHoldMilliseconds = 1800;

    [ObservableProperty]
    private bool _isMiniWhiteboardEnabled = true;

    [ObservableProperty]
    private bool _isMiniWhiteboardSyncWithPptEnabled = true;

    [ObservableProperty]
    private bool _isCanvasAdvancedSmoothingEnabled = true;

    [ObservableProperty]
    private bool _isGestureFingerSlideEnabled = true;

    [RelayCommand]
    private void OpenPptFloatingWindow()
    {
        _iccceBridgeService.ShowFloatingBar();
    }

    [RelayCommand]
    private void OpenMiniWhiteboardWindow()
    {
        _iccceBridgeService.OpenWhiteboard();
    }

    [RelayCommand]
    private void LaunchIccceShow() => _iccceBridgeService.ShowFloatingBar();

    [RelayCommand]
    private void LaunchIccceBoard() => _iccceBridgeService.OpenWhiteboard();

    [RelayCommand]
    private void LaunchIcccePreviousPage() => _iccceBridgeService.SendUriCommand("page/previous");

    [RelayCommand]
    private void LaunchIccceNextPage() => _iccceBridgeService.SendUriCommand("page/next");

    [RelayCommand]
    private void LaunchIccceClearInk() => _iccceBridgeService.SendUriCommand("clear");

    [RelayCommand]
    private void LaunchIccceUndo() => _iccceBridgeService.SendUriCommand("undo");

    [RelayCommand]
    private void LaunchIccceRedo() => _iccceBridgeService.SendUriCommand("redo");

    [RelayCommand]
    private void LaunchIcccePen() => _iccceBridgeService.SendUriCommand("tool/pen");

    [RelayCommand]
    private void LaunchIccceEraser() => _iccceBridgeService.SendUriCommand("tool/eraser");

    partial void OnIsIccceEnabledChanged(bool value) => SaveFeatureSettings();
    partial void OnIsPptSupportEnabledChanged(bool value) => SaveFeatureSettings();
    partial void OnIsPptSupportWpsEnabledChanged(bool value) => SaveFeatureSettings();
    partial void OnIsPptAutoFloatingWindowEnabledChanged(bool value) => SaveFeatureSettings();
    partial void OnIsPptAutoCloseIccceAfterSlideShowEnabledChanged(bool value) => SaveFeatureSettings();
    partial void OnIsPptKeepTopmostEnabledChanged(bool value) => SaveFeatureSettings();
    partial void OnIsPptEnhancedPreviewEnabledChanged(bool value) => SaveFeatureSettings();
    partial void OnIsPptPreloadPreviewEnabledChanged(bool value) => SaveFeatureSettings();
    partial void OnIsPptLongPressPageTurnEnabledChanged(bool value) => SaveFeatureSettings();
    partial void OnIsPptOptimisticPageNumberEnabledChanged(bool value) => SaveFeatureSettings();
    partial void OnPptOptimisticPageHoldMillisecondsChanged(double value) => SaveFeatureSettings();
    partial void OnIsMiniWhiteboardEnabledChanged(bool value) => SaveFeatureSettings();
    partial void OnIsMiniWhiteboardSyncWithPptEnabledChanged(bool value) => SaveFeatureSettings();
    partial void OnIsCanvasAdvancedSmoothingEnabledChanged(bool value) => SaveFeatureSettings();
    partial void OnIsGestureFingerSlideEnabledChanged(bool value) => SaveFeatureSettings();

    private void LoadFeatureSettings()
    {
        _isLoading = true;
        try
        {
            IsIccceEnabled = _config.Iccce.IsEnabled;
            IsPptSupportEnabled = _config.Iccce.Ppt.PowerPointSupport;
            IsPptSupportWpsEnabled = _config.Iccce.Ppt.SupportWps;
            IsPptAutoFloatingWindowEnabled = _config.Iccce.Ppt.AutoShowFloatingWindow;
            IsPptAutoCloseIccceAfterSlideShowEnabled = _config.Iccce.Ppt.AutoCloseIccceAfterSlideShow;
            IsPptKeepTopmostEnabled = _config.Iccce.Ppt.KeepFloatingWindowTopmost;
            IsPptEnhancedPreviewEnabled = _config.Iccce.Ppt.EnhancedPreview;
            IsPptPreloadPreviewEnabled = _config.Iccce.Ppt.PreloadPreview;
            IsPptLongPressPageTurnEnabled = _config.Iccce.Ppt.LongPressPageTurn;
            IsPptOptimisticPageNumberEnabled = _config.Iccce.Ppt.OptimisticPageNumber;
            PptOptimisticPageHoldMilliseconds = _config.Iccce.Ppt.OptimisticPageHoldMilliseconds;
            IsMiniWhiteboardEnabled = _config.Iccce.MiniWhiteboard.IsEnabled;
            IsMiniWhiteboardSyncWithPptEnabled = _config.Iccce.MiniWhiteboard.SyncWithPptPages;
            IsCanvasAdvancedSmoothingEnabled = _config.Iccce.Canvas.UseAdvancedBezierSmoothing;
            IsGestureFingerSlideEnabled = _config.Iccce.Gesture.EnableFingerGestureSlideShowControl;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void SaveFeatureSettings()
    {
        if (_isLoading)
        {
            return;
        }

        RuntimeConfigService.Update(config =>
        {
            config.Iccce.IsEnabled = IsIccceEnabled;
            config.Iccce.Ppt.PowerPointSupport = IsPptSupportEnabled;
            config.Iccce.Ppt.SupportWps = IsPptSupportWpsEnabled;
            config.Iccce.Ppt.AutoShowFloatingWindow = IsPptAutoFloatingWindowEnabled;
            config.Iccce.Ppt.AutoCloseIccceAfterSlideShow = IsPptAutoCloseIccceAfterSlideShowEnabled;
            config.Iccce.Ppt.KeepFloatingWindowTopmost = IsPptKeepTopmostEnabled;
            config.Iccce.Ppt.EnhancedPreview = IsPptEnhancedPreviewEnabled;
            config.Iccce.Ppt.PreloadPreview = IsPptPreloadPreviewEnabled;
            config.Iccce.Ppt.LongPressPageTurn = IsPptLongPressPageTurnEnabled;
            config.Iccce.Ppt.OptimisticPageNumber = IsPptOptimisticPageNumberEnabled;
            config.Iccce.Ppt.OptimisticPageHoldMilliseconds =
                Math.Clamp(Convert.ToInt32(PptOptimisticPageHoldMilliseconds), 300, 5000);
            config.Iccce.MiniWhiteboard.IsEnabled = IsMiniWhiteboardEnabled;
            config.Iccce.MiniWhiteboard.SyncWithPptPages = IsMiniWhiteboardSyncWithPptEnabled;
            config.Iccce.Canvas.UseAdvancedBezierSmoothing = IsCanvasAdvancedSmoothingEnabled;
            config.Iccce.Gesture.EnableFingerGestureSlideShowControl = IsGestureFingerSlideEnabled;
        });
    }

    protected override void OnLanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnLanguageChanged(sender, e);
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(PageDescription));
        OnPropertyChanged(nameof(PptSectionTitle));
        OnPropertyChanged(nameof(PptSectionDescription));
        OnPropertyChanged(nameof(WhiteboardSectionTitle));
        OnPropertyChanged(nameof(WhiteboardSectionDescription));
        OnPropertyChanged(nameof(OpenMiniWhiteboard));
        OnPropertyChanged(nameof(OpenPptFloating));
        OnPropertyChanged(nameof(PptSupportPowerPoint));
        OnPropertyChanged(nameof(PptSupportWps));
        OnPropertyChanged(nameof(PptAutoFloatingWindow));
        OnPropertyChanged(nameof(PptAutoCloseIccceAfterSlideShow));
        OnPropertyChanged(nameof(PptKeepTopmost));
        OnPropertyChanged(nameof(PptEnhancedPreview));
        OnPropertyChanged(nameof(PptPreloadPreview));
        OnPropertyChanged(nameof(PptLongPressPageTurn));
        OnPropertyChanged(nameof(PptOptimisticPageNumber));
        OnPropertyChanged(nameof(PptOptimisticPageHold));
        OnPropertyChanged(nameof(MiniWhiteboardEnabled));
        OnPropertyChanged(nameof(MiniWhiteboardSyncWithPpt));
        OnPropertyChanged(nameof(CanvasAdvancedSmoothing));
        OnPropertyChanged(nameof(GestureFingerSlide));
        OnPropertyChanged(nameof(IccceShowToolbar));
        OnPropertyChanged(nameof(IccceBoard));
        OnPropertyChanged(nameof(IcccePreviousPage));
        OnPropertyChanged(nameof(IccceNextPage));
        OnPropertyChanged(nameof(IccceClearInk));
        OnPropertyChanged(nameof(IccceUndo));
        OnPropertyChanged(nameof(IccceRedo));
        OnPropertyChanged(nameof(IcccePen));
        OnPropertyChanged(nameof(IccceEraser));
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}
