# Downloads the PawnIO installer that gets bundled into the Celsius setup.
#
# PawnIO is a separate, signed kernel driver (GPL-2.0) that Celsius does not
# redistribute in source form. We pin an exact version so release builds are
# reproducible and reviewed, rather than silently pulling "latest".
#
# Usage:
#   pwsh -File installer\fetch-pawnio.ps1 [-Version 2.2.0] [-OutDir vendor]

[CmdletBinding()]
param(
    [string]$Version = '2.2.0',
    [string]$OutDir = 'vendor'
)

$ErrorActionPreference = 'Stop'

$asset = 'PawnIO_setup.exe'
$url = "https://github.com/namazso/PawnIO.Setup/releases/download/$Version/$asset"

$repoRoot = Split-Path -Parent $PSScriptRoot
$targetDir = if ([System.IO.Path]::IsPathRooted($OutDir)) { $OutDir } else { Join-Path $repoRoot $OutDir }
$targetPath = Join-Path $targetDir $asset

New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

Write-Host "Downloading PawnIO $Version ..."
Write-Host "  $url"

Invoke-WebRequest -Uri $url -OutFile $targetPath -UseBasicParsing

$size = (Get-Item $targetPath).Length
Write-Host "Saved $targetPath ($([math]::Round($size / 1MB, 2)) MB)"

# Emit the path so CI can capture it if needed.
Write-Output $targetPath
