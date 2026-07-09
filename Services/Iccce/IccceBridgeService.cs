using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Gallery.Models;

namespace Gallery.Services.Iccce;

public sealed class IccceBridgeService
{
    private readonly AppConfig _config;

    public IccceBridgeService(AppConfig? config = null)
    {
        _config = config ?? RuntimeConfigService.Current;
    }

    public IccceBridgeResult ShowFloatingBar() => Launch("--show", "icc://show");

    public IccceBridgeResult OpenWhiteboard() => Launch("--board", "icc://board");

    public IccceBridgeResult CloseIccce() => SendUriCommand("exit");

    public IccceBridgeResult SendUriCommand(string command)
    {
        var normalized = NormalizeUriCommand(command);
        return Launch(normalized, normalized);
    }

    private IccceBridgeResult Launch(string exeArgument, string uriFallback)
    {
        if (!OperatingSystem.IsWindows())
        {
            return IccceBridgeResult.UnsupportedPlatform;
        }

        var exePath = ResolveExecutablePath();
        if (!string.IsNullOrWhiteSpace(exePath))
        {
            return StartProcess(exePath, exeArgument);
        }

        return StartProcess(uriFallback, string.Empty);
    }

    private IccceBridgeResult StartProcess(string fileName, string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = true
            };

            if (!string.IsNullOrWhiteSpace(arguments))
            {
                startInfo.Arguments = QuoteArgument(arguments);
            }

            Process.Start(startInfo);
            return IccceBridgeResult.Ok;
        }
        catch (Exception ex)
        {
            AppLoggerService.Info("iccce", $"启动 ICCCE 失败: File={fileName}, Args={arguments}, Error={ex}");
            return new IccceBridgeResult(false, ex.Message);
        }
    }

    private string ResolveExecutablePath()
    {
        foreach (var candidate in EnumerateExecutableCandidates())
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private IEnumerable<string> EnumerateExecutableCandidates()
    {
        if (!string.IsNullOrWhiteSpace(_config.Iccce.IccceExecutablePath))
        {
            yield return _config.Iccce.IccceExecutablePath;
        }

        var envPath = Environment.GetEnvironmentVariable("ICCCE_PATH");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            yield return envPath;
        }

        yield return Path.Combine(AppContext.BaseDirectory, "ICCCE", "InkCanvasForClass.exe");
        yield return Path.Combine(AppContext.BaseDirectory, "ThirdParty", "ICCCE", "InkCanvasForClass.exe");

        foreach (var root in EnumerateSearchRoots())
        {
            yield return Path.Combine(root, "ThirdParty", "ICCCE", "InkCanvasForClass.exe");

            var sourceRoot = Path.Combine(root, "Tools", "ICCCE", "Ink Canvas");
            foreach (var relative in new[]
                     {
                         Path.Combine("bin", "Release", "net6.0-windows10.0.19041.0", "InkCanvasForClass.exe"),
                         Path.Combine("bin", "Debug", "net6.0-windows10.0.19041.0", "InkCanvasForClass.exe"),
                         Path.Combine("bin", "publish", "InkCanvasForClass.exe"),
                         "InkCanvasForClass.exe"
                     })
            {
                yield return Path.Combine(sourceRoot, relative);
            }
        }
    }

    private static IEnumerable<string> EnumerateSearchRoots()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var seed in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
        {
            var directory = new DirectoryInfo(seed);
            while (directory != null)
            {
                if (seen.Add(directory.FullName))
                {
                    yield return directory.FullName;
                }

                directory = directory.Parent;
            }
        }
    }

    private static string NormalizeUriCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return "icc://show";
        }

        var trimmed = command.Trim();
        return trimmed.StartsWith("icc:", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"icc://{trimmed.TrimStart('/')}";
    }

    private static string QuoteArgument(string argument) =>
        argument.Contains(' ') ? $"\"{argument.Replace("\"", "\\\"")}\"" : argument;
}

public readonly record struct IccceBridgeResult(bool Success, string? ErrorMessage)
{
    public static IccceBridgeResult Ok { get; } = new(true, null);
    public static IccceBridgeResult UnsupportedPlatform { get; } = new(false, "当前平台不支持 ICCCE 调用。");
}
