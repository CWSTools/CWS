using System;
using System.Collections.Generic;
using System.IO;
using Gallery.Models;
using Gallery.Services;

namespace Gallery.Services.Ppt;

public sealed class WindowsPptSlideShowService : IPptSlideShowService
{
    private readonly AppConfig _config;
    private readonly FileOpenRouterService _fileOpenRouterService = new();
    private readonly IcccePptManager _pptManager = new();

    public WindowsPptSlideShowService(AppConfig? config = null)
    {
        _config = config ?? RuntimeConfigService.Current;
    }

    public PptSlideShowState RefreshState()
    {
        if (!OperatingSystem.IsWindows())
        {
            return PptSlideShowState.Disconnected("当前平台不支持 PowerPoint COM 控制。");
        }

        if (!IsPowerPointSupportEnabled())
        {
            return PptSlideShowState.Disconnected("PPT 放映控制已在设置中关闭。");
        }

        try
        {
            _pptManager.IsSupportWps = _config.Iccce.Ppt.SupportWps;
            return _pptManager.GetState();
        }
        catch (Exception ex)
        {
            return PptSlideShowState.Disconnected($"读取 PPT 状态失败：{ex.Message}");
        }
    }

    public FileOpenResult OpenPresentation(string filePath, IReadOnlyDictionary<string, string>? preferences = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new FileOpenResult(FileOpenStatus.UnsupportedPlatform);
        }

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return new FileOpenResult(FileOpenStatus.FileNotFound);
        }

        return _fileOpenRouterService.OpenFile(filePath, preferences);
    }

    public bool StartSlideShow() => IsPowerPointSupportEnabled() && _pptManager.TryStartSlideShow();

    public bool EndSlideShow() => IsPowerPointSupportEnabled() && _pptManager.TryEndSlideShow();

    public bool Next() => IsPowerPointSupportEnabled() && _pptManager.TryNavigateNext();

    public bool Previous() => IsPowerPointSupportEnabled() && _pptManager.TryNavigatePrevious();

    public bool GoToSlide(int slideNumber) => IsPowerPointSupportEnabled() && _pptManager.TryNavigateToSlide(slideNumber);

    public IReadOnlyList<PptSlideThumbnail> ExportSlideThumbnails(int width, int height) =>
        IsPowerPointSupportEnabled() && _config.Iccce.Ppt.EnhancedPreview
            ? _pptManager.ExportSlideThumbnails(width, height)
            : [];

    public void Dispose() => _pptManager.Dispose();

    private bool IsPowerPointSupportEnabled() => _config.Iccce.IsEnabled && _config.Iccce.Ppt.PowerPointSupport;
}
