using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Gallery.Models;

public class AppConfig
{
    public string Theme { get; set; } = "";
    public bool IsCustomAccentColor { get; set; }
    public string CustomAccentColor { get; set; } = "";
    public bool IsWindowEffectEnabled { get; set; }
    public string WindowEffect { get; set; } = "";
    public bool IsEnabledBackgroundImage { get; set; }
    public string Language { get; set; } = "";
    public bool ReleaseResourcesOnMinimize { get; set; } = true;
    public bool HideToTrayAfterMinimizeDelay { get; set; } = true;
    public bool IsBehaviorLoggingEnabled { get; set; }
    public bool IsLaunchAtStartupEnabled { get; set; }
    public Dictionary<string, string> OpenMethodPreferences { get; set; } = [];
    public CwsIccceSettings Iccce { get; set; } = new();
}

public class CwsIccceSettings
{
    public bool IsEnabled { get; set; } = true;
    public string IccceExecutablePath { get; set; } = string.Empty;
    public CwsIcccePptSettings Ppt { get; set; } = new();
    public CwsIccceCanvasSettings Canvas { get; set; } = new();
    public CwsIccceGestureSettings Gesture { get; set; } = new();
    public CwsIccceAutomationSettings Automation { get; set; } = new();
    public CwsIccceToolbarSettings Toolbar { get; set; } = new();
    public CwsIccceMiniWhiteboardSettings MiniWhiteboard { get; set; } = new();
    public CwsIccceNotificationSettings Notification { get; set; } = new();
    public CwsIcccePerformanceSettings Performance { get; set; } = new();
    public CwsIccceSecuritySettings Security { get; set; } = new();
    public CwsIccceAdvancedSettings Advanced { get; set; } = new();
}

public class CwsIcccePptSettings
{
    public bool PowerPointSupport { get; set; } = true;
    public bool SupportWps { get; set; } = true;
    public bool UseRotConnection { get; set; } = true;
    public bool AutoShowFloatingWindow { get; set; } = true;
    public bool AutoCloseIccceAfterSlideShow { get; set; }
    public bool KeepFloatingWindowTopmost { get; set; } = true;
    public bool ShowPageNumber { get; set; } = true;
    public bool PageButtonClickable { get; set; } = true;
    public bool EnhancedPreview { get; set; } = true;
    public bool PreloadPreview { get; set; }
    public bool LongPressPageTurn { get; set; } = true;
    public bool OptimisticPageNumber { get; set; } = true;
    public int OptimisticPageHoldMilliseconds { get; set; } = 1800;
    public bool SkipAnimationsWhenGoNext { get; set; }
    public bool EnterAnnotationOnShow { get; set; }
    public bool AutoSaveStrokes { get; set; } = true;
    public bool AutoScreenshot { get; set; }
    public bool NotifyHiddenPage { get; set; } = true;
    public bool NotifyAutoPlay { get; set; } = true;
    public bool RememberLastPage { get; set; }
    public bool GoToFirstPageOnReenter { get; set; }
    public bool FingerGestureSlide { get; set; } = true;
    public bool TwoFingerGesture { get; set; }
    public bool ShowQuickPanelInShow { get; set; }
    public bool ShowGestureButtonInShow { get; set; }
    public bool TimeCapsule { get; set; } = true;
    public int TimeCapsulePosition { get; set; } = 1;
    public double TimeCapsuleOpacity { get; set; } = 1.0;
    public double TimeCapsuleScale { get; set; } = 1.0;
    public double FloatingWindowScale { get; set; } = 1.0;
    public double FloatingWindowOpacity { get; set; } = 1.0;
    public string FloatingWindowPosition { get; set; } = "RightBottom";
}

public class CwsIccceCanvasSettings
{
    public double InkWidth { get; set; } = 2.5;
    public double HighlighterWidth { get; set; } = 20;
    public double InkAlpha { get; set; } = 255;
    public double HighlighterAlpha { get; set; } = 255;
    public bool ShowCursor { get; set; }
    public int PenCursorType { get; set; } = 1;
    public int InkStyle { get; set; }
    public int EraserSize { get; set; } = 2;
    public int EraserType { get; set; }
    public bool HideStrokeWhenSelecting { get; set; } = true;
    public bool UseAdvancedBezierSmoothing { get; set; } = true;
    public bool UseAsyncInkSmoothing { get; set; } = true;
    public bool UseHardwareAcceleration { get; set; } = true;
    public int InkSmoothingQuality { get; set; } = 2;
    public bool AutoStraightenLine { get; set; } = true;
    public int AutoStraightenLineThreshold { get; set; } = 80;
    public bool EnablePalmEraser { get; set; } = true;
    public int PalmEraserSensitivity { get; set; }
    public bool ClearCanvasAlsoClearImages { get; set; } = true;
    public bool EnableInkFade { get; set; }
    public int InkFadeTime { get; set; } = 3000;
    public double LaserPenWidth { get; set; } = 5;
    public int LaserPenAlpha { get; set; } = 128;
    public bool EnableBrushAutoRestore { get; set; }
    public int BrushAutoRestoreDelaySeconds { get; set; } = 30;
    public string BrushAutoRestoreColor { get; set; } = "#FFFF0000";
    public double BrushAutoRestoreWidth { get; set; } = 5;
    public int BrushAutoRestoreAlpha { get; set; } = 255;
    public bool EnableEraserAutoSwitchBack { get; set; }
    public int EraserAutoSwitchBackDelaySeconds { get; set; } = 10;
    public string CustomBackgroundColor { get; set; } = "#162924";
}

