# CWS Tool

CWS Tool is a Windows desktop utility built with Avalonia and Fluent-style UI. It focuses on Office/WPS document workflows, starting with file opening preferences and a lightweight host process for routing supported document types.

## Chinese Documentation

See [README.zh-CN.md](README.zh-CN.md).

## Features

- File opening preferences for PowerPoint, Word, Excel, and PDF files.
- Conservative Office/WPS switching inside the app without directly modifying Windows `UserChoice`.
- Lightweight `CWSOpenHost.exe` for file association routing and external launch handling.
- Current-user default-app candidate registration for `.ppt`, `.pptx`, `.doc`, `.docx`, `.xls`, `.xlsx`, and `.pdf`.
- Runtime behavior logging with a settings toggle.
- Startup option, tray behavior, theme settings, background image mode, and multilingual UI.

## Open Method Flow

The app does not forcibly rewrite Windows default-app hashes. Instead, it registers CWS Tool as a default-app candidate and routes files through `CWSOpenHost.exe` when Windows is configured to open supported files with CWS Tool.

At runtime:

1. The user selects `System`, `Microsoft Office`, or `WPS` for each document group.
2. Preferences are saved to the app config.
3. Registered CWS Tool file icons are refreshed based on the selected target.
4. When a supported file is opened through CWS Tool, `CWSOpenHost.exe` forwards it to the selected target app.

This keeps the implementation safer than directly editing Windows protected default-app entries.

## Projects

- `Gallery.csproj`: Main Avalonia desktop app. The output assembly is `CWSTool`.
- `CWSOpenHost/CWSOpenHost.csproj`: Lightweight no-UI host used by file associations and launch routing.
- `CWSTools.iss`: Inno Setup installer script.
- `publish-installer.ps1`: Publish and installer helper script.

## Requirements

- Windows is the primary target platform.
- .NET SDK 10.0 or newer.
- Inno Setup 6 is required for installer packaging.

## Build

```powershell
dotnet build .\CWSTools.sln
```

If `CWSTool.exe` is currently running, the build may fail at the final copy step because the executable is locked. Close the running app and build again.

## Publish Installer

```powershell
.\publish-installer.ps1
```

Useful options:

```powershell
.\publish-installer.ps1 -Configuration Release
.\publish-installer.ps1 -SelfContained
.\publish-installer.ps1 -KillRunning
```

The script publishes both the main app and `CWSOpenHost`, then invokes Inno Setup using `CWSTools.iss`.

## Configuration And Logs

Runtime configuration is stored under the app config directory. Behavior logging is disabled by default and can be enabled in Settings.

When enabled, logs are written under:

```text
Config/Logs/
```

## Notes

- `CWSOpenHost.exe` is intentionally lightweight and has no UI.
- Open-method switching depends on Windows routing supported files to CWS Tool.
- System-level default-app selection still happens through Windows Settings.
- File icons are refreshed for CWS Tool ProgIDs when open-method preferences change.

