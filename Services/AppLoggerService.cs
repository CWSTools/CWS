using System;
using System.IO;
using System.Text;

namespace Gallery.Services;

public static class AppLoggerService
{
    private static readonly object SyncRoot = new();
    private static bool _isEnabled;

    private static string LogDir => Path.Combine(AppContext.BaseDirectory, "Config", "Logs");

    private static string CurrentLogPath => Path.Combine(LogDir, $"behavior-{DateTime.Now:yyyyMMdd}.log");

    public static bool IsEnabled => _isEnabled;

    public static void Initialize(bool isEnabled)
    {
        _isEnabled = isEnabled;

        if (_isEnabled)
        {
            WriteLine("system", "Behavior logging initialized.");
        }
    }

    public static void SetEnabled(bool enabled)
    {
        if (_isEnabled == enabled)
        {
            return;
        }

        if (enabled)
        {
            _isEnabled = true;
            WriteLine("system", "Behavior logging enabled.");
            return;
        }

        WriteLine("system", "Behavior logging disabled.");
        _isEnabled = false;
    }

    public static void Info(string category, string message)
    {
        if (!_isEnabled)
        {
            return;
        }

        WriteLine(category, message);
    }

    public static void Error(string category, Exception exception, string? message = null)
    {
        if (!_isEnabled)
        {
            return;
        }

        var finalMessage = string.IsNullOrWhiteSpace(message)
            ? exception.ToString()
            : $"{message}{Environment.NewLine}{exception}";
        WriteLine(category, finalMessage);
    }

    private static void WriteLine(string category, string message)
    {
        try
        {
            lock (SyncRoot)
            {
                Directory.CreateDirectory(LogDir);
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{category}] {message}{Environment.NewLine}";
                File.AppendAllText(CurrentLogPath, line, Encoding.UTF8);
            }
        }
        catch
        {
        }
    }
}
