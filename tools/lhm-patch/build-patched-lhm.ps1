# Builds the patched LibreHardwareMonitorLib NuGet package that Celsius consumes.
#
# Upstream 0.9.6 throws in SMBios.GetDate for boards reporting an invalid BIOS
# date (e.g. "00/00/0000"), which aborts Computer.Open() and disables every
# sensor. See tools/lhm-patch/README.md for details.
#
# Usage:
#   pwsh -File tools/lhm-patch/build-patched-lhm.ps1

[CmdletBinding()]
param(
    [string]$LhmTag = 'v0.9.6',
    [string]$PackageVersion = '0.9.6-celsius1',
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$patchFile = Join-Path $PSScriptRoot 'lhm-0.9.6-smbios-date.patch'
$srcDir = Join-Path $repoRoot 'lhm-src'
$outDir = Join-Path $repoRoot 'local-nuget'

# Resolve git even when it is not on PATH (common on developer machines).
$gitCmd = Get-Command git -ErrorAction SilentlyContinue
$git = if ($gitCmd) { $gitCmd.Source } else { $null }
if (-not $git) {
    $candidates = @(
        "$env:ProgramFiles\Git\cmd\git.exe",
        "${env:ProgramFiles(x86)}\Git\cmd\git.exe",
        "$env:LOCALAPPDATA\Programs\Git\cmd\git.exe"
    )
    $git = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $git) {
    throw "git executable not found. Install Git or add it to PATH."
}

# --- 1. Clone the pinned upstream tag -------------------------------------------------
if (Test-Path $srcDir) {
    Write-Host "Reusing existing clone at $srcDir"
} else {
    Write-Host "Cloning LibreHardwareMonitor $LhmTag ..."
    & $git clone --depth 1 --branch $LhmTag `
        https://github.com/LibreHardwareMonitor/LibreHardwareMonitor.git $srcDir
}

# --- 2. Apply the patch ---------------------------------------------------------------
Push-Location $srcDir
try {
    # Reset first so re-runs are idempotent.
    & $git checkout -- LibreHardwareMonitorLib/Hardware/SMBios.cs
    Write-Host "Applying $(Split-Path -Leaf $patchFile) ..."
    & $git apply $patchFile
} finally {
    Pop-Location
}

# --- 3. Pack --------------------------------------------------------------------------
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$libProj = Join-Path $srcDir 'LibreHardwareMonitorLib\LibreHardwareMonitorLib.csproj'

# Resolve dotnet even when it is not on PATH.
$dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnet = if ($dotnetCmd) { $dotnetCmd.Source } else { $null }
if (-not $dotnet -and (Test-Path "$env:ProgramFiles\dotnet\dotnet.exe")) {
    $dotnet = "$env:ProgramFiles\dotnet\dotnet.exe"
}
if (-not $dotnet) {
    throw "dotnet executable not found. Install the .NET SDK or add it to PATH."
}

Write-Host "Building $PackageVersion ..."
& $dotnet build $libProj `
    -c $Configuration `
    -f net10.0 `
    -p:Platform=x64 `
    -p:Version=$PackageVersion

Write-Host "Packing $PackageVersion ..."
& $dotnet pack $libProj `
    -c $Configuration `
    -p:Platform=x64 `
    -p:Version=$PackageVersion `
    -p:TargetFrameworks=net10.0 `
    --no-build `
    -o $outDir

Write-Host "Done. Package written to $outDir"
