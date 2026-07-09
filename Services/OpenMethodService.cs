using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using Gallery.Models;

namespace Gallery.Services;

public sealed class OpenMethodService
{
    private static readonly IReadOnlyList<OpenMethodDefinition> Definitions =
    [
        new("powerpoint", "OM_PowerPoint", ".ppt / .pptx / .pps", "OM_PowerPointDescription"),
        new("word", "OM_Word", ".doc / .docx", "OM_WordDescription"),
        new("excel", "OM_Excel", ".xls / .xlsx", "OM_ExcelDescription"),
        new("pdf", "OM_Pdf", ".pdf", "OM_PdfDescription")
    ];

    private static readonly IReadOnlyDictionary<string, string[]> ExtensionMap = new Dictionary<string, string[]>
    {
        ["powerpoint"] = [".ppt", ".pptx", ".pptm", ".pps", ".ppsx", ".ppsm", ".pot", ".potx", ".potm", ".dps", ".dpt"],
        ["word"] = [".doc", ".docx"],
        ["excel"] = [".xls", ".xlsx"],
        ["pdf"] = [".pdf"]
    };

    public IReadOnlyList<OpenMethodDefinition> GetDefinitions() => Definitions;

    public string[] GetExtensions(string entryKey)
    {
        return ExtensionMap.TryGetValue(entryKey, out var extensions) ? extensions : [];
    }

    public string? GetEntryKeyForExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return null;
        }

        return ExtensionMap.FirstOrDefault(
            pair => pair.Value.Contains(extension, StringComparer.OrdinalIgnoreCase)).Key;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public IReadOnlyDictionary<string, string> DetectCurrentTargets()
    {
        var detected = new Dictionary<string, string>();

        foreach (var definition in Definitions)
        {
            var extensions = GetExtensions(definition.Key);
            var targets = extensions
                .Select(GetTargetForExtension)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            detected[definition.Key] = targets.Length switch
            {
                0 => OpenMethodTargets.Unknown,
                1 => targets[0],
                _ => OpenMethodTargets.Mixed
            };
        }

        return detected;
    }

    public IReadOnlyList<OpenMethodAssociationPlanItem> BuildAssociationPlan(
        IReadOnlyDictionary<string, string> preferences)
    {
        var plan = new List<OpenMethodAssociationPlanItem>();

        foreach (var definition in Definitions)
        {
            if (!preferences.TryGetValue(definition.Key, out var target))
            {
                target = OpenMethodTargets.System;
            }

            if (!ExtensionMap.TryGetValue(definition.Key, out var extensions))
            {
                continue;
            }

            foreach (var extension in extensions)
            {
                plan.Add(new OpenMethodAssociationPlanItem(definition.Key, extension, target));
            }
        }

        return plan;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public OpenMethodSaveResult SaveSelection(string entryKey, string target)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new OpenMethodSaveResult(OpenMethodSaveStatus.UnsupportedPlatform);
        }

        if (target == OpenMethodTargets.System)
        {
            return new OpenMethodSaveResult(OpenMethodSaveStatus.SavedToSystem, null, GetExtensions(entryKey));
        }

        var appInfo = ResolveTargetAppInfo(entryKey, target);
        if (appInfo is null)
        {
            return new OpenMethodSaveResult(OpenMethodSaveStatus.SavedWithoutDetectedApp, target, GetExtensions(entryKey));
        }

        return new OpenMethodSaveResult(
            OpenMethodSaveStatus.Saved,
            appInfo.DisplayName,
            GetExtensions(entryKey));
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static string GetTargetForExtension(string extension)
    {
        var progId = GetUserChoiceProgId(extension) ?? GetClassRootProgId(extension);
        if (string.IsNullOrWhiteSpace(progId))
        {
            return OpenMethodTargets.Unknown;
        }

        var normalized = progId.ToLowerInvariant();

        if (normalized.Contains("wps") || normalized.Contains("kingsoft") || normalized.Contains("et.") || normalized.Contains("wpp.") || normalized.Contains("writer"))
        {
            return OpenMethodTargets.Wps;
        }

        if (normalized.Contains("word") ||
            normalized.Contains("winword") ||
            normalized.Contains("excel") ||
            normalized.Contains("powerpoint") ||
            normalized.Contains("powerpnt") ||
            normalized.Contains("office") ||
            normalized.Contains("acrobat") ||
            normalized.Contains("microsoft"))
        {
            return OpenMethodTargets.Office;
        }

        return OpenMethodTargets.System;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static string? GetUserChoiceProgId(string extension)
    {
        using var userChoice = Registry.CurrentUser.OpenSubKey(
            $@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{extension}\UserChoice");
        return userChoice?.GetValue("ProgId") as string;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static string? GetClassRootProgId(string extension)
    {
        using var classRoot = Registry.ClassesRoot.OpenSubKey(extension);
        return classRoot?.GetValue(string.Empty) as string;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public OpenMethodAppInfo? ResolveTargetAppInfo(string entryKey, string target)
    {
        return (entryKey, target) switch
        {
            ("word", OpenMethodTargets.Office) => FindApp("Microsoft Word", "WINWORD.EXE"),
            ("excel", OpenMethodTargets.Office) => FindApp("Microsoft Excel", "EXCEL.EXE"),
            ("powerpoint", OpenMethodTargets.Office) => FindApp("Microsoft PowerPoint", "POWERPNT.EXE"),
            ("pdf", OpenMethodTargets.Office) => FindApp("Microsoft Word", "WINWORD.EXE"),

            ("word", OpenMethodTargets.Wps) => FindApp("WPS Writer", "wps.exe"),
            ("excel", OpenMethodTargets.Wps) => FindApp("WPS Spreadsheets", "et.exe"),
            ("powerpoint", OpenMethodTargets.Wps) => FindApp("WPS Presentation", "wpp.exe"),
            ("pdf", OpenMethodTargets.Wps) => FindApp("WPS PDF", "wpspdf.exe"),
            _ => null
        };
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static OpenMethodAppInfo? FindApp(string displayName, string exeName)
    {
        var path = FindAppPathFromRegistry(exeName) ?? FindAppPathFromCommonFolders(exeName);
        return path is null ? null : new OpenMethodAppInfo(displayName, path);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static string? FindAppPathFromRegistry(string exeName)
    {
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            using var appPaths = baseKey.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{exeName}");
            var value = appPaths?.GetValue(string.Empty) as string;
            if (!string.IsNullOrWhiteSpace(value) && File.Exists(value))
            {
                return value;
            }
        }

        return null;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static string? FindAppPathFromCommonFolders(string exeName)
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Office"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Office"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WPS Office"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "WPS Office"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Kingsoft"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Kingsoft")
        };

        foreach (var root in candidates.Where(Directory.Exists))
        {
            try
            {
                var match = Directory.EnumerateFiles(root, exeName, SearchOption.AllDirectories).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(match))
                {
                    return match;
                }
            }
            catch
            {
            }
        }

        return null;
    }
}

public sealed record OpenMethodDefinition(
    string Key,
    string TitleResourceKey,
    string Extensions,
    string DescriptionResourceKey);

public sealed record OpenMethodAssociationPlanItem(
    string EntryKey,
    string Extension,
    string Target);

public sealed record OpenMethodSaveResult(
    OpenMethodSaveStatus Status,
    string? TargetAppDisplayName = null,
    IReadOnlyList<string>? Extensions = null);

public enum OpenMethodSaveStatus
{
    Saved,
    SavedToSystem,
    SavedWithoutDetectedApp,
    UnsupportedPlatform
}

public sealed record OpenMethodAppInfo(string DisplayName, string ExecutablePath);
