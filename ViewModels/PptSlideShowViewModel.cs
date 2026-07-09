using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaFluentUI.Locale;
using AvaloniaFluentUI.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Gallery.Messages.MainWindowMessages;
using Gallery.Models;
using Gallery.Services;
using Gallery.Services.Ppt;

namespace Gallery.ViewModels;

public partial class PptSlideShowViewModel : ViewModelBase
{
    private const int PreviewThumbnailWidth = 320;
    private const int PreviewThumbnailHeight = 180;

    private readonly AppConfig _config;
    private readonly IPptSlideShowService _slideShowService;
    private readonly DispatcherTimer _refreshTimer;
    private readonly DispatcherTimer _longPressTimer;
    private int _optimisticSlideNumber;
    private DateTime _optimisticSlideUntil = DateTime.MinValue;
    private bool _isLongPressNext;
    private bool _isLongPressRepeating;
    private int _lastDisplayedCurrentSlide;
    private int _lastDisplayedTotalSlides;

    public override string Title => LocalizationService.Instance.GetString("PPT_Title");

    public PptSlideShowViewModel(AppConfig? config)
    {
        _config = config ?? RuntimeConfigService.Current;
        _slideShowService = new WindowsPptSlideShowService(_config);
        IsBackgroundImageEnabled = _config.IsEnabledBackgroundImage;

        AvaloniaFluentTheme.Instance.ThemeChanged += OnThemeChanged;
        WeakReferenceMessenger.Default.Register<EnabledBackgroundImageMessage>(
            this,
            (_, message) => IsBackgroundImageEnabled = message.IsVisible);

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1.5)
        };
        _refreshTimer.Tick += (_, _) => RefreshState();
        _longPressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(90)
        };
        _longPressTimer.Tick += (_, _) =>
        {
            _longPressTimer.Interval = TimeSpan.FromMilliseconds(90);
            _isLongPressRepeating = true;

            if (_isLongPressNext)
            {
                Next();
            }
            else
            {
                Previous();
            }
        };

        Dispatcher.UIThread.Post(() =>
        {
            RefreshState();
            if (_config.Iccce.Ppt.PreloadPreview && _config.Iccce.Ppt.EnhancedPreview)
            {
                _ = LoadPreviewItemsAsync();
            }

            _refreshTimer.Start();
        });
    }

    [ObservableProperty]
    private bool _isBackgroundImageEnabled;

    partial void OnIsBackgroundImageEnabledChanged(bool value) => RefreshChrome();

    [ObservableProperty]
    private bool _isConnected;

    partial void OnIsConnectedChanged(bool value)
    {
        OnPropertyChanged(nameof(FloatingCurrentSlideText));
        OnPropertyChanged(nameof(FloatingTotalSlidesText));
        OnPropertyChanged(nameof(SlideCounter));
    }

    [ObservableProperty]
    private bool _isInSlideShow;

    partial void OnIsInSlideShowChanged(bool value)
    {
        OnPropertyChanged(nameof(CanStartSlideShow));
        OnPropertyChanged(nameof(CanControlSlideShow));
    }

    [ObservableProperty]
    private int _currentSlide;

    partial void OnCurrentSlideChanged(int value)
    {
        if (value > 0)
        {
            _lastDisplayedCurrentSlide = value;
        }

        OnPropertyChanged(nameof(FloatingCurrentSlideText));
        OnPropertyChanged(nameof(SlideCounter));
    }

    [ObservableProperty]
    private int _totalSlides;

    partial void OnTotalSlidesChanged(int value)
    {
        if (value > 0)
        {
            _lastDisplayedTotalSlides = value;
        }

        OnPropertyChanged(nameof(FloatingTotalSlidesText));
        OnPropertyChanged(nameof(SlideCounter));
        OnPropertyChanged(nameof(FloatingWindowWidth));
        OnPropertyChanged(nameof(FloatingWindowHeight));
    }

    [ObservableProperty]
    private double _targetSlideNumber = 1;

    [ObservableProperty]
    private string _presentationName = string.Empty;

    [ObservableProperty]
    private string _presentationPath = string.Empty;

    [ObservableProperty]
    private string _connectionKind = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isPreviewExpanded;

    partial void OnIsPreviewExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(FloatingWindowWidth));
        OnPropertyChanged(nameof(FloatingWindowHeight));
        OnPropertyChanged(nameof(FloatingChromeOpacity));

        if (value && PreviewItems.Count == 0)
        {
            _ = LoadPreviewItemsAsync();
        }
    }

    public ObservableCollection<PptSlidePreviewItemModel> PreviewItems { get; } = [];

    public bool CanStartSlideShow => IsConnected && !IsInSlideShow;
    public bool CanControlSlideShow => IsConnected && IsInSlideShow;
    public string SlideCounter => ResolveDisplayedCurrentSlide() > 0 && ResolveDisplayedTotalSlides() > 0
        ? $"{ResolveDisplayedCurrentSlide()} / {ResolveDisplayedTotalSlides()}"
        : "- / -";
    public string FloatingCurrentSlideText => ResolveDisplayedCurrentSlide() > 0 ? ResolveDisplayedCurrentSlide().ToString() : "?";
    public string FloatingTotalSlidesText => ResolveDisplayedTotalSlides() > 0 ? $"/ {ResolveDisplayedTotalSlides()}" : "/ ?";
    public string ConnectionLabel => IsConnected ? $"{ConnectedLabel} · {ConnectionKind}" : DisconnectedLabel;
    public string ActiveFileLabel => string.IsNullOrWhiteSpace(PresentationName) ? NoPresentationLabel : PresentationName;
    public string FloatingWindowPosition => NormalizeFloatingWindowPosition(_config.Iccce.Ppt.FloatingWindowPosition);
    public bool IsFloatingSideLayout => FloatingWindowPosition is "LeftSide" or "RightSide";
    public double FloatingButtonSize => 50 * FloatingWindowScale;
    public double FloatingIconSize => 28 * FloatingWindowScale;
    public double FloatingPageFontSize => 17 * FloatingWindowScale;
    public double FloatingPageTotalFontSize => 10 * FloatingWindowScale;
    public double FloatingButtonGroupLength => FloatingButtonSize * (IsPageNumberVisible ? 6 : 5);
    public double FloatingPreviewWidth => (IsFloatingSideLayout ? 240 : Math.Max(280, FloatingButtonGroupLength / FloatingWindowScale)) * FloatingWindowScale;
    public double FloatingPreviewMaxHeight => (IsFloatingSideLayout ? 480 : 380) * FloatingWindowScale;
    public double FloatingWindowWidth => IsFloatingSideLayout
        ? (IsPreviewExpanded ? FloatingButtonSize + FloatingPreviewWidth : FloatingButtonSize)
        : (IsPreviewExpanded ? FloatingPreviewWidth : FloatingButtonGroupLength);
    public double FloatingWindowHeight => IsFloatingSideLayout
        ? (IsPreviewExpanded ? Math.Max(FloatingButtonGroupLength, FloatingPreviewMaxHeight) : FloatingButtonGroupLength)
        : (IsPreviewExpanded ? FloatingPreviewMaxHeight + FloatingButtonSize : FloatingButtonSize);
    public double FloatingWindowScale => Math.Clamp(_config.Iccce.Ppt.FloatingWindowScale, 0.75, 1.5);
    public double FloatingWindowOpacity => Math.Clamp(_config.Iccce.Ppt.FloatingWindowOpacity, 0.35, 1.0);
    public double FloatingChromeOpacity => IsPreviewExpanded ? 1.0 : FloatingWindowOpacity;
    public bool IsPageNumberVisible => _config.Iccce.Ppt.ShowPageNumber;

    public string PageDescription => LocalizationService.Instance.GetString("PPT_PageDescription");
    public string OpenButton => LocalizationService.Instance.GetString("PPT_OpenButton");
    public string RefreshButton => LocalizationService.Instance.GetString("PPT_RefreshButton");
    public string FloatingWindowButton => LocalizationService.Instance.GetString("PPT_FloatingWindowButton");
    public string StartButton => LocalizationService.Instance.GetString("PPT_StartButton");
    public string EndButton => LocalizationService.Instance.GetString("PPT_EndButton");
    public string PreviousButton => LocalizationService.Instance.GetString("PPT_PreviousButton");
    public string NextButton => LocalizationService.Instance.GetString("PPT_NextButton");
    public string GotoButton => LocalizationService.Instance.GetString("PPT_GotoButton");
    public string TargetSlideLabel => LocalizationService.Instance.GetString("PPT_TargetSlideLabel");
    public string ConnectionStatusTitle => LocalizationService.Instance.GetString("PPT_ConnectionStatusTitle");
    public string CurrentDeckTitle => LocalizationService.Instance.GetString("PPT_CurrentDeckTitle");
    public string SlideCounterTitle => LocalizationService.Instance.GetString("PPT_SlideCounterTitle");
    private string ConnectedLabel => LocalizationService.Instance.GetString("PPT_Connected");
    private string DisconnectedLabel => LocalizationService.Instance.GetString("PPT_Disconnected");
    private string NoPresentationLabel => LocalizationService.Instance.GetString("PPT_NoPresentation");

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

    public IBrush InnerPanelBackground => Brush.Parse(IsBackgroundImageEnabled
        ? IsDarkTheme ? "#402F3540" : "#4AFFFFFF"
        : IsDarkTheme ? "#59313740" : "#F7FFFFFF");

    public IBrush InnerPanelBorderBrush => Brush.Parse(IsBackgroundImageEnabled
        ? IsDarkTheme ? "#24FFFFFF" : "#2E142236"
        : IsDarkTheme ? "#24FFFFFF" : "#18000000");

    public async Task OpenPresentationAsync(string filePath)
    {
        var preferences = RuntimeConfigService.Current.OpenMethodPreferences;
        var result = _slideShowService.OpenPresentation(filePath, preferences);
        StatusMessage = BuildOpenFileMessage(result);
        AppLoggerService.Info("ppt", $"Open presentation requested. File={filePath}, Status={result.Status}");

        await Task.Delay(1500);
        RefreshState();
    }

    [RelayCommand]
    private void Refresh()
    {
        RefreshState();
    }

    public void RefreshNow() => RefreshState();

    [RelayCommand]
    private void StartSlideShow()
    {
        StatusMessage = _slideShowService.StartSlideShow()
            ? LocalizationService.Instance.GetString("PPT_ResultStartRequested")
            : LocalizationService.Instance.GetString("PPT_ResultCommandFailed");
        RefreshState();
    }

    [RelayCommand]
    private void EndSlideShow()
    {
        StatusMessage = _slideShowService.EndSlideShow()
            ? LocalizationService.Instance.GetString("PPT_ResultEndRequested")
            : LocalizationService.Instance.GetString("PPT_ResultCommandFailed");
        RefreshState();
    }

    [RelayCommand]
    private void Next()
    {
        var optimisticSlide = GetOptimisticSlideDelta(1);
        if (optimisticSlide > 0 && _config.Iccce.Ppt.OptimisticPageNumber)
        {
            ApplyOptimisticSlide(optimisticSlide);
        }

        if (_slideShowService.Next())
        {
            StatusMessage = LocalizationService.Instance.GetString("PPT_ResultNextRequested");
            return;
        }

        ClearOptimisticSlide();
        StatusMessage = LocalizationService.Instance.GetString("PPT_ResultCommandFailed");
        RefreshState();
    }

    [RelayCommand]
    private void Previous()
    {
        var optimisticSlide = GetOptimisticSlideDelta(-1);
        if (optimisticSlide > 0 && _config.Iccce.Ppt.OptimisticPageNumber)
        {
            ApplyOptimisticSlide(optimisticSlide);
        }

        if (_slideShowService.Previous())
        {
            StatusMessage = LocalizationService.Instance.GetString("PPT_ResultPreviousRequested");
            return;
        }

        ClearOptimisticSlide();
        StatusMessage = LocalizationService.Instance.GetString("PPT_ResultCommandFailed");
        RefreshState();
    }

    [RelayCommand]
    private void GoToSlide()
    {
        var slide = Math.Max(1, Convert.ToInt32(TargetSlideNumber));
        if (_config.Iccce.Ppt.OptimisticPageNumber)
        {
            ApplyOptimisticSlide(slide);
        }

        StatusMessage = _slideShowService.GoToSlide(slide)
            ? string.Format(LocalizationService.Instance.GetString("PPT_ResultGotoRequested"), slide)
            : LocalizationService.Instance.GetString("PPT_ResultCommandFailed");
        RefreshState();
    }

    [RelayCommand]
    private void TogglePreview()
    {
        if (!_config.Iccce.Ppt.PageButtonClickable || !_config.Iccce.Ppt.EnhancedPreview)
        {
            return;
        }

        IsPreviewExpanded = !IsPreviewExpanded;
    }

    [RelayCommand]
    private void SelectPreviewSlide(PptSlidePreviewItemModel? item)
    {
        if (item == null)
        {
            return;
        }

        IsPreviewExpanded = false;
        TargetSlideNumber = item.SlideNumber;
        GoToSlide();
    }

    public void BeginLongPress(bool next)
    {
        if (!_config.Iccce.Ppt.LongPressPageTurn)
        {
            return;
        }

        _isLongPressNext = next;
        _isLongPressRepeating = false;
        _longPressTimer.Stop();
        _longPressTimer.Interval = TimeSpan.FromMilliseconds(420);
        _longPressTimer.Start();
    }

    public void EndLongPress()
    {
        _longPressTimer.Stop();
        _longPressTimer.Interval = TimeSpan.FromMilliseconds(90);
    }

    public bool ConsumeLongPressRepeating()
    {
        var value = _isLongPressRepeating;
        _isLongPressRepeating = false;
        return value;
    }

    private void RefreshState()
    {
        PptSlideShowState state;
        try
        {
            state = _slideShowService.RefreshState();
        }
        catch (Exception ex)
        {
            state = PptSlideShowState.Disconnected(
                string.Format(LocalizationService.Instance.GetString("PPT_ResultStateFailed"), ex.Message));
        }

        IsConnected = state.IsConnected;
        IsInSlideShow = state.IsInSlideShow;

        if (!state.IsConnected)
        {
            _lastDisplayedCurrentSlide = 0;
            _lastDisplayedTotalSlides = 0;
        }

        var currentSlide = state.CurrentSlide;
        var hasOptimisticSlide = state.IsConnected &&
                                 state.IsInSlideShow &&
                                 _config.Iccce.Ppt.OptimisticPageNumber &&
                                 _optimisticSlideNumber > 0 &&
                                 DateTime.UtcNow <= _optimisticSlideUntil;

        if (hasOptimisticSlide)
        {
            if (currentSlide == _optimisticSlideNumber)
            {
                ClearOptimisticSlide();
            }
            else
            {
                currentSlide = _optimisticSlideNumber;
            }
        }

        if (state.IsConnected && state.IsInSlideShow && currentSlide <= 0 && CurrentSlide > 0)
        {
            currentSlide = CurrentSlide;
        }

        var totalSlides = state.TotalSlides;
        if (state.IsConnected && totalSlides <= 0 && TotalSlides > 0)
        {
            totalSlides = TotalSlides;
        }

        CurrentSlide = currentSlide;
        TotalSlides = totalSlides;
        PresentationName = state.PresentationName;
        PresentationPath = state.PresentationPath;
        ConnectionKind = state.ConnectionKind;

        if (!state.IsConnected)
        {
            IsPreviewExpanded = false;
            PreviewItems.Clear();
        }

        if (!string.IsNullOrWhiteSpace(state.StatusMessage) && string.IsNullOrWhiteSpace(StatusMessage))
        {
            StatusMessage = state.StatusMessage;
        }
        else if (!state.IsConnected)
        {
            StatusMessage = state.StatusMessage;
        }

        if (CurrentSlide > 0)
        {
            TargetSlideNumber = CurrentSlide;
        }

        RefreshStateDependentProperties();
    }

    private int GetOptimisticSlideDelta(int delta)
    {
        if (!CanControlSlideShow || CurrentSlide <= 0)
        {
            return 0;
        }

        var slide = CurrentSlide + delta;
        if (TotalSlides > 0)
        {
            slide = Math.Clamp(slide, 1, TotalSlides);
        }
        else
        {
            slide = Math.Max(1, slide);
        }

        return slide == CurrentSlide ? 0 : slide;
    }

    private void ApplyOptimisticSlide(int slideNumber)
    {
        _optimisticSlideNumber = slideNumber;
        _optimisticSlideUntil = DateTime.UtcNow.AddMilliseconds(
            Math.Clamp(_config.Iccce.Ppt.OptimisticPageHoldMilliseconds, 300, 5000));
        CurrentSlide = slideNumber;
        TargetSlideNumber = slideNumber;
        RefreshStateDependentProperties();
    }

    private void ClearOptimisticSlide()
    {
        _optimisticSlideNumber = 0;
        _optimisticSlideUntil = DateTime.MinValue;
    }

    private void RefreshStateDependentProperties()
    {
        OnPropertyChanged(nameof(CanStartSlideShow));
        OnPropertyChanged(nameof(CanControlSlideShow));
        OnPropertyChanged(nameof(SlideCounter));
        OnPropertyChanged(nameof(FloatingCurrentSlideText));
        OnPropertyChanged(nameof(FloatingTotalSlidesText));
        OnPropertyChanged(nameof(ConnectionLabel));
        OnPropertyChanged(nameof(ActiveFileLabel));
    }

    private int ResolveDisplayedCurrentSlide()
    {
        if (!IsConnected)
        {
            return 0;
        }

        if (CurrentSlide > 0)
        {
            return CurrentSlide;
        }

        return _lastDisplayedCurrentSlide;
    }

    private int ResolveDisplayedTotalSlides()
    {
        if (!IsConnected)
        {
            return 0;
        }

        if (TotalSlides > 0)
        {
            return TotalSlides;
        }

        return _lastDisplayedTotalSlides;
    }

    private static string NormalizeFloatingWindowPosition(string? position) =>
        position switch
        {
            "LeftBottom" => "LeftBottom",
            "RightBottom" => "RightBottom",
            "LeftSide" => "LeftSide",
            "RightSide" => "RightSide",
            _ => "RightBottom"
        };

    private async Task LoadPreviewItemsAsync()
    {
        if (!IsConnected || TotalSlides <= 0 || !_config.Iccce.Ppt.EnhancedPreview)
        {
            return;
        }

        try
        {
            var thumbnails = await Task.Run(() =>
                _slideShowService.ExportSlideThumbnails(PreviewThumbnailWidth, PreviewThumbnailHeight));

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PreviewItems.Clear();
                foreach (var thumbnail in thumbnails)
                {
                    PreviewItems.Add(new PptSlidePreviewItemModel
                    {
                        SlideNumber = thumbnail.SlideNumber,
                        Thumbnail = CreateBitmap(thumbnail.PngBytes)
                    });
                }
            });
        }
        catch (Exception ex)
        {
            AppLoggerService.Info("ppt", $"加载 PPT 缩略图失败: {ex}");
        }
    }

    private static Bitmap? CreateBitmap(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return null;
        }

        try
        {
            return new Bitmap(new MemoryStream(bytes, false));
        }
        catch
        {
            return null;
        }
    }

    private string BuildOpenFileMessage(FileOpenResult result)
    {
        return result.Status switch
        {
            FileOpenStatus.OpenedWithTarget when !string.IsNullOrWhiteSpace(result.TargetAppDisplayName)
                => string.Format(LocalizationService.Instance.GetString("PPT_ResultOpenedWithTarget"), result.TargetAppDisplayName),
            FileOpenStatus.OpenedWithSystem
                => LocalizationService.Instance.GetString("PPT_ResultOpenedWithSystem"),
            FileOpenStatus.TargetAppNotFound
                => string.Format(LocalizationService.Instance.GetString("OM_ResultTargetNotFound"), result.Target ?? LocalizationService.Instance.GetString("OM_SelectedApp")),
            FileOpenStatus.FileNotFound
                => LocalizationService.Instance.GetString("OM_ResultFileNotFound"),
            FileOpenStatus.LaunchFailed
                => string.Format(LocalizationService.Instance.GetString("OM_ResultLaunchFailed"), result.ErrorMessage ?? LocalizationService.Instance.GetString("COM_Error")),
            FileOpenStatus.SystemDefaultRoutesToCwsTool
                => LocalizationService.Instance.GetString("OM_ResultSystemDefaultRoutesToCwsTool"),
            _ => LocalizationService.Instance.GetString("OM_ResultUnsupportedPlatform")
        };
    }

    private void OnThemeChanged(object? sender, ThemeVariant? variant) => RefreshChrome();

    private void RefreshChrome()
    {
        OnPropertyChanged(nameof(PageOverlayBrush));
        OnPropertyChanged(nameof(GlassPanelBackground));
        OnPropertyChanged(nameof(GlassPanelBorderBrush));
        OnPropertyChanged(nameof(InnerPanelBackground));
        OnPropertyChanged(nameof(InnerPanelBorderBrush));
    }

    protected override void OnLanguageChanged(object? sender, PropertyChangedEventArgs e)
    {
        base.OnLanguageChanged(sender, e);
        OnPropertyChanged(nameof(PageDescription));
        OnPropertyChanged(nameof(OpenButton));
        OnPropertyChanged(nameof(RefreshButton));
        OnPropertyChanged(nameof(FloatingWindowButton));
        OnPropertyChanged(nameof(StartButton));
        OnPropertyChanged(nameof(EndButton));
        OnPropertyChanged(nameof(PreviousButton));
        OnPropertyChanged(nameof(NextButton));
        OnPropertyChanged(nameof(GotoButton));
        OnPropertyChanged(nameof(TargetSlideLabel));
        OnPropertyChanged(nameof(ConnectionStatusTitle));
        OnPropertyChanged(nameof(CurrentDeckTitle));
        OnPropertyChanged(nameof(SlideCounterTitle));
        RefreshStateDependentProperties();
    }

    public override void Dispose()
    {
        _refreshTimer.Stop();
        _longPressTimer.Stop();
        _slideShowService.Dispose();
        AvaloniaFluentTheme.Instance.ThemeChanged -= OnThemeChanged;
        WeakReferenceMessenger.Default.UnregisterAll(this);
        base.Dispose();
    }
}
