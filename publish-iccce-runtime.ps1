param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [bool]$SelfContained = $true
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$iccceProject = Join-Path $repoRoot "Tools\ICCCE\Ink Canvas\InkCanvasForClass.csproj"
$runtimeDir = Join-Path $repoRoot "ThirdParty\ICCCE"

if (-not (Test-Path $iccceProject)) {
    throw "ICCCE project not found: $iccceProject"
}

New-Item -ItemType Directory -Force -Path $runtimeDir | Out-Null

$selfContainedValue = if ($SelfContained) { "true" } else { "false" }

dotnet publish $iccceProject `
    -c $Configuration `
    -r $Runtime `
    --self-contained $selfContainedValue `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=false `
    -o $runtimeDir

if ($LASTEXITCODE -ne 0) {
    throw "ICCCE publish failed with exit code $LASTEXITCODE."
}

Write-Host "ICCCE runtime published to $runtimeDir"
