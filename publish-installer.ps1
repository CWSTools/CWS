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

$root = if ($PSScriptRoot) {
    $PSScriptRoot
}
else {
    Split-Path -Parent $MyInvocation.MyCommand.Path
}
$project = Join-Path $root "Gallery.csproj"
$hostProject = Join-Path $root "CWSOpenHost\CWSOpenHost.csproj"
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

function ConvertTo-ShortHash {
    param(
        [Parameter(Mandatory = $true)]
        [byte[]]$Bytes
    )

    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha.ComputeHash($Bytes)
        return (-join ($hash | ForEach-Object { $_.ToString("x2") })).Substring(0, 7)
    }
    finally {
        $sha.Dispose()
    }
}

function ConvertTo-ShortTextHash {
    param(
        [AllowEmptyString()]
        [string]$Text
    )

    return ConvertTo-ShortHash -Bytes ([System.Text.Encoding]::UTF8.GetBytes($Text))
}

function Get-FileHashText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $stream = [System.IO.File]::OpenRead($Path)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha.ComputeHash($stream)
        return -join ($hash | ForEach-Object { $_.ToString("x2") })
    }
    finally {
        $sha.Dispose()
        $stream.Dispose()
    }
}

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        return $null
    }

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = & git -C $root @Arguments 2>$null
        if ($LASTEXITCODE -ne 0) {
            return $null
        }

        return @($output)
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
}

function Get-ShortCode {
    $gitCode = Invoke-Git -Arguments @("rev-parse", "--short=7", "HEAD")
    if ($gitCode -and $gitCode[0] -match '^[0-9a-fA-F]{7}$') {
        $commitCode = $gitCode[0].ToLowerInvariant()
        $status = Invoke-Git -Arguments @("status", "--porcelain=v1", "--untracked-files=all")
        if ($null -ne $status -and $status.Count -eq 0) {
            return $commitCode
        }

        $fullHead = Invoke-Git -Arguments @("rev-parse", "HEAD")
        $diff = Invoke-Git -Arguments @("diff", "--binary", "HEAD", "--")
        $untrackedFiles = Invoke-Git -Arguments @("ls-files", "--others", "--exclude-standard")
        $sourceState = [System.Text.StringBuilder]::new()

        [void]$sourceState.AppendLine("HEAD:$($fullHead[0])")
        [void]$sourceState.AppendLine("STATUS:")
        [void]$sourceState.AppendLine(($status | Sort-Object) -join "`n")
        [void]$sourceState.AppendLine("DIFF:")
        [void]$sourceState.AppendLine($diff -join "`n")
        [void]$sourceState.AppendLine("UNTRACKED:")

        foreach ($relativePath in ($untrackedFiles | Sort-Object)) {
            $normalizedPath = $relativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar
            $absolutePath = Join-Path $root $normalizedPath
            if (Test-Path -LiteralPath $absolutePath -PathType Leaf) {
                $item = Get-Item -LiteralPath $absolutePath
                [void]$sourceState.AppendLine("$relativePath|$($item.Length)|$(Get-FileHashText -Path $absolutePath)")
            }
        }

        return ConvertTo-ShortTextHash -Text $sourceState.ToString()
    }

    $chars = "0123456789abcdef"
    return -join (1..7 | ForEach-Object { $chars[(Get-Random -Minimum 0 -Maximum $chars.Length)] })
}

if (-not (Test-Path -LiteralPath $project)) {
    throw "Project file not found: $project"
}

if (-not (Test-Path -LiteralPath $hostProject)) {
    throw "Open host project file not found: $hostProject"
}

if (-not (Test-Path -LiteralPath $iss)) {
    throw "Inno Setup script not found: $iss"
}

$iscc = Find-Iscc
if (-not $iscc) {
    throw "ISCC.exe not found. Expected: $root\Tools\Inno Setup 6\ISCC.exe"
}

$running = @(
    Get-Process -Name "CWSTool" -ErrorAction SilentlyContinue
    Get-Process -Name "CWSOpenHost" -ErrorAction SilentlyContinue
)
if ($running) {
    if ($KillRunning) {
        $running | Stop-Process -Force
    }
    else {
        Write-Host "CWS Tool is running. Close CWSTool.exe/CWSOpenHost.exe first, or rerun with -KillRunning." -ForegroundColor Yellow
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

$hostPublishArgs = @(
    "publish",
    $hostProject,
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
    $hostPublishArgs += "-p:PublishSingleFile=true"
}

dotnet @hostPublishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish CWSOpenHost failed with exit code $LASTEXITCODE."
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
