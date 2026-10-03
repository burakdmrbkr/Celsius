# Builds the Celsius application icon (.ico) from the source logo PNG.
#
# The source is src/Celsius.App/Assets/celsius.png (the project logo). This
# script scales it to the standard icon sizes and packs them into a
# multi-resolution .ico used for Celsius.exe and the system tray.
#
# Run after replacing the logo PNG:
#
#   powershell -ExecutionPolicy Bypass -File tools\make-icon.ps1
#
# Output:
#   src/Celsius.App/Assets/celsius.ico   (16,24,32,48,64,128,256)

[CmdletBinding()]
param(
    [string]$SourcePng = '',
    [string]$OutIco = ''
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($SourcePng)) {
    $SourcePng = Join-Path $repoRoot 'src\Celsius.App\Assets\celsius.png'
}
if ([string]::IsNullOrWhiteSpace($OutIco)) {
    $OutIco = Join-Path $repoRoot 'src\Celsius.App\Assets\celsius.ico'
}

if (-not (Test-Path $SourcePng)) {
    throw "Source logo not found: $SourcePng"
}

Write-Host "Source logo: $SourcePng"

function New-ScaledBitmap([System.Drawing.Image]$img, [int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($img, 0, 0, $size, $size)
    $g.Dispose()
    return $bmp
}

# A multi-resolution .ico is built by writing PNG-encoded frames into the ICO
# container (Windows Vista+ / all supported Windows versions read PNG frames).
function Write-IcoFile([string]$Path, [int[]]$Sizes, [System.Drawing.Image]$img) {
    $frames = @()
    foreach ($sz in $Sizes) {
        $bmp = New-ScaledBitmap $img $sz
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        $frames += ,@{ Size = $sz; Bytes = $ms.ToArray() }
        $ms.Dispose()
    }

    $fs = [System.IO.File]::Create($Path)
    $bw = New-Object System.IO.BinaryWriter($fs)

    # ICONDIR
    $bw.Write([UInt16]0)                 # reserved
    $bw.Write([UInt16]1)                 # type = icon
    $bw.Write([UInt16]$frames.Count)     # image count

    # ICONDIRENTRY per frame (16 bytes each)
    $offset = 6 + 16 * $frames.Count
    foreach ($f in $frames) {
        $dim = if ($f.Size -ge 256) { 0 } else { $f.Size }   # 0 means 256
        $bw.Write([Byte]$dim)            # width
        $bw.Write([Byte]$dim)            # height
        $bw.Write([Byte]0)               # palette colours
        $bw.Write([Byte]0)               # reserved
        $bw.Write([UInt16]1)             # colour planes
        $bw.Write([UInt16]32)            # bits per pixel
        $bw.Write([UInt32]$f.Bytes.Length)
        $bw.Write([UInt32]$offset)
        $offset += $f.Bytes.Length
    }

    foreach ($f in $frames) { $bw.Write($f.Bytes) }

    $bw.Dispose()
    $fs.Dispose()
}

$source = [System.Drawing.Image]::FromFile((Resolve-Path $SourcePng))
try {
    $sizes = 16, 24, 32, 48, 64, 128, 256
    Write-IcoFile -Path $OutIco -Sizes $sizes -Img $source
    Write-Host "Wrote $OutIco"
}
finally {
    $source.Dispose()
}