public class CwsIccceGestureSettings
{
    public bool EnableMultiTouchMode { get; set; }
    public bool EnableTwoFingerZoom { get; set; } = true;
    public bool EnableTwoFingerTranslate { get; set; } = true;
    public bool EnableTwoFingerRotation { get; set; }
    public bool EnableMultiTouchModeOnWhiteboard { get; set; }
    public bool EnableTwoFingerZoomOnWhiteboard { get; set; } = true;
    public bool EnableTwoFingerTranslateOnWhiteboard { get; set; } = true;
    public bool EnableTwoFingerRotationOnWhiteboard { get; set; }
    public bool EnableFingerGestureSlideShowControl { get; set; } = true;
}

public class CwsIccceAutomationSettings
{
    public bool AutoEnterAnnotationModeWhenExitFoldMode { get; set; }
    public bool AutoFoldWhenExitWhiteboard { get; set; }
    public bool AutoFoldInPptSlideShow { get; set; }
    public bool AutoFoldAfterPptSlideShow { get; set; }
    public bool AutoSaveStrokes { get; set; } = true;
    public int AutoSaveStrokesIntervalMinutes { get; set; } = 5;
    public bool AutoSaveStrokesAtScreenshot { get; set; }
    public bool AutoScreenshotAtClear { get; set; }
    public bool SaveScreenshotsInDateFolders { get; set; }
    public bool AutoClearWhenExitingWritingMode { get; set; }
    public int MinimumAutomationStrokeNumber { get; set; }
    public bool KeepFoldAfterSoftwareExit { get; set; }
    public bool ThoroughlyHideWhenFolded { get; set; }
    public CwsIccceFloatingInterceptorSettings FloatingWindowInterceptor { get; set; } = new();
}

public class CwsIccceFloatingInterceptorSettings
{
    public bool IsEnabled { get; set; }
    public int ScanIntervalMs { get; set; } = 5000;
    public bool AutoStart { get; set; }
    public bool ShowNotifications { get; set; } = true;
    public Dictionary<string, bool> InterceptRules { get; set; } = [];
}

public class CwsIccceToolbarSettings
{
    public double FloatingBarScale { get; set; } = 1.0;
    public bool ColorfulFloatingBar { get; set; }
    public int ToolbarPosition { get; set; }
    public bool AutoCollapseQuickPanel { get; set; } = true;
    public int AutoCollapseQuickPanelDelayMs { get; set; } = 1500;
    public bool ShowPptButton { get; set; } = true;
    public bool ShowWhiteboardButton { get; set; } = true;
}

public class CwsIccceMiniWhiteboardSettings
{
    public bool IsEnabled { get; set; } = true;
    public double DefaultWidth { get; set; } = 400;
    public double DefaultHeight { get; set; } = 300;
    public double DefaultOpacity { get; set; } = 0.95;
    public string BackgroundColor { get; set; } = "#FF2A2A2A";
    public bool SyncWithPptPages { get; set; } = true;
    public double PenWidth { get; set; } = 3;
    public string PenColor { get; set; } = "#FFFFFFFF";
    public int CurrentColorIndex { get; set; }
}

public class CwsIccceNotificationSettings
{
    public bool AnnouncementEnabled { get; set; } = true;
    public bool DynamicNotificationEnabled { get; set; } = true;
    public bool WindowsToastEnabled { get; set; }
    public bool DoNotDisturbInPpt { get; set; } = true;
    public bool DoNotDisturbInWhiteboard { get; set; } = true;
}

public class CwsIcccePerformanceSettings
{
    public bool MonitoringEnabled { get; set; }
    public bool HardwareAccelerationEnabled { get; set; } = true;
    public int DeviceScore { get; set; } = -1;
    public int CpuScore { get; set; } = -1;
    public int MemoryScore { get; set; } = -1;
    public int DiskScore { get; set; } = -1;
    public string LastTestTime { get; set; } = string.Empty;
}

public class CwsIccceSecuritySettings
{
    public bool PasswordEnabled { get; set; }
    public string PasswordSalt { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool TotpEnabled { get; set; }
    public bool ProcessProtectionEnabled { get; set; } = true;
    public bool UsbVerificationEnabled { get; set; }
    public string UsbAuthorizedSerialNumbers { get; set; } = string.Empty;
}

public class CwsIccceAdvancedSettings
{
    public bool SpecialScreen { get; set; }
    public bool QuadIr { get; set; }
    public double TouchMultiplier { get; set; } = 1.0;
    public bool UiAutomationEnabled { get; set; } = true;
    public int UiaMode { get; set; }
    public bool EnableDeveloperDiagnostics { get; set; }
}

[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(CwsIccceSettings))]
public partial class ConfigJsonContext : JsonSerializerContext
{
}
