using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaFluentUI.Styling;
using Gallery.Models;

namespace Gallery.Services;

public class ConfigService
{
    private static string ConfigDir => Path.Combine(AppContext.BaseDirectory, "Config");
    private static string AppConfigPath => Path.Combine(ConfigDir, "config.json");

    static ConfigService()
    {
    }

    public static void SaveConfig(AppConfig config)
    {
        try
        {
#if DEBUG
            Debug.WriteLine("BaseDirectory: " + AppContext.BaseDirectory);
            Debug.WriteLine("CurrentDirectory: " + Environment.CurrentDirectory);
            Debug.WriteLine("FullPath: " + Path.GetFullPath(AppConfigPath));
#endif

            Directory.CreateDirectory(ConfigDir);
            var json = JsonSerializer.Serialize(config, ConfigJsonContext.Default.AppConfig);
            File.WriteAllText(AppConfigPath, json, Encoding.UTF8);
        }
#if DEBUG
        catch (Exception e)
        {
            Debug.WriteLine("Write Failed");
            Debug.WriteLine(e);
        }
#else
        catch
        {
        }
#endif
    }

    public static AppConfig? LoadConfig()
    {
#if DEBUG
        Debug.WriteLine("BaseDirectory: " + AppContext.BaseDirectory);
        Debug.WriteLine("CurrentDirectory: " + Environment.CurrentDirectory);
        Debug.WriteLine("FullPath: " + Path.GetFullPath(AppConfigPath));
#endif

        Directory.CreateDirectory(ConfigDir);

        if (!File.Exists(AppConfigPath))
        {
            var config = new AppConfig
            {
                Theme = "Default",
                IsCustomAccentColor = false,
                IsWindowEffectEnabled = true,
                IsEnabledBackgroundImage = false,
                WindowEffect = "Mica",
                Language = "zh-CN",
                ReleaseResourcesOnMinimize = true,
                HideToTrayAfterMinimizeDelay = true,
                IsBehaviorLoggingEnabled = false,
                IsLaunchAtStartupEnabled = false,
                OpenMethodPreferences = [],
                Iccce = new()
            };

            Console.WriteLine("Config File Not Exists, Return Of Create");
            return config;
        }

        string file = File.ReadAllText(AppConfigPath);
        var loaded = JsonSerializer.Deserialize(file, ConfigJsonContext.Default.AppConfig);

        if (loaded != null)
        {
            NormalizeConfig(loaded);
            Application.Current?.RequestedThemeVariant = loaded.Theme switch
            {
                "Light" => ThemeVariant.Light,
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
        }

        Console.WriteLine("Config File Loaded");
        return loaded;
    }

    public static bool IsDarkTheme() => Application.Current?.RequestedThemeVariant == ThemeVariant.Dark;

    private static void NormalizeConfig(AppConfig config)
    {
        config.OpenMethodPreferences ??= [];
        config.Iccce ??= new();
        config.Iccce.Ppt ??= new();
        config.Iccce.Canvas ??= new();
        config.Iccce.Gesture ??= new();
        config.Iccce.Automation ??= new();
        config.Iccce.Automation.FloatingWindowInterceptor ??= new();
        config.Iccce.Automation.FloatingWindowInterceptor.InterceptRules ??= [];
        config.Iccce.Toolbar ??= new();
        config.Iccce.MiniWhiteboard ??= new();
        config.Iccce.Notification ??= new();
        config.Iccce.Performance ??= new();
        config.Iccce.Security ??= new();
        config.Iccce.Advanced ??= new();
    }
}
