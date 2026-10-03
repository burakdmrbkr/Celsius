# Generates the Celsius application icon (logo) as a multi-resolution .ico and a .png.
#
# The design is a flat red circle with a white outline thermometer, matching the
# project logo. Run this whenever the logo needs to be regenerated:
#
#   powershell -ExecutionPolicy Bypass -File tools\make-icon.ps1
#
# It writes:
#   src/Celsius.App/Assets/celsius.ico   (16,24,32,48,64,128,256)
#   src/Celsius.App/Assets/celsius.png   (512x512, for docs/README)

[CmdletBinding()]
param(
    [string]$OutDir = ''
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutDir)) {
    $OutDir = Join-Path $repoRoot 'src\Celsius.App\Assets'
}
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

# ---------------------------------------------------------------- drawing ----

function New-LogoBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    # Colors
    $red = [System.Drawing.Color]::FromArgb(255, 176, 42, 26)   # #B02A1A-ish
    $white = [System.Drawing.Color]::White

    # Scale factor relative to a 768px design canvas.
    $s = $size / 768.0

    # --- red circle background (fills the icon) ---
    $bg = New-Object System.Drawing.SolidBrush($red)
    $g.FillEllipse($bg, 0, 0, $size, $size)
    $bg.Dispose()

    # --- thermometer, drawn with thick white strokes (design is line-art) ---
    # All coordinates are relative to a 768px canvas, scaled by $s.
    $stroke = [Math]::Max(1.0, 30.0 * $s)
    $pen = New-Object System.Drawing.Pen($white, $stroke)
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

    # Bulb centre sits below the stem; the stem is a vertical rounded capsule.
    $cx = 0.50 * $size
    $bulbCY = 0.63 * $size
    $bulbOD = 0.30 * $size          # outer bulb diameter (stroke midline)
    $stemTop = 0.16 * $size
    $stemHalf = 0.085 * $size       # half of the stem width
    $stemBottom = $bulbCY           # stem meets the bulb centre

    # Stem as a capsule: two vertical lines + a round top cap.
    $stemLeft = $cx - $stemHalf
    $stemRight = $cx + $stemHalf
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $stemHalf * 2.0
    $path.AddArc($stemLeft, $stemTop, $d, $d, 180, 180)                 # top cap
    $path.AddLine($stemRight, $stemTop + $stemHalf, $stemRight, $stemBottom)
    $path.AddLine($stemLeft, $stemBottom, $stemLeft, $stemTop + $stemHalf)
    $path.CloseFigure()
    $g.DrawPath($pen, $path)

    # Bulb: outer circle (outline) + filled white centre (mercury).
    $bulbX = $cx - $bulbOD / 2.0
    $bulbY = $bulbCY - $bulbOD / 2.0
    $g.DrawEllipse($pen, $bulbX, $bulbY, $bulbOD, $bulbOD)
    $innerD = $bulbOD - 2.6 * $stroke
    if ($innerD -gt 1) {
        $fillBrush = New-Object System.Drawing.SolidBrush($white)
        $g.FillEllipse($fillBrush, $bulbX + ($bulbOD - $innerD) / 2.0, $bulbY + ($bulbOD - $innerD) / 2.0, $innerD, $innerD)
        $fillBrush.Dispose()
    }

    # Mercury column: a thin white line from just below the top cap into the bulb.
    $innerPen = New-Object System.Drawing.Pen($white, [Math]::Max(1.0, 20.0 * $s))
    $innerPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $innerPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawLine($innerPen, $cx, 0.34 * $size, $cx, $bulbY)

    # Scale ticks on the right side of the stem.
    $tickPen = New-Object System.Drawing.Pen($white, [Math]::Max(1.0, 20.0 * $s))
    $tickPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $tickPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $tickStartX = $stemRight + 3.0 * $stroke
    for ($i = 0; $i -lt 5; $i++) {
        $y = 0.26 * $size + $i * 0.062 * $size
        $len = if ($i % 2 -eq 0) { 0.075 * $size } else { 0.048 * $size }
        $g.DrawLine($tickPen, $tickStartX, $y, $tickStartX + $len, $y)
    }

    $tickPen.Dispose()
    $innerPen.Dispose()
    $pen.Dispose()
    $path.Dispose()
    $g.Dispose()
    return $bmp
}

# ------------------------------------------------------------------- ICO -----

# A multi-resolution .ico is built by writing PNG-encoded frames into the ICO
# container (Windows Vista+ / all supported Windows versions read PNG frames).
function Write-IcoFile([string]$Path, [int[]]$Sizes) {
    $frames = @()
    foreach ($sz in $Sizes) {
        $bmp = New-LogoBitmap $sz
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

# ------------------------------------------------------------------ run ------

$icoSizes = 16, 24, 32, 48, 64, 128, 256
$icoPath = Join-Path $OutDir 'celsius.ico'
Write-IcoFile -Path $icoPath -Sizes $icoSizes
Write-Host "Wrote $icoPath"

$pngPath = Join-Path $OutDir 'celsius.png'
$png = New-LogoBitmap 512
$png.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
$png.Dispose()
Write-Host "Wrote $pngPath"
