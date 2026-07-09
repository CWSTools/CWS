using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Gallery.Services.Ppt;

#pragma warning disable CA1416

internal sealed class IcccePptManager : IDisposable
{
    private readonly object _syncRoot = new();
    private object? _application;
    private object? _currentPresentation;
    private object? _currentSlides;
    private object? _currentSlide;
    private bool _cachedIsConnected;
    private bool _cachedIsInSlideShow;
    private bool _disposed;
    private int _lastKnownSlideNumber;

    public bool IsSupportWps { get; set; } = true;

    public bool IsConnected => _application != null && _cachedIsConnected;

    public bool IsInSlideShow => _cachedIsInSlideShow;

    public int SlidesCount { get; private set; }

    public string ConnectionKind { get; private set; } = string.Empty;

    public bool EnsureConnected()
    {
        if (!OperatingSystem.IsWindows() || _disposed)
        {
            return false;
        }

        lock (_syncRoot)
        {
            if (_application != null && IsComApplicationAlive(_application))
            {
                _cachedIsConnected = true;
                RefreshIsInSlideShowFromCom();
                UpdateCurrentPresentationInfo();
                return true;
            }

            Disconnect();

            var app = IcccePptRotConnectionHelper.TryConnectViaRot(IsSupportWps);
            if (app == null || !IsComApplicationAlive(app))
            {
                IcccePptRotConnectionHelper.SafeReleaseComObject(app);
                return false;
            }

            _application = app;
            _cachedIsConnected = true;
            ConnectionKind = DetectConnectionKind(app);
            RefreshIsInSlideShowFromCom();
            UpdateCurrentPresentationInfo();
            return true;
        }
    }

    public PptSlideShowState GetState()
    {
        if (!EnsureConnected())
        {
            return PptSlideShowState.Disconnected("未检测到正在运行的 PowerPoint 或 WPS 演示。");
        }

        var currentSlide = GetCurrentSlideNumber();
        if (currentSlide > 0)
        {
            _lastKnownSlideNumber = currentSlide;
        }
        else if (IsInSlideShow && _lastKnownSlideNumber > 0)
        {
            currentSlide = _lastKnownSlideNumber;
        }

        return new PptSlideShowState
        {
            IsConnected = true,
            IsInSlideShow = IsInSlideShow,
            CurrentSlide = currentSlide,
            TotalSlides = SlidesCount,
            PresentationName = GetPresentationName(),
            PresentationPath = GetPresentationPath(),
            ConnectionKind = ConnectionKind,
            StatusMessage = IsInSlideShow ? "PPT 放映中。" : "已连接，可以开始放映。"
        };
    }

    public bool TryStartSlideShow()
    {
        try
        {
            if (!EnsureConnected() || _application == null)
            {
                return false;
            }

            object? presentation = GetCurrentActivePresentation();
            if (presentation == null)
            {
                return false;
            }

            dynamic presentationObject = presentation;
            presentationObject.SlideShowSettings.Run();
            RefreshIsInSlideShowFromCom();
            return true;
        }
        catch (COMException ex)
        {
            HandleCommandComException(ex, "开始幻灯片放映失败");
            return false;
        }
        catch (Exception ex)
        {
            AppLoggerService.Info("ppt", $"开始幻灯片放映失败: {ex}");
            return false;
        }
    }

    public bool TryEndSlideShow()
    {
        object? slideShowWindows = null;
        object? slideShowWindow = null;
        object? view = null;

        try
        {
            if (!EnsureConnected() || !IsInSlideShow || _application == null)
            {
                return false;
            }

            dynamic app = _application;
            slideShowWindows = app.SlideShowWindows;
            dynamic windows = slideShowWindows;
            if (windows.Count <= 0)
            {
                return false;
            }

            slideShowWindow = windows[1];
            dynamic window = slideShowWindow;
            view = window.View;
            dynamic viewObject = view;
            viewObject.Exit();
            RefreshIsInSlideShowFromCom();
            return true;
        }
        catch (COMException ex)
        {
            HandleCommandComException(ex, "结束幻灯片放映失败");
            return false;
        }
        catch (Exception ex)
        {
            AppLoggerService.Info("ppt", $"结束幻灯片放映失败: {ex}");
            return false;
        }
        finally
        {
            IcccePptRotConnectionHelper.SafeReleaseComObject(view);
            IcccePptRotConnectionHelper.SafeReleaseComObject(slideShowWindow);
            IcccePptRotConnectionHelper.SafeReleaseComObject(slideShowWindows);
        }
    }

