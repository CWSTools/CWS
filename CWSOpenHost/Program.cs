using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace CWSOpenHost;

internal static class Program
{
    private const string IpcPipeName = "CWSTool.Ipc.v1";
    private const string ProtocolScheme = "cwstool";

    [STAThread]
    private static int Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            return 2;
        }

        var config = HostConfig.Load();
        var router = new OpenRouter(config.OpenMethodPreferences);
        var exitCode = 0;
        var handledAny = false;

        foreach (var arg in args.Where(arg => !arg.StartsWith("--", StringComparison.OrdinalIgnoreCase)))
        {
            if (TryHandleProtocol(arg, router, out var protocolHandled))
            {
                handledAny = true;
                if (!protocolHandled)
                {
                    exitCode = 1;
                }

                continue;
            }

            if (File.Exists(arg))
            {
                handledAny = true;
                if (!router.Open(arg))
                {
                    exitCode = 1;
                }
            }
        }

        return handledAny ? exitCode : 0;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static bool TryHandleProtocol(string arg, OpenRouter router, out bool handled)
    {
        handled = false;
        if (!Uri.TryCreate(arg, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, ProtocolScheme, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var action = GetProtocolAction(uri);
        switch (action)
        {
            case "show":
                handled = SendIpcCommand("show") || LaunchMainApp("--show");
                return true;
            case "settings":
                handled = SendIpcCommand("settings") || LaunchMainApp("--settings");
                return true;
            case "open":
                var filePath = GetProtocolFilePath(uri);
                handled = !string.IsNullOrWhiteSpace(filePath) &&
                          File.Exists(filePath) &&
                          router.Open(filePath);
                return true;
            default:
                handled = false;
                return true;
        }
    }

    private static string GetProtocolAction(Uri uri)
    {
        if (!string.IsNullOrWhiteSpace(uri.Host))
        {
            return uri.Host.Trim().ToLowerInvariant();
        }

        return uri.AbsolutePath.Trim('/').ToLowerInvariant();
    }

    private static string? GetProtocolFilePath(Uri uri)
    {
        var queryFile = GetQueryValue(uri, "file");
        if (!string.IsNullOrWhiteSpace(queryFile))
        {
            return queryFile;
        }

        var path = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        return string.IsNullOrWhiteSpace(path) ? null : path;
    }

    private static string? GetQueryValue(Uri uri, string key)
    {
        var query = uri.Query.TrimStart('?');
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var name = UrlDecode(parts[0]);
            if (!string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return parts.Length == 2 ? UrlDecode(parts[1]) : string.Empty;
        }

        return null;
    }

    private static string UrlDecode(string value)
    {
        return Uri.UnescapeDataString(value.Replace("+", " "));
    }

    private static bool SendIpcCommand(string command)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(
                ".",
                IpcPipeName,
                PipeDirection.InOut,
                PipeOptions.CurrentUserOnly);
            pipe.Connect(500);

            using var writer = new StreamWriter(pipe, Encoding.UTF8, leaveOpen: true)
            {
                AutoFlush = true
            };
            writer.WriteLine(command);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool LaunchMainApp(string argument)
    {
        try
        {
            var appPath = Path.Combine(AppContext.BaseDirectory, "CWSTool.exe");
            if (!File.Exists(appPath))
            {
                return false;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = appPath,
                Arguments = argument,
                UseShellExecute = true,
                WorkingDirectory = AppContext.BaseDirectory
            });
            return true;
        }
        catch
        {
            return false;
        }
    }
}

internal sealed class HostConfig
{
    public Dictionary<string, string> OpenMethodPreferences { get; set; } = [];

    public static HostConfig Load()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Config", "config.json");
            if (!File.Exists(path))
            {
                return new HostConfig();
            }

            return JsonSerializer.Deserialize<HostConfig>(File.ReadAllText(path)) ?? new HostConfig();
        }
        catch
        {
            return new HostConfig();
        }
    }
}

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class OpenRouter(IReadOnlyDictionary<string, string> preferences)
{
    private static readonly IReadOnlyDictionary<string, string[]> ExtensionMap = new Dictionary<string, string[]>
    {
        ["powerpoint"] = [".ppt", ".pptx"],
        ["word"] = [".doc", ".docx"],
        ["excel"] = [".xls", ".xlsx"],
        ["pdf"] = [".pdf"]
    };

    public bool Open(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        var entryKey = ExtensionMap.FirstOrDefault(
            pair => pair.Value.Contains(extension, StringComparer.OrdinalIgnoreCase)).Key;

        if (string.IsNullOrWhiteSpace(entryKey))
        {
            return OpenWithSystem(filePath);
        }

        var target = preferences.TryGetValue(entryKey, out var configuredTarget)
            ? NormalizeTarget(configuredTarget)
            : OpenTargets.System;

        if (target == OpenTargets.System)
        {
            return OpenWithSystem(filePath);
        }

        var app = ResolveTargetApp(entryKey, target);
        return app is not null && OpenWithExecutable(app.ExecutablePath, filePath);
    }

    private static bool OpenWithSystem(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(filePath) ?? AppContext.BaseDirectory
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool OpenWithExecutable(string executablePath, string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = $"\"{filePath}\"",
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(filePath) ?? AppContext.BaseDirectory
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static AppInfo? ResolveTargetApp(string entryKey, string target)
    {
        return (entryKey, target) switch
        {
            ("word", OpenTargets.Office) => FindApp("WINWORD.EXE"),
            ("excel", OpenTargets.Office) => FindApp("EXCEL.EXE"),
            ("powerpoint", OpenTargets.Office) => FindApp("POWERPNT.EXE"),
            ("pdf", OpenTargets.Office) => FindApp("WINWORD.EXE"),
            ("word", OpenTargets.Wps) => FindApp("wps.exe"),
            ("excel", OpenTargets.Wps) => FindApp("et.exe"),
            ("powerpoint", OpenTargets.Wps) => FindApp("wpp.exe"),
            ("pdf", OpenTargets.Wps) => FindApp("wpspdf.exe"),
            _ => null
        };
    }

    private static AppInfo? FindApp(string exeName)
    {
        var path = FindAppPathFromRegistry(exeName) ?? FindAppPathFromCommonFolders(exeName);
        return path is null ? null : new AppInfo(path);
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

    private static string NormalizeTarget(string? target)
    {
        return target switch
        {
            OpenTargets.Office => OpenTargets.Office,
            OpenTargets.Wps => OpenTargets.Wps,
            _ => OpenTargets.System
        };
    }
}

internal static class OpenTargets
{
    public const string System = "System";
    public const string Office = "Office";
    public const string Wps = "WPS";
}

internal sealed record AppInfo(string ExecutablePath);
