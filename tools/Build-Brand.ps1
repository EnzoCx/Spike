param([string]$OverlayPreview = '')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase
$project = Split-Path $PSScriptRoot -Parent
$brand = Join-Path $project 'brand'
$desktopBrand = Join-Path $project 'src/DPSMeter.Desktop/Brand'
[IO.Directory]::CreateDirectory($desktopBrand) | Out-Null
$culture = [Globalization.CultureInfo]::InvariantCulture
$symbolPath = [IO.File]::ReadAllText((Join-Path $brand 'symbol.path')).Trim()
$symbol = [Windows.Media.Geometry]::Parse($symbolPath)
$fontRoot = [Uri]((Join-Path $project 'src/DPSMeter.Desktop/Fonts') + '/')
$body = [Windows.Media.FontFamily]::new($fontRoot, './#Barlow')
$display = [Windows.Media.FontFamily]::new($fontRoot, './#Barlow Condensed SemiBold')
$faceCheck = [Windows.Media.Typeface]::new($display, [Windows.FontStyles]::Normal, [Windows.FontWeights]::SemiBold, [Windows.FontStretches]::Normal)
$resolvedFace = $null
if (-not $faceCheck.TryGetGlyphTypeface([ref]$resolvedFace) -or $resolvedFace.FontUri.ToString() -notmatch 'BARLOWCONDENSED-SEMIBOLD') {
    throw 'The bundled Barlow Condensed SemiBold face did not resolve; refusing a fallback logo.'
}
function Brush([string]$color) { [Windows.Media.BrushConverter]::new().ConvertFromString($color) }
function TextShape([string]$text, [double]$size, [bool]$condensed = $false, [string]$color = '#F3F0E8') {
    $family = if ($condensed) { $display } else { $body }
    $weight = if ($condensed) { [Windows.FontWeights]::SemiBold } else { [Windows.FontWeights]::Normal }
    $face = [Windows.Media.Typeface]::new($family, [Windows.FontStyles]::Normal, $weight, [Windows.FontStretches]::Normal)
    [Windows.Media.FormattedText]::new($text, $culture, [Windows.FlowDirection]::LeftToRight, $face, $size, (Brush $color), 1.0)
}
function SavePng($visual, [int]$width, [int]$height, [string]$path) {
    $bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new($width, $height, 96, 96, [Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = [IO.File]::Create($path)
    try { $encoder.Save($stream) } finally { $stream.Dispose() }
}
function DrawMark($dc, [double]$x, [double]$y, [double]$size, [string]$color) {
    $dc.PushTransform([Windows.Media.TranslateTransform]::new($x, $y))
    $dc.PushTransform([Windows.Media.ScaleTransform]::new($size / 64, $size / 64))
    $dc.DrawGeometry((Brush $color), $null, $symbol)
    $dc.Pop(); $dc.Pop()
}
function Label($dc, [string]$text, [double]$x, [double]$y, [double]$size = 16, [string]$color = '#B1B1A5', [bool]$condensed = $false) {
    $dc.DrawText((TextShape $text $size $condensed $color), [Windows.Point]::new($x, $y))
}

# SVG lockups use outlined glyphs, so no font install is needed to display them.
$word = (TextShape 'Spike' 64 $true).BuildGeometry([Windows.Point]::new(0, 0))
$outline = $word.GetOutlinedPathGeometry()
$wordPath = $outline.ToString($culture) -replace '^F[01]', ''
$fillRule = if ($outline.FillRule -eq [Windows.Media.FillRule]::Nonzero) { 'nonzero' } else { 'evenodd' }
if ($wordPath -notmatch '^\s*[Mm]' -or $wordPath.Contains('System.')) { throw 'Invalid vector wordmark export.' }
$tx = (82 - $word.Bounds.X).ToString($culture)
$ty = ((64 - $word.Bounds.Height) / 2 - $word.Bounds.Y).ToString($culture)
$lockupWidth = [Math]::Ceiling(82 + $word.Bounds.Width + 8)
foreach ($variant in @(
    @{ Name = 'dark'; Symbol = '#DDA66A'; Text = '#F3F0E8' },
    @{ Name = 'light'; Symbol = '#85501F'; Text = '#242720' },
    @{ Name = 'mono'; Symbol = 'currentColor'; Text = 'currentColor' }
)) {
    $svg = @"
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 $lockupWidth 64" role="img" aria-label="Spike"><title>Spike</title><path fill="$($variant.Symbol)" d="$symbolPath"/><path fill="$($variant.Text)" fill-rule="$fillRule" transform="translate($tx $ty)" d="$wordPath"/></svg>
"@
    [IO.File]::WriteAllText((Join-Path $brand "logo-$($variant.Name).svg"), $svg)
    [IO.File]::WriteAllText((Join-Path $brand "symbol-$($variant.Name).svg"), "<svg xmlns=`"http://www.w3.org/2000/svg`" viewBox=`"0 0 64 64`" role=`"img`" aria-label=`"Spike`"><title>Spike</title><path fill=`"$($variant.Symbol)`" d=`"$symbolPath`"/></svg>")
    $symbolColor = if ($variant.Name -eq 'mono') { '#242720' } else { $variant.Symbol }
    $textColor = if ($variant.Name -eq 'mono') { '#242720' } else { $variant.Text }
    $visual = [Windows.Media.DrawingVisual]::new(); $dc = $visual.RenderOpen()
    $dc.PushTransform([Windows.Media.ScaleTransform]::new(4, 4))
    DrawMark $dc 0 0 64 $symbolColor
    $dc.PushTransform([Windows.Media.TranslateTransform]::new([double]::Parse($tx, $culture), [double]::Parse($ty, $culture)))
    $dc.DrawGeometry((Brush $textColor), $null, $word)
    $dc.Pop(); $dc.Pop(); $dc.Close()
    SavePng $visual ($lockupWidth * 4) 256 (Join-Path $brand "logo-$($variant.Name).png")
}
$visual = [Windows.Media.DrawingVisual]::new(); $dc = $visual.RenderOpen()
DrawMark $dc 0 0 1024 '#DDA66A'; $dc.Close()
SavePng $visual 1024 1024 (Join-Path $brand 'symbol-transparent.png')

# Multi-resolution Windows icon: PNG frames preserve sharp edges and alpha.
$frames = @()
foreach ($size in @(16, 24, 32, 48, 64, 128, 256)) {
    $visual = [Windows.Media.DrawingVisual]::new(); $dc = $visual.RenderOpen()
    $dc.DrawRoundedRectangle((Brush '#191A18'), $null, [Windows.Rect]::new(0, 0, $size, $size), $size * 0.16, $size * 0.16)
    DrawMark $dc ($size * 0.06) ($size * 0.06) ($size * 0.88) '#DDA66A'; $dc.Close()
    $path = Join-Path $brand "icon-$size.png"
    SavePng $visual $size $size $path
    $frames += ,@{ Size = $size; Bytes = [IO.File]::ReadAllBytes($path) }
}
$icoPath = Join-Path $brand 'dpsmeter.ico'
$stream = [IO.File]::Create($icoPath); $writer = [IO.BinaryWriter]::new($stream)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
    $offset = 6 + 16 * $frames.Count
    foreach ($frame in $frames) {
        $dimension = if ($frame.Size -eq 256) { 0 } else { $frame.Size }
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension); $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$frame.Bytes.Length); $writer.Write([uint32]$offset)
        $offset += $frame.Bytes.Length
    }
    foreach ($frame in $frames) { $writer.Write([byte[]]$frame.Bytes) }
} finally { $writer.Dispose() }
Copy-Item -LiteralPath $icoPath -Destination (Join-Path $desktopBrand 'dpsmeter.ico') -Force

