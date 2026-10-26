<#
.SYNOPSIS
    Generates the A$PECT UTILS brand assets used by the docs site and desktop app.

.DESCRIPTION
    Draws the wordmark, favicon source, social preview, and application icon from one palette so
    the PowerShell app, WPF desktop app, and docs site stay visually consistent.

    Run from the repository root:
        powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\branding\generate-brand-assets.ps1
#>
[CmdletBinding()]
param()

Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$brandingDir = Join-Path $root "docs\src\assets\branding"
$publicDir = Join-Path $root "docs\public"
$desktopAssets = Join-Path $root "desktop\Assets"

foreach ($dir in @($brandingDir, $publicDir, $desktopAssets)) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
}

$palette = @{
    Mint      = [System.Drawing.ColorTranslator]::FromHtml("#4ADE80")
    MintDeep  = [System.Drawing.ColorTranslator]::FromHtml("#16A34A")
    MintPale  = [System.Drawing.ColorTranslator]::FromHtml("#D1FAE5")
    Ink       = [System.Drawing.ColorTranslator]::FromHtml("#161A1D")
    Panel     = [System.Drawing.ColorTranslator]::FromHtml("#1F2529")
    Gray      = [System.Drawing.ColorTranslator]::FromHtml("#9CA3AF")
    GrayDark  = [System.Drawing.ColorTranslator]::FromHtml("#2A3136")
    OffWhite  = [System.Drawing.ColorTranslator]::FromHtml("#F3F6F4")
}

function New-RoundedRectanglePath {
    param(
        [System.Drawing.RectangleF]$Rectangle,
        [float]$Radius
    )

    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = $Radius * 2
    $arc = [System.Drawing.RectangleF]::new($Rectangle.X, $Rectangle.Y, $diameter, $diameter)
    $path.AddArc($arc, 180, 90)
    $arc.X = $Rectangle.Right - $diameter
    $path.AddArc($arc, 270, 90)
    $arc.Y = $Rectangle.Bottom - $diameter
    $path.AddArc($arc, 0, 90)
    $arc.X = $Rectangle.X
    $path.AddArc($arc, 90, 90)
    $path.CloseFigure()
    return $path
}

