using System;
using Gallery.Models;

namespace Gallery.Services;

public static class RuntimeConfigService
{
    private static AppConfig _current = new();

    public static AppConfig Current => _current;

    public static void Initialize(AppConfig? config)
    {
        _current = config ?? new AppConfig();
    }

    public static void Update(Action<AppConfig> update)
    {
        update(_current);
        ConfigService.SaveConfig(_current);
    }
}