    public bool TryNavigateToSlide(int slideNumber)
    {
        if (slideNumber <= 0)
        {
            return false;
        }

        object? slideShowWindows = null;
        object? slideShowWindow = null;
        object? view = null;
        object? windows = null;
        object? window = null;
        object? windowView = null;

        try
        {
            if (!EnsureConnected() || _application == null)
            {
                return false;
            }

            if (IsInSlideShow)
            {
                dynamic app = _application;
                slideShowWindows = app.SlideShowWindows;
                dynamic ssw = slideShowWindows;
                if (ssw.Count >= 1)
                {
                    slideShowWindow = ssw[1];
                    dynamic sswObject = slideShowWindow;
                    view = sswObject.View;
                    dynamic viewObject = view;
                    viewObject.GotoSlide(slideNumber);
                    return true;
                }
            }
            else if (_currentPresentation != null)
            {
                dynamic presentation = _currentPresentation;
                windows = presentation.Windows;
                dynamic presentationWindows = windows;
                if (presentationWindows.Count >= 1)
                {
                    window = presentationWindows[1];
                    dynamic windowObject = window;
                    windowView = windowObject.View;
                    dynamic viewObject = windowView;
                    viewObject.GotoSlide(slideNumber);
                    return true;
                }
            }

            return false;
        }
        catch (COMException ex)
        {
            HandleCommandComException(ex, $"跳转到幻灯片 {slideNumber} 失败");
            return false;
        }
        catch (Exception ex)
        {
            AppLoggerService.Info("ppt", $"跳转到幻灯片 {slideNumber} 失败: {ex}");
            return false;
        }
        finally
        {
            IcccePptRotConnectionHelper.SafeReleaseComObject(windowView);
            IcccePptRotConnectionHelper.SafeReleaseComObject(window);
            IcccePptRotConnectionHelper.SafeReleaseComObject(windows);
            IcccePptRotConnectionHelper.SafeReleaseComObject(view);
            IcccePptRotConnectionHelper.SafeReleaseComObject(slideShowWindow);
            IcccePptRotConnectionHelper.SafeReleaseComObject(slideShowWindows);
        }
    }

    public bool TryNavigateNext() => TryRunSlideShowNavigationAsync("下一页", view => view.Next());

    public bool TryNavigatePrevious() => TryRunSlideShowNavigationAsync("上一页", view => view.Previous());

