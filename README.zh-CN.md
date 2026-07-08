# CWS Tool

CWS Tool 是一个基于 Avalonia 和 Fluent 风格界面的 Windows 桌面工具，主要面向 Office / WPS 文档工作流。目前重点能力是文档打开方式偏好管理，以及通过轻量 Host 进程完成文件关联后的路由。

## English Documentation

See [README.md](README.md).

## 功能

- 支持 PowerPoint、Word、Excel、PDF 的打开方式偏好设置。
- 在应用内保守切换 Office / WPS，不直接修改 Windows 受保护的 `UserChoice`。
- 使用轻量级 `CWSOpenHost.exe` 处理文件关联路由和外部启动。
- 注册当前用户默认应用候选，支持 `.ppt`、`.pptx`、`.doc`、`.docx`、`.xls`、`.xlsx`、`.pdf`。
- 设置页提供行为日志开关，默认关闭。
- 支持开机自启动、托盘行为、主题、背景图模式和多语言界面。

## 打开方式逻辑

应用不会强行改写 Windows 默认程序哈希。当前方案是把 CWS Tool 注册为默认应用候选；当用户在 Windows 中把支持的文件类型设为使用 CWS Tool 打开后，由 `CWSOpenHost.exe` 按应用内偏好继续转发到 Office、WPS 或系统默认程序。

运行流程：

1. 用户为每一类文档选择 `系统默认`、`Microsoft Office` 或 `WPS`。
2. 偏好写入应用配置。
3. 根据当前偏好刷新 CWS Tool 注册的文件图标。
4. 当文件通过 CWS Tool 打开时，`CWSOpenHost.exe` 按偏好转发到目标程序。

这个方式比直接写 Windows 受保护的默认程序项更安全，也更容易回退。

## 项目结构

- `Gallery.csproj`：Avalonia 主程序，输出程序集为 `CWSTool`。
- `CWSOpenHost/CWSOpenHost.csproj`：无界面轻量 Host，用于文件关联和启动路由。
- `CWSTools.iss`：Inno Setup 安装包脚本。
- `publish-installer.ps1`：发布和打包辅助脚本。

## 环境要求

- 主要目标平台是 Windows。
- 需要 .NET SDK 10.0 或更新版本。
- 构建安装包需要 Inno Setup 6。

## 构建

```powershell
dotnet build .\CWSTools.sln
```

如果 `CWSTool.exe` 正在运行，构建可能会在最后复制输出文件时失败，因为 exe 被占用。关闭正在运行的应用后重新构建即可。

## 发布安装包

```powershell
.\publish-installer.ps1
```

常用参数：

```powershell
.\publish-installer.ps1 -Configuration Release
.\publish-installer.ps1 -SelfContained
.\publish-installer.ps1 -KillRunning
```

脚本会发布主程序和 `CWSOpenHost`，然后使用 `CWSTools.iss` 调用 Inno Setup 生成安装包。

## 配置与日志

运行配置存放在应用配置目录。行为日志默认关闭，可以在设置页开启。

开启后日志位于：

```text
Config/Logs/
```

## 注意事项

- `CWSOpenHost.exe` 保持轻量，无 UI。
- 打开方式切换依赖 Windows 将支持的文件类型路由到 CWS Tool。
- 系统默认应用的选择仍由 Windows 设置完成。
- 打开方式偏好变更时，会刷新 CWS Tool ProgID 对应的文件图标。
