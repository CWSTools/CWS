param(
    [string]$Configuration = "Release",
    [string]$PublishOutput = "publish",
    [string]$InstallerOutput = "installer",
    [string]$Runtime = "win-x64",
    [string]$IssPath = "CWSTools.iss",
    [switch]$SelfContained,
    [switch]$KeepSymbols,
    [switch]$KillRunning
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "Gallery.csproj"
$iss = Join-Path $root $IssPath

function Find-Iscc {
    $bundled = Join-Path $root "Tools\Inno Setup 6\ISCC.exe"
    if (Test-Path -LiteralPath $bundled) {
        return $bundled
    }

    $isccCommand = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
    if ($isccCommand) {
        return $isccCommand.Source
    }

    $candidates = @()
    if (${env:ProgramFiles(x86)}) {
        $candidates += Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"
    }
    if ($env:ProgramFiles) {
        $candidates += Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe"
    }

    return $candidates | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1
}

function Get-ShortCode {
    if (Get-Command git -ErrorAction SilentlyContinue) {
        $previousErrorActionPreference = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        try {
            $gitCode = git -C $root rev-parse --short=7 HEAD 2>$null
            if ($LASTEXITCODE -eq 0 -and $gitCode -match '^[0-9a-fA-F]{7}$') {
                return $gitCode.ToLowerInvariant()
            }
        }
        finally {
            $ErrorActionPreference = $previousErrorActionPreference
        }
    }

    $chars = "0123456789abcdef"
    return -join (1..7 | ForEach-Object { $chars[(Get-Random -Minimum 0 -Maximum $chars.Length)] })
}

if (-not (Test-Path -LiteralPath $project)) {
    throw "Project file not found: $project"
}

if (-not (Test-Path -LiteralPath $iss)) {
    throw "Inno Setup script not found: $iss"
}

$iscc = Find-Iscc
if (-not $iscc) {
    throw "ISCC.exe not found. Expected: $root\Tools\Inno Setup 6\ISCC.exe"
}

$running = Get-Process -Name "Gallery" -ErrorAction SilentlyContinue
if ($running) {
    if ($KillRunning) {
        $running | Stop-Process -Force
    }
    else {
        Write-Host "Gallery.exe is running. Close it first, or rerun with -KillRunning." -ForegroundColor Yellow
        $running | Select-Object Id, ProcessName, MainWindowTitle
        exit 1
    }
}

$projectXml = [xml](Get-Content -LiteralPath $project)
$propertyGroup = $projectXml.Project.PropertyGroup | Select-Object -First 1
$currentVersion = [string]$propertyGroup.Version
if ([string]::IsNullOrWhiteSpace($currentVersion)) {
    $currentVersion = "0.1.0"
}

$baseVersion = ($currentVersion -split '-', 2)[0]
$stamp = Get-Date -Format "yyMMddHHmmss"
$shortCode = Get-ShortCode
$version = "$baseVersion-$stamp.$shortCode"

if ($propertyGroup.Version) {
    $propertyGroup.Version = $version
}
else {
    $versionElement = $projectXml.CreateElement("Version")
    $versionElement.InnerText = $version
    [void]$propertyGroup.AppendChild($versionElement)
}

$projectXml.Save($project)

$publishRoot = if ([System.IO.Path]::IsPathRooted($PublishOutput)) {
    $PublishOutput
}
else {
    Join-Path $root $PublishOutput
}

$installerRoot = if ([System.IO.Path]::IsPathRooted($InstallerOutput)) {
    $InstallerOutput
}
else {
    Join-Path $root $InstallerOutput
}

$publishDir = Join-Path $publishRoot $version

if (Test-Path -LiteralPath $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

$publishArgs = @(
    "publish",
    $project,
    "-c",
    $Configuration,
    "-r",
    $Runtime,
    "--self-contained",
    $SelfContained.IsPresent.ToString().ToLowerInvariant(),
    "-p:DebugType=None",
    "-p:DebugSymbols=false",
    "-o",
    $publishDir
)

if ($SelfContained) {
    $publishArgs += "-p:PublishSingleFile=true"
}

Write-Host "Version updated: $version" -ForegroundColor Cyan
dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

if (-not $KeepSymbols) {
    Get-ChildItem -LiteralPath $publishDir -Recurse -File -Filter "*.pdb" |
        Remove-Item -Force
}

New-Item -ItemType Directory -Path $installerRoot -Force | Out-Null

& $iscc `
    "/DAppVersion=$version" `
    "/DSourceDir=$publishDir" `
    "/DOutputDir=$installerRoot" `
    $iss

Write-Host "Release version: $version" -ForegroundColor Cyan
Write-Host "Publish folder: $publishDir" -ForegroundColor Green
Write-Host "Installer output: $installerRoot" -ForegroundColor Green
