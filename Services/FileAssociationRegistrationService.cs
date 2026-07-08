using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Gallery.Models;
using Microsoft.Win32;

namespace Gallery.Services;

public sealed class FileAssociationRegistrationService
{
    private const string AppName = "CWS Tool";
    private const string AppKeyPath = @"Software\CWS Tool";
    private const string CapabilitiesPath = AppKeyPath + @"\Capabilities";
    private const string RegisteredApplicationsPath = @"Software\RegisteredApplications";
    private const string HostExeName = "CWSOpenHost.exe";
    private const string ApplicationEntryPath = @"Software\Classes\Applications\" + HostExeName;
    private const string ProtocolKeyPath = @"Software\Classes\cwstool";
    private const int ShcneAssocChanged = 0x08000000;
    private const int ShcnfIdList = 0x0000;

    private static readonly IReadOnlyDictionary<string, string> ExtensionProgIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".doc"] = "CWSTool.Doc",
        [".docx"] = "CWSTool.Docx",
        [".xls"] = "CWSTool.Xls",
        [".xlsx"] = "CWSTool.Xlsx",
        [".ppt"] = "CWSTool.Ppt",
        [".pptx"] = "CWSTool.Pptx",
        [".pdf"] = "CWSTool.Pdf"
    };

    private static readonly IReadOnlyDictionary<string, string> ExtensionEntryKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".doc"] = "word",
        [".docx"] = "word",
        [".xls"] = "excel",
        [".xlsx"] = "excel",
        [".ppt"] = "powerpoint",
        [".pptx"] = "powerpoint",
        [".pdf"] = "pdf"
    };

    public IReadOnlyCollection<string> SupportedExtensions => ExtensionProgIds.Keys.ToArray();

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public FileAssociationRegistrationState GetState()
    {
        var registered = IsRegistered();
        var associatedCount = 0;

        foreach (var extension in ExtensionProgIds.Keys)
        {
            if (IsExtensionAssociatedWithCwsTool(extension))
            {
                associatedCount++;
            }
        }

        return new FileAssociationRegistrationState(
            registered,
            associatedCount,
            ExtensionProgIds.Count,
            associatedCount == ExtensionProgIds.Count);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public FileAssociationRegistrationResult RegisterForCurrentUser(IReadOnlyDictionary<string, string>? preferences = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new FileAssociationRegistrationResult(FileAssociationRegistrationStatus.UnsupportedPlatform);
        }

        var appExecutablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(appExecutablePath) || !File.Exists(appExecutablePath))
        {
            return new FileAssociationRegistrationResult(FileAssociationRegistrationStatus.ExecutableNotFound);
        }

        var hostExecutablePath = ResolveHostExecutablePath(appExecutablePath);

        try
        {
            RegisterCapabilities(appExecutablePath);
            RegisterApplicationEntry(hostExecutablePath);
            RegisterProgIds(appExecutablePath, hostExecutablePath, preferences);
            RegisterProtocol(appExecutablePath, hostExecutablePath);
            RegisterApplication();
            NotifyShellAssociationChanged();
            AppLoggerService.Info("association", "Registered CWS Tool as a current-user default-app candidate.");
            return new FileAssociationRegistrationResult(FileAssociationRegistrationStatus.Registered);
        }
        catch (Exception ex)
        {
            AppLoggerService.Error("association", ex, "Failed to register CWS Tool as default-app candidate.");
            return new FileAssociationRegistrationResult(FileAssociationRegistrationStatus.Failed, ex.Message);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public FileAssociationRegistrationResult RefreshFileIcons(IReadOnlyDictionary<string, string> preferences)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new FileAssociationRegistrationResult(FileAssociationRegistrationStatus.UnsupportedPlatform);
        }

        var appExecutablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(appExecutablePath))
        {
            appExecutablePath = Path.Combine(AppContext.BaseDirectory, "CWSTool.exe");
        }

        try
        {
            foreach (var (extension, progId) in ExtensionProgIds)
            {
                using var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}");
                using var defaultIcon = progIdKey.CreateSubKey("DefaultIcon");
                defaultIcon.SetValue(string.Empty, ResolveFileIconPath(extension, preferences, appExecutablePath));
            }

            NotifyShellAssociationChanged();
            AppLoggerService.Info("association", "Refreshed CWS Tool file icons from open-method preferences.");
            return new FileAssociationRegistrationResult(FileAssociationRegistrationStatus.Registered);
        }
        catch (Exception ex)
        {
            AppLoggerService.Error("association", ex, "Failed to refresh CWS Tool file icons.");
            return new FileAssociationRegistrationResult(FileAssociationRegistrationStatus.Failed, ex.Message);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public FileAssociationRegistrationResult OpenDefaultAppsSettings()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new FileAssociationRegistrationResult(FileAssociationRegistrationStatus.UnsupportedPlatform);
        }

        var candidates = new[]
        {
            "ms-settings:defaultapps?registeredAppUser=CWS%20Tool",
            "ms-settings:defaultapps?registeredAppMachine=CWS%20Tool",
            "ms-settings:defaultapps"
        };

        foreach (var candidate in candidates)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = candidate,
                    UseShellExecute = true
                });
                AppLoggerService.Info("association", $"Opened Windows Default Apps settings. Uri={candidate}");
                return new FileAssociationRegistrationResult(FileAssociationRegistrationStatus.OpenedSettings);
            }
            catch
            {
            }
        }

        return new FileAssociationRegistrationResult(FileAssociationRegistrationStatus.SettingsLaunchFailed);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public bool IsExtensionAssociatedWithCwsTool(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        var progId = GetUserChoiceProgId(extension) ?? GetClassRootProgId(extension);
        return progId is not null && ExtensionProgIds.Values.Contains(progId, StringComparer.OrdinalIgnoreCase);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static bool IsRegistered()
    {
        using var currentUserKey = Registry.CurrentUser.OpenSubKey(RegisteredApplicationsPath);
        if (string.Equals(currentUserKey?.GetValue(AppName) as string, CapabilitiesPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        using var localMachineKey = Registry.LocalMachine.OpenSubKey(RegisteredApplicationsPath);
        return string.Equals(localMachineKey?.GetValue(AppName) as string, CapabilitiesPath, StringComparison.OrdinalIgnoreCase);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void RegisterCapabilities(string executablePath)
    {
        using var capabilities = Registry.CurrentUser.CreateSubKey(CapabilitiesPath);
        capabilities.SetValue("ApplicationName", AppName);
        capabilities.SetValue("ApplicationDescription", "Routes Office, WPS, and PDF files through CWS Tool preferences.");
        capabilities.SetValue("ApplicationIcon", $"{executablePath},0");

        using var fileAssociations = capabilities.CreateSubKey("FileAssociations");
        foreach (var (extension, progId) in ExtensionProgIds)
        {
            fileAssociations.SetValue(extension, progId);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void RegisterApplicationEntry(string hostExecutablePath)
    {
        using var appKey = Registry.CurrentUser.CreateSubKey(ApplicationEntryPath);
        appKey.SetValue("FriendlyAppName", AppName);

        using var command = appKey.CreateSubKey(@"shell\open\command");
        command.SetValue(string.Empty, $"\"{hostExecutablePath}\" \"%1\"");

        using var supportedTypes = appKey.CreateSubKey("SupportedTypes");
        foreach (var extension in ExtensionProgIds.Keys)
        {
            supportedTypes.SetValue(extension, string.Empty);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void RegisterProgIds(
        string appExecutablePath,
        string hostExecutablePath,
        IReadOnlyDictionary<string, string>? preferences)
    {
        foreach (var (extension, progId) in ExtensionProgIds)
        {
            using var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}");
            progIdKey.SetValue(string.Empty, $"CWS Tool {extension.ToUpperInvariant()} File");
            progIdKey.SetValue("FriendlyTypeName", $"CWS Tool {extension.ToUpperInvariant()} File");

            using var defaultIcon = progIdKey.CreateSubKey("DefaultIcon");
            defaultIcon.SetValue(string.Empty, ResolveFileIconPath(extension, preferences, appExecutablePath));

            using var command = progIdKey.CreateSubKey(@"shell\open\command");
            command.SetValue(string.Empty, $"\"{hostExecutablePath}\" \"%1\"");
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void RegisterProtocol(string appExecutablePath, string hostExecutablePath)
    {
        using var protocolKey = Registry.CurrentUser.CreateSubKey(ProtocolKeyPath);
        protocolKey.SetValue(string.Empty, "URL:CWS Tool Protocol");
        protocolKey.SetValue("URL Protocol", string.Empty);

        using var defaultIcon = protocolKey.CreateSubKey("DefaultIcon");
        defaultIcon.SetValue(string.Empty, $"{appExecutablePath},0");

        using var command = protocolKey.CreateSubKey(@"shell\open\command");
        command.SetValue(string.Empty, $"\"{hostExecutablePath}\" \"%1\"");
    }

    private static string ResolveHostExecutablePath(string appExecutablePath)
    {
        var hostPath = Path.Combine(AppContext.BaseDirectory, HostExeName);
        return File.Exists(hostPath) ? hostPath : appExecutablePath;
    }

    private static string ResolveFileIconPath(
        string extension,
        IReadOnlyDictionary<string, string>? preferences,
        string executablePath)
    {
        var entryKey = ExtensionEntryKeys.TryGetValue(extension, out var value) ? value : string.Empty;
        var target = ResolvePreferredTarget(entryKey, preferences);
        var iconRelativePath = ResolveIconRelativePath(entryKey, target)
            ?? ResolveIconRelativePath(entryKey, OpenMethodTargets.System);

        if (!string.IsNullOrWhiteSpace(iconRelativePath))
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", iconRelativePath);
            if (File.Exists(iconPath))
            {
                return iconPath;
            }
        }

        return $"{executablePath},0";
    }

    private static string? ResolveIconRelativePath(string entryKey, string target)
    {
        if (string.Equals(target, OpenMethodTargets.Wps, StringComparison.OrdinalIgnoreCase))
        {
            return entryKey switch
            {
                "word" => Path.Combine("wpsofficeicon", "IDI_57APPLICATION.ico"),
                "excel" => Path.Combine("wpsofficeicon", "IDI_58APPLICATION.ico"),
                "powerpoint" => Path.Combine("wpsofficeicon", "IDI_59APPLICATION.ico"),
                "pdf" => Path.Combine("wpsofficeicon", "IDI_60APPLICATION.ico"),
                _ => null
            };
        }

        return entryKey switch
        {
            "word" => Path.Combine("wordicon", "#203.ico"),
            "excel" => Path.Combine("xlicons", "#260.ico"),
            "powerpoint" => Path.Combine("pptico", "#1303.ico"),
            "pdf" => Path.Combine("wpsofficeicon", "IDI_60APPLICATION.ico"),
            _ => null
        };
    }

    private static string ResolvePreferredTarget(
        string entryKey,
        IReadOnlyDictionary<string, string>? preferences)
    {
        if (!string.IsNullOrWhiteSpace(entryKey) &&
            preferences is not null &&
            preferences.TryGetValue(entryKey, out var target))
        {
            return NormalizeTarget(target);
        }

        return OpenMethodTargets.System;
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

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void RegisterApplication()
    {
        using var registeredApplications = Registry.CurrentUser.CreateSubKey(RegisteredApplicationsPath);
        registeredApplications.SetValue(AppName, CapabilitiesPath);
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

    private static void NotifyShellAssociationChanged()
    {
        SHChangeNotify(ShcneAssocChanged, ShcnfIdList, IntPtr.Zero, IntPtr.Zero);
    }

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
}

public sealed record FileAssociationRegistrationState(
    bool IsRegistered,
    int AssociatedExtensionCount,
    int SupportedExtensionCount,
    bool AllSupportedExtensionsAssociated);

public sealed record FileAssociationRegistrationResult(
    FileAssociationRegistrationStatus Status,
    string? ErrorMessage = null);

public enum FileAssociationRegistrationStatus
{
    Registered,
    OpenedSettings,
    ExecutableNotFound,
    SettingsLaunchFailed,
    UnsupportedPlatform,
    Failed
}