function Draw-Mark {
    param(
        [System.Drawing.Graphics]$Graphics,
        [float]$X,
        [float]$Y,
        [float]$Size
    )

    $rect = [System.Drawing.RectangleF]::new($X, $Y, $Size, $Size)
    $path = New-RoundedRectanglePath -Rectangle $rect -Radius ($Size * 0.24)
    $fill = [System.Drawing.SolidBrush]::new($palette.Ink)
    $Graphics.FillPath($fill, $path)

    $border = [System.Drawing.Pen]::new($palette.Mint, [Math]::Max(2, $Size * 0.035))
    $Graphics.DrawPath($border, $path)

    $letterFont = [System.Drawing.Font]::new("Segoe UI Black", $Size * 0.58, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $letterBrush = [System.Drawing.SolidBrush]::new($palette.Mint)
    $format = [System.Drawing.StringFormat]::new()
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    $textRect = [System.Drawing.RectangleF]::new($X, $Y - ($Size * 0.02), $Size, $Size)
    $Graphics.DrawString("A", $letterFont, $letterBrush, $textRect, $format)

    $fill.Dispose()
    $border.Dispose()
    $letterFont.Dispose()
    $letterBrush.Dispose()
    $format.Dispose()
    $path.Dispose()
}

function Draw-Wordmark {
    param(
        [System.Drawing.Graphics]$Graphics,
        [float]$X,
        [float]$Y,
        [float]$Height,
        [switch]$Stacked,
        [switch]$OnLight
    )

    $fontSize = if ($Stacked) { $Height * 0.24 } else { $Height * 0.34 }
    $font = [System.Drawing.Font]::new("Segoe UI", $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $white = [System.Drawing.SolidBrush]::new($(if ($OnLight) { $palette.Ink } else { $palette.OffWhite }))
    $mint = [System.Drawing.SolidBrush]::new($(if ($OnLight) { $palette.MintDeep } else { $palette.Mint }))
    $gray = [System.Drawing.SolidBrush]::new($(if ($OnLight) { [System.Drawing.ColorTranslator]::FromHtml("#5B6660") } else { $palette.Gray }))

    if ($Stacked) {
        $Graphics.DrawString("A`$PECT", $font, $white, $X, $Y)
        $Graphics.DrawString("UTILS", $font, $mint, $X, $Y + ($fontSize * 1.02))
        $smallFont = [System.Drawing.Font]::new("Segoe UI", $Height * 0.075, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
        $Graphics.DrawString("WINDOWS SETUP UTILITY", $smallFont, $gray, $X, $Y + ($fontSize * 2.25))
        $smallFont.Dispose()
    } else {
        $Graphics.DrawString("A`$PECT", $font, $white, $X, $Y)
        $firstWidth = $Graphics.MeasureString("A`$PECT ", $font).Width
        $Graphics.DrawString("UTILS", $font, $mint, $X + $firstWidth, $Y)
    }

    $font.Dispose()
    $white.Dispose()
    $mint.Dispose()
    $gray.Dispose()
}

# --- Docs nav logos (horizontal wordmark, light and dark variants) ----------------------
$navWidth = 720
$navHeight = 168
foreach ($variant in @(
        @{ Name = "navlogo-dark.png"; OnLight = $false },
        @{ Name = "navlogo-light.png"; OnLight = $true },
        @{ Name = "navlogo.png"; OnLight = $false }
    )) {
    $navBitmap = [System.Drawing.Bitmap]::new($navWidth, $navHeight, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $navGraphics = [System.Drawing.Graphics]::FromImage($navBitmap)
    $navGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $navGraphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $navGraphics.Clear([System.Drawing.Color]::Transparent)
    Draw-Mark -Graphics $navGraphics -X 4 -Y 14 -Size 140
    Draw-Wordmark -Graphics $navGraphics -X 168 -Y 48 -Height 140 -OnLight:$variant.OnLight
    $navFilePath = Join-Path $brandingDir $variant.Name
    $navBitmap.Save($navFilePath, [System.Drawing.Imaging.ImageFormat]::Png)
    $navGraphics.Dispose()
    $navBitmap.Dispose()
    Write-Host "Generated: $navFilePath"
}

# --- Square app mark --------------------------------------------------------------------
$markSize = 512
$markBitmap = [System.Drawing.Bitmap]::new($markSize, $markSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$markGraphics = [System.Drawing.Graphics]::FromImage($markBitmap)
$markGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$markGraphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$markGraphics.Clear([System.Drawing.Color]::Transparent)
Draw-Mark -Graphics $markGraphics -X 16 -Y 16 -Size 480
$markPng = Join-Path $desktopAssets "AspectUtils.png"
$markBitmap.Save($markPng, [System.Drawing.Imaging.ImageFormat]::Png)
$markGraphics.Dispose()
$markBitmap.Dispose()

# WPF accepts a PNG payload wrapped in an ICO container on Windows Vista and later.
$pngBytes = [System.IO.File]::ReadAllBytes($markPng)
$icoPath = Join-Path $desktopAssets "AspectUtils.ico"
$stream = [System.IO.File]::Create($icoPath)
$writer = [System.IO.BinaryWriter]::new($stream)
$writer.Write([UInt16]0)          # reserved
$writer.Write([UInt16]1)          # icon
$writer.Write([UInt16]1)          # image count
$writer.Write([Byte]0)            # width: 256 encoded as 0
$writer.Write([Byte]0)            # height: 256 encoded as 0
$writer.Write([Byte]0)            # palette count
$writer.Write([Byte]0)            # reserved
$writer.Write([UInt16]1)          # color planes
$writer.Write([UInt16]32)         # bits per pixel
$writer.Write([UInt32]$pngBytes.Length)
$writer.Write([UInt32]22)         # image data offset
$writer.Write($pngBytes)
$writer.Dispose()
$stream.Dispose()

# --- Social preview ---------------------------------------------------------------------
$previewWidth = 1200
$previewHeight = 630
$preview = [System.Drawing.Bitmap]::new($previewWidth, $previewHeight)
$previewGraphics = [System.Drawing.Graphics]::FromImage($preview)
$previewGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$previewGraphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$previewGraphics.Clear($palette.Ink)

$glow = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
    [System.Drawing.Rectangle]::new(0, 0, $previewWidth, $previewHeight),
    [System.Drawing.Color]::FromArgb(70, $palette.Mint),
    [System.Drawing.Color]::FromArgb(0, $palette.Mint),
    35
)
$previewGraphics.FillRectangle($glow, 0, 0, $previewWidth, $previewHeight)

Draw-Mark -Graphics $previewGraphics -X 96 -Y 92 -Size 120
Draw-Wordmark -Graphics $previewGraphics -X 244 -Y 108 -Height 100
$subtitleFont = [System.Drawing.Font]::new("Segoe UI", 40, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$subtitleBrush = [System.Drawing.SolidBrush]::new($palette.Gray)
$previewGraphics.DrawString("Windows setup, minus the hassle.", $subtitleFont, $subtitleBrush, 96, 300)
$taglineFont = [System.Drawing.Font]::new("Segoe UI", 24, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$taglineBrush = [System.Drawing.SolidBrush]::new($palette.Mint)
$previewGraphics.DrawString("Install apps  -  Tweak with undo  -  Fix problems  -  Control updates", $taglineFont, $taglineBrush, 96, 380)

$windowRect = [System.Drawing.RectangleF]::new(96, 452, 1008, 128)
$windowPath = New-RoundedRectanglePath -Rectangle $windowRect -Radius 16
$windowBrush = [System.Drawing.SolidBrush]::new($palette.Panel)
$previewGraphics.FillPath($windowBrush, $windowPath)
$windowBorder = [System.Drawing.Pen]::new($palette.GrayDark, 2)
$previewGraphics.DrawPath($windowBorder, $windowPath)
$previewGraphics.FillEllipse([System.Drawing.SolidBrush]::new($palette.Mint), 122, 510, 12, 12)
$previewGraphics.FillEllipse([System.Drawing.SolidBrush]::new($palette.Gray), 144, 510, 12, 12)
$previewGraphics.FillEllipse([System.Drawing.SolidBrush]::new($palette.GrayDark), 166, 510, 12, 12)
$windowTextFont = [System.Drawing.Font]::new("Consolas", 20, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$previewGraphics.DrawString("PS> irm .../AspectUtils.ps1 | iex", $windowTextFont, $subtitleBrush, 200, 500)

$preview.Save((Join-Path $publicDir "social-preview.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$previewGraphics.Dispose()
$preview.Dispose()

# --- Favicon ----------------------------------------------------------------------------
$faviconSize = 64
$faviconBitmap = [System.Drawing.Bitmap]::new($faviconSize, $faviconSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$faviconGraphics = [System.Drawing.Graphics]::FromImage($faviconBitmap)
$faviconGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$faviconGraphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$faviconGraphics.Clear([System.Drawing.Color]::Transparent)
Draw-Mark -Graphics $faviconGraphics -X 2 -Y 2 -Size 60
$faviconStream = [System.IO.MemoryStream]::new()
$faviconBitmap.Save($faviconStream, [System.Drawing.Imaging.ImageFormat]::Png)
$faviconBase64 = [Convert]::ToBase64String($faviconStream.ToArray())
$faviconGraphics.Dispose()
$faviconBitmap.Dispose()
$faviconStream.Dispose()

$faviconSvgPath = Join-Path $publicDir "favicon.svg"
$faviconSvg = @"
<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32">
  <image href="data:image/png;base64,$faviconBase64" width="32" height="32"/>
</svg>
"@
[System.IO.File]::WriteAllText($faviconSvgPath, $faviconSvg, [System.Text.UTF8Encoding]::new($false))

Write-Host "Generated:"
Write-Host "  $brandingDir\navlogo-light.png"
Write-Host "  $brandingDir\navlogo-dark.png"
Write-Host "  $markPng"
Write-Host "  $icoPath"
Write-Host "  $(Join-Path $publicDir 'social-preview.png')"
Write-Host "  $faviconSvgPath"