$visual = [Windows.Media.DrawingVisual]::new(); $dc = $visual.RenderOpen()
$dc.DrawRectangle((Brush '#191A18'), $null, [Windows.Rect]::new(0, 0, 1280, 640))
Label $dc 'AION 2  /  COMBAT ANALYTICS' 72 62 18 '#B1B1A5'
DrawMark $dc 44 195 160 '#DDA66A'
Label $dc 'Spike' 230 176 132 '#F3F0E8' $true
Label $dc 'Le combat, en clair.' 236 336 32 '#B1B1A5'
$dc.DrawLine([Windows.Media.Pen]::new((Brush '#3D4038'), 1), [Windows.Point]::new(72, 488), [Windows.Point]::new(1208, 488))
Label $dc 'OVERLAY   /   HISTORIQUE   /   ANALYSE' 72 534 22 '#DDA66A'
$dc.Close(); SavePng $visual 1280 640 (Join-Path $brand 'social-cover.png')

# Static art-direction sheet. The optional overlay is an unchanged app render.
$visual = [Windows.Media.DrawingVisual]::new(); $dc = $visual.RenderOpen()
$dc.DrawRectangle((Brush '#191A18'), $null, [Windows.Rect]::new(0, 0, 1600, 1100))
$dc.DrawRectangle((Brush '#F3F0E8'), $null, [Windows.Rect]::new(1080, 0, 520, 1100))
Label $dc 'SPIKE  /  IDENTITÉ VISUELLE' 64 46 15
Label $dc '01 — INSTRUMENT DE COMBAT' 64 114 14 '#DDA66A'
DrawMark $dc 44 194 160 '#DDA66A'
Label $dc 'Spike' 230 188 108 '#F3F0E8' $true
Label $dc 'Le combat, en clair.' 237 325 27 '#B1B1A5'
$dc.DrawLine([Windows.Media.Pen]::new((Brush '#3D4038'), 1), [Windows.Point]::new(64, 421), [Windows.Point]::new(1016, 421))
Label $dc 'UN SIGNE ISSU DE L''INTERFACE' 64 458 15 '#DDA66A'
Label $dc 'Trois barres. Un S.' 64 496 60 '#F3F0E8' $true
Label $dc 'Le classement devient le symbole de l''application.' 64 579 23 '#B1B1A5'
Label $dc 'Une silhouette franche, lisible dans un overlay comme dans la barre des tâches.' 64 614 20 '#B1B1A5'
Label $dc '02 — TYPOGRAPHIE' 64 721 14 '#DDA66A'
Label $dc 'BARLOW CONDENSED' 64 760 34 '#F3F0E8' $true
Label $dc 'Titres courts et signature' 64 810 18 '#B1B1A5'
Label $dc 'Barlow' 588 760 32 '#F3F0E8'
Label $dc 'Joueurs, compétences, chiffres' 588 810 18 '#B1B1A5'
Label $dc '03 — DU SYMBOLE À L''ICÔNE' 64 907 14 '#DDA66A'
foreach ($entry in @(@(64, 64), @(180, 48), @(280, 32), @(364, 24), @(440, 16))) {
    DrawMark $dc $entry[0] 953 $entry[1] '#DDA66A'
}
Label $dc 'Même tracé à toutes les tailles.' 588 967 20 '#B1B1A5'
Label $dc 'PALETTE' 1124 46 15 '#5F6257'
$swatches = @(@('Graphite', '#191A18'), @('Bronze', '#DDA66A'), @('Ivoire', '#F3F0E8'))
for ($i = 0; $i -lt 3; $i++) {
    $x = 1124 + $i * 142
    $dc.DrawRectangle((Brush $swatches[$i][1]), [Windows.Media.Pen]::new((Brush '#C7C9BE'), 1), [Windows.Rect]::new($x, 96, 124, 80))
    Label $dc $swatches[$i][0] $x 188 18 '#242720'
    Label $dc $swatches[$i][1] $x 217 15 '#5F6257'
}
DrawMark $dc 1124 290 52 '#85501F'
Label $dc 'Spike' 1198 281 48 '#242720' $true
Label $dc 'Déclinaison claire / même identité' 1124 362 18 '#5F6257'
Label $dc 'DANS L''OVERLAY' 1124 453 15 '#5F6257'
if ($OverlayPreview -and (Test-Path -LiteralPath $OverlayPreview)) {
    $bitmap = [Windows.Media.Imaging.BitmapImage]::new([Uri]$OverlayPreview)
    $dc.DrawImage($bitmap, [Windows.Rect]::new(1124, 500, 390, 420))
    Label $dc 'Interface réelle · combat enregistré' 1124 944 18 '#5F6257'
} else {
    Label $dc 'Barres de classe, chiffres lisibles,' 1124 512 20 '#242720'
    Label $dc 'bronze réservé aux commandes.' 1124 545 20 '#242720'
}
Label $dc 'SPIKE  /  LOCAL FIRST' 64 1061 12 '#B1B1A5'
Label $dc 'DIRECTION 01  ·  OCTOBRE 2026' 1124 1061 12 '#5F6257'
$dc.Close(); SavePng $visual 1600 1100 (Join-Path $brand 'direction-graphique.png')
Write-Output 'Brand kit generated: SVG lockups, transparent PNG, 7 icon sizes, Windows ICO and art-direction sheet.'
