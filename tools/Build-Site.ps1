param()
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$destination = Join-Path $project 'artifacts/site'
$assets = Join-Path $destination 'assets'
[IO.Directory]::CreateDirectory((Join-Path $assets 'Fonts')) | Out-Null
# Explicit public allowlist: never publish the repository or a whole verification directory.
foreach ($name in @('index.html', 'style.css', 'app.js')) {
    Copy-Item -LiteralPath (Join-Path $project "site/$name") -Destination $destination -Force
}
foreach ($name in @('symbol-dark.svg', 'symbol-light.svg', 'icon-32.png', 'social-cover.png', 'tokens.css')) {
    Copy-Item -LiteralPath (Join-Path $project "brand/$name") -Destination $assets -Force
}
foreach ($name in @('Barlow-Regular.ttf', 'BarlowCondensed-SemiBold.ttf', 'Barlow-OFL.txt', 'BarlowCondensed-OFL.txt')) {
    Copy-Item -LiteralPath (Join-Path $project "src/Spike.Desktop/Fonts/$name") -Destination (Join-Path $assets 'Fonts') -Force
}
foreach ($name in @('LICENSE', 'THIRD-PARTY-NOTICES.md')) {
    Copy-Item -LiteralPath (Join-Path $project $name) -Destination $assets -Force
}
foreach ($theme in @('dark', 'light', 'contrast')) {
    foreach ($view in @('overlay', 'report', 'checklist', 'events')) {
        $name = "$view-en-$theme.png"
        Copy-Item -LiteralPath (Join-Path $project "site/images/$name") -Destination $assets -Force
    }
}
# Tie CSS and JavaScript URLs to their contents so returning visitors get matching assets.
$indexPath = Join-Path $destination 'index.html'
$index = [IO.File]::ReadAllText($indexPath)
foreach ($name in @('assets/tokens.css', 'style.css', 'app.js')) {
    $hash = (Get-FileHash -LiteralPath (Join-Path $destination $name) -Algorithm SHA256).Hash.Substring(0, 12).ToLowerInvariant()
    $index = $index.Replace('"' + $name + '"', '"' + $name + '?v=' + $hash + '"')
}
[IO.File]::WriteAllText($indexPath, $index)
[IO.File]::WriteAllText((Join-Path $destination '.nojekyll'), '')
Write-Output "Static site assembled in artifacts/site (public assets only)."