    public IReadOnlyList<PptSlideThumbnail> ExportSlideThumbnails(int width, int height)
    {
        var result = new List<PptSlideThumbnail>();
        object? presentation = null;
        object? slides = null;
        object? slide = null;
        var tempDirectory = string.Empty;

        try
        {
            if (!EnsureConnected())
            {
                return result;
            }

            presentation = _currentPresentation ?? GetCurrentActivePresentation();
            if (presentation == null)
            {
                return result;
            }

            dynamic presentationObject = presentation;
            slides = presentationObject.Slides;
            dynamic slidesObject = slides;
            var count = Convert.ToInt32(slidesObject.Count);
            if (count <= 0)
            {
                return result;
            }

            tempDirectory = Path.Combine(Path.GetTempPath(), "CWSTools", "PPTPreviews", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            for (var index = 1; index <= count; index++)
            {
                try
                {
                    slide = slidesObject[index];
                    dynamic slideObject = slide;
                    var imagePath = Path.Combine(tempDirectory, $"slide_{index:0000}.png");
                    slideObject.Export(imagePath, "PNG", width, height);
                    result.Add(new PptSlideThumbnail
                    {
                        SlideNumber = index,
                        PngBytes = File.ReadAllBytes(imagePath)
                    });
                }
                catch (Exception ex)
                {
                    AppLoggerService.Info("ppt", $"生成 PPT 第 {index} 页缩略图失败: {ex.Message}");
                }
                finally
                {
                    IcccePptRotConnectionHelper.SafeReleaseComObject(slide);
                    slide = null;
                }
            }
        }
        catch (Exception ex)
        {
            AppLoggerService.Info("ppt", $"导出 PPT 缩略图失败: {ex}");
        }
        finally
        {
            IcccePptRotConnectionHelper.SafeReleaseComObject(slide);
            if (!ReferenceEquals(slides, _currentSlides))
            {
                IcccePptRotConnectionHelper.SafeReleaseComObject(slides);
            }

            if (!ReferenceEquals(presentation, _currentPresentation))
            {
                IcccePptRotConnectionHelper.SafeReleaseComObject(presentation);
            }

            if (!string.IsNullOrWhiteSpace(tempDirectory) && Directory.Exists(tempDirectory))
            {
                try
                {
                    Directory.Delete(tempDirectory, true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }

        return result;
    }

    public int GetCurrentSlideNumber()
    {
        object? slideShowWindows = null;
        object? slideShowWindow = null;
        object? view = null;
        object? activeWindow = null;
        object? selection = null;
        object? slideRange = null;

        try
        {
            if (!EnsureConnected() || _application == null)
            {
                return 0;
            }

            dynamic app = _application;
            if (IsInSlideShow)
            {
                slideShowWindows = app.SlideShowWindows;
                dynamic ssw = slideShowWindows;
                if (ssw.Count > 0)
                {
                    slideShowWindow = ssw[1];
                    dynamic window = slideShowWindow;
                    view = window.View;
                    dynamic viewObject = view;
                    return Convert.ToInt32(viewObject.CurrentShowPosition);
                }
            }

            activeWindow = app.ActiveWindow;
            if (activeWindow != null)
            {
                dynamic activeWindowObject = activeWindow;
                selection = activeWindowObject.Selection;
                dynamic selectionObject = selection;
                slideRange = selectionObject.SlideRange;
                dynamic slideRangeObject = slideRange;
                var slideNumber = Convert.ToInt32(slideRangeObject.SlideNumber);
                if (slideNumber > 0)
                {
                    return slideNumber;
                }
            }

            if (_currentSlide != null)
            {
                dynamic slide = _currentSlide;
                return Convert.ToInt32(slide.SlideNumber);
            }
        }
        catch (COMException ex)
        {
            HandleStateComException(ex);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        finally
        {
            IcccePptRotConnectionHelper.SafeReleaseComObject(slideRange);
            IcccePptRotConnectionHelper.SafeReleaseComObject(selection);
            IcccePptRotConnectionHelper.SafeReleaseComObject(view);
            IcccePptRotConnectionHelper.SafeReleaseComObject(slideShowWindow);
            IcccePptRotConnectionHelper.SafeReleaseComObject(slideShowWindows);
            IcccePptRotConnectionHelper.SafeReleaseComObject(activeWindow);
        }

        return 0;
    }

    public string GetPresentationName() => TryReadPresentationString("Name");

    public string GetPresentationPath() => TryReadPresentationString("FullName");

    public object? GetCurrentActivePresentation()
    {
        object? slideShowWindow = null;
        object? view = null;
        object? slide = null;

        try
        {
            if (!EnsureConnected() || _application == null)
            {
                return null;
            }

            dynamic app = _application;
            if (IsInSlideShow && Convert.ToInt32(app.SlideShowWindows.Count) > 0)
            {
                slideShowWindow = app.SlideShowWindows[1];
                dynamic window = slideShowWindow;
                view = window.View;
                dynamic viewObject = view;
                slide = viewObject.Slide;
                dynamic slideObject = slide;
                return slideObject.Parent;
            }

            try
            {
                return app.ActiveWindow.Presentation;
            }
            catch (COMException ex) when (IsNoActivePresentationHResult((uint)ex.HResult) || IsPptBusyHResult((uint)ex.HResult))
            {
                return _currentPresentation;
            }
        }
        catch (COMException ex)
        {
            HandleStateComException(ex);
            return _currentPresentation;
        }
        catch (Exception ex)
        {
            AppLoggerService.Info("ppt", $"获取当前活跃演示文稿失败: {ex}");
            return _currentPresentation;
        }
        finally
        {
            IcccePptRotConnectionHelper.SafeReleaseComObject(slide);
            IcccePptRotConnectionHelper.SafeReleaseComObject(view);
            IcccePptRotConnectionHelper.SafeReleaseComObject(slideShowWindow);
        }
    }

    private bool TryRunSlideShowNavigationAsync(string direction, Action<dynamic> navigate)
    {
        try
        {
            if (!EnsureConnected() || !IsInSlideShow || _application == null)
            {
                return false;
            }

            var thread = new Thread(() => RunSlideShowNavigation(direction, navigate))
            {
                IsBackground = true,
                Name = $"CWS PPT {direction}"
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return true;
        }
        catch (Exception ex)
        {
            AppLoggerService.Info("ppt", $"启动{direction}线程失败: {ex}");
            return false;
        }
    }

    private void RunSlideShowNavigation(string direction, Action<dynamic> navigate)
    {
        object? slideShowWindows = null;
        object? slideShowWindow = null;
        object? view = null;

        try
        {
            if (_application == null)
            {
                return;
            }

            dynamic app = _application;
            slideShowWindows = app.SlideShowWindows;
            dynamic windows = slideShowWindows;
            if (windows.Count <= 0)
            {
                return;
            }

            slideShowWindow = windows[1];
            dynamic window = slideShowWindow;
            try
            {
                window.Activate();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            view = window.View;
            navigate((dynamic)view);
        }
        catch (COMException ex)
        {
            HandleCommandComException(ex, $"切换到{direction}失败");
        }
        catch (Exception ex)
        {
            AppLoggerService.Info("ppt", $"切换到{direction}失败: {ex}");
        }
        finally
        {
            IcccePptRotConnectionHelper.SafeReleaseComObject(view);
            IcccePptRotConnectionHelper.SafeReleaseComObject(slideShowWindow);
            IcccePptRotConnectionHelper.SafeReleaseComObject(slideShowWindows);
        }
    }

    private bool RefreshIsInSlideShowFromCom()
    {
        object? slideShowWindows = null;
        object? slideShowWindow = null;
        object? view = null;

        try
        {
            if (_application == null)
            {
                _cachedIsInSlideShow = false;
                return false;
            }

            dynamic app = _application;
            slideShowWindows = app.SlideShowWindows;
            dynamic windows = slideShowWindows;
            if (windows.Count == 0)
            {
                _cachedIsInSlideShow = false;
                return false;
            }

            slideShowWindow = windows[1];
            dynamic window = slideShowWindow;
            view = window.View;
            _cachedIsInSlideShow = view != null;
            return _cachedIsInSlideShow;
        }
        catch (COMException ex)
        {
            _cachedIsInSlideShow = false;
            HandleStateComException(ex);
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            _cachedIsInSlideShow = false;
            return false;
        }
        finally
        {
            IcccePptRotConnectionHelper.SafeReleaseComObject(view);
            IcccePptRotConnectionHelper.SafeReleaseComObject(slideShowWindow);
            IcccePptRotConnectionHelper.SafeReleaseComObject(slideShowWindows);
        }
    }

    private void UpdateCurrentPresentationInfo()
    {
        object? activePresentation = null;
        object? slideShowWindows = null;
        object? slideShowWindow = null;
        object? activeWindow = null;
        object? view = null;
        object? selection = null;
        object? slideRange = null;

        try
        {
            if (_application == null)
            {
                ClearPresentationState();
                return;
            }

            dynamic app = _application;
            activePresentation = app.ActivePresentation;
            if (activePresentation == null)
            {
                ClearPresentationState();
                return;
            }

            SafeReleasePresentationState();
            _currentPresentation = activePresentation;
            activePresentation = null;

            dynamic presentation = _currentPresentation;
            _currentSlides = presentation.Slides;
            dynamic slides = _currentSlides;
            SlidesCount = Convert.ToInt32(slides.Count);

            if (IsInSlideShow)
            {
                slideShowWindows = app.SlideShowWindows;
                dynamic windows = slideShowWindows;
                if (windows.Count > 0)
                {
                    slideShowWindow = windows[1];
                    dynamic window = slideShowWindow;
                    view = window.View;
                    dynamic viewObject = view;
                    _currentSlide = viewObject.Slide;
                }
            }
            else
            {
                activeWindow = app.ActiveWindow;
                if (activeWindow != null)
                {
                    dynamic activeWindowObject = activeWindow;
                    selection = activeWindowObject.Selection;
                    dynamic selectionObject = selection;
                    slideRange = selectionObject.SlideRange;
                    dynamic slideRangeObject = slideRange;
                    var slideNumber = Convert.ToInt32(slideRangeObject.SlideNumber);
                    if (slideNumber > 0 && slideNumber <= SlidesCount)
                    {
                        _currentSlide = slides[slideNumber];
                    }
                }

                if (_currentSlide == null && SlidesCount > 0)
                {
                    _currentSlide = slides[1];
                }
            }
        }
        catch (COMException ex)
        {
            if (!IsNoActivePresentationHResult((uint)ex.HResult) && !IsPptBusyHResult((uint)ex.HResult))
            {
                AppLoggerService.Info("ppt", $"更新演示文稿信息失败: {ex.Message}");
            }

            if (IsComDisconnectedHResult((uint)ex.HResult))
            {
                Disconnect();
            }
            else
            {
                ClearPresentationState();
            }
        }
        catch (Exception ex)
        {
            AppLoggerService.Info("ppt", $"更新演示文稿信息失败: {ex}");
            ClearPresentationState();
        }
        finally
        {
            IcccePptRotConnectionHelper.SafeReleaseComObject(slideRange);
            IcccePptRotConnectionHelper.SafeReleaseComObject(selection);
            IcccePptRotConnectionHelper.SafeReleaseComObject(view);
            IcccePptRotConnectionHelper.SafeReleaseComObject(slideShowWindow);
            IcccePptRotConnectionHelper.SafeReleaseComObject(activeWindow);
            IcccePptRotConnectionHelper.SafeReleaseComObject(slideShowWindows);
            IcccePptRotConnectionHelper.SafeReleaseComObject(activePresentation);
        }
    }

    private string TryReadPresentationString(string propertyName)
    {
        try
        {
            if (!EnsureConnected() || _currentPresentation == null)
            {
                return string.Empty;
            }

            return Convert.ToString(_currentPresentation.GetType().InvokeMember(
                propertyName,
                System.Reflection.BindingFlags.GetProperty,
                null,
                _currentPresentation,
                null)) ?? string.Empty;
        }
        catch (COMException ex)
        {
            HandleStateComException(ex);
            return string.Empty;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return string.Empty;
        }
    }

    private static bool IsComApplicationAlive(object application)
    {
        try
        {
            if (!Marshal.IsComObject(application))
            {
                return false;
            }

            dynamic app = application;
            _ = app.Name;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string DetectConnectionKind(object app)
    {
        try
        {
            dynamic application = app;
            var name = Convert.ToString(application.Name) ?? string.Empty;
            var path = Convert.ToString(application.Path) ?? string.Empty;
            if (name.Contains("WPS", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("WPP", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("Kingsoft", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("WPS Office", StringComparison.OrdinalIgnoreCase))
            {
                return "WPS Presentation";
            }
        }
        catch
        {
        }

        return "PowerPoint";
    }

    private void HandleCommandComException(COMException exception, string message)
    {
        var hr = (uint)exception.HResult;
        if (!IsPptBusyHResult(hr) && IsComDisconnectedHResult(hr))
        {
            Disconnect();
        }

        AppLoggerService.Info("ppt", $"{message}: {exception.Message} (HRESULT: 0x{hr:X8})");
    }

    private void HandleStateComException(COMException exception)
    {
        var hr = (uint)exception.HResult;
        if (!IsPptBusyHResult(hr) && IsComDisconnectedHResult(hr))
        {
            Disconnect();
        }
    }

    private static bool IsPptBusyHResult(uint hr) => hr is 0x80010001 or 0x8001010A;

    private static bool IsNoActivePresentationHResult(uint hr) => hr == 0x80048240;

    private static bool IsComDisconnectedHResult(uint hr) => hr is 0x8001010E or 0x80004005 or 0x800706B5;

    private void Disconnect()
    {
        SafeReleasePresentationState();
        IcccePptRotConnectionHelper.SafeReleaseComObject(_application);
        _application = null;
        _cachedIsConnected = false;
        _cachedIsInSlideShow = false;
        SlidesCount = 0;
        ConnectionKind = string.Empty;
        _lastKnownSlideNumber = 0;
    }

    private void ClearPresentationState()
    {
        SafeReleasePresentationState();
        SlidesCount = 0;
    }

    private void SafeReleasePresentationState()
    {
        IcccePptRotConnectionHelper.SafeReleaseComObject(_currentSlide);
        IcccePptRotConnectionHelper.SafeReleaseComObject(_currentSlides);
        IcccePptRotConnectionHelper.SafeReleaseComObject(_currentPresentation);
        _currentSlide = null;
        _currentSlides = null;
        _currentPresentation = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Disconnect();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
