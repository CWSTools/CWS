using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Gallery.Models;

namespace Gallery.Services;

public sealed class FileOpenRouterService
{
    private readonly OpenMethodService _openMethodService = new();
    private readonly FileAssociationRegistrationService _associationRegistrationService = new();

    public FileOpenResult OpenFile(string filePath, IReadOnlyDictionary<string, string>? preferences = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new FileOpenResult(FileOpenStatus.UnsupportedPlatform);
        }

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return new FileOpenResult(FileOpenStatus.FileNotFound);
        }

        var extension = Path.GetExtension(filePath);
        var entryKey = _openMethodService.GetEntryKeyForExtension(extension);
        if (string.IsNullOrWhiteSpace(entryKey))
        {
            return OpenWithSystem(filePath);
        }

        var effectivePreferences = preferences ?? RuntimeConfigService.Current.OpenMethodPreferences;
        var target = effectivePreferences.TryGetValue(entryKey, out var configuredTarget)
            ? NormalizeTarget(configuredTarget)
            : OpenMethodTargets.System;

        if (target == OpenMethodTargets.System)
        {
            if (_associationRegistrationService.IsExtensionAssociatedWithCwsTool(extension))
            {
                return new FileOpenResult(FileOpenStatus.SystemDefaultRoutesToCwsTool, target, null, extension);
            }

            return OpenWithSystem(filePath);
        }

        var appInfo = _openMethodService.ResolveTargetAppInfo(entryKey, target);
        if (appInfo is null)
        {
            return new FileOpenResult(FileOpenStatus.TargetAppNotFound, target, null, extension);
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = appInfo.ExecutablePath,
                Arguments = $"\"{filePath}\"",
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(filePath) ?? AppContext.BaseDirectory
            });
            return new FileOpenResult(FileOpenStatus.OpenedWithTarget, target, appInfo.DisplayName, extension);
        }
        catch (Exception ex)
        {
            return new FileOpenResult(FileOpenStatus.LaunchFailed, target, appInfo.DisplayName, extension, ex.Message);
        }
    }

    private static FileOpenResult OpenWithSystem(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(filePath) ?? AppContext.BaseDirectory
            });
            return new FileOpenResult(FileOpenStatus.OpenedWithSystem, OpenMethodTargets.System, null, Path.GetExtension(filePath));
        }
        catch (Exception ex)
        {
            return new FileOpenResult(FileOpenStatus.LaunchFailed, OpenMethodTargets.System, null, Path.GetExtension(filePath), ex.Message);
        }
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
}

public sealed record FileOpenResult(
    FileOpenStatus Status,
    string? Target = null,
    string? TargetAppDisplayName = null,
    string? Extension = null,
    string? ErrorMessage = null);

public enum FileOpenStatus
{
    OpenedWithSystem,
    OpenedWithTarget,
    TargetAppNotFound,
    FileNotFound,
    LaunchFailed,
    SystemDefaultRoutesToCwsTool,
    UnsupportedPlatform
}
