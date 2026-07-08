using System;
using Microsoft.Win32;

namespace Gallery.Services;

public static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "CWSTool";

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(RunValueName) is string value && !string.IsNullOrWhiteSpace(value);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
        {
            key.SetValue(RunValueName, BuildCommandLine());
            return;
        }

        key.DeleteValue(RunValueName, false);
    }

    private static string BuildCommandLine()
    {
        return $"\"{Environment.ProcessPath ?? AppContext.BaseDirectory}\" --background";
    }
}
