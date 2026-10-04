param()
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$harness = Join-Path $project 'tests/DPSMeter.UpdateHarness/DPSMeter.UpdateHarness.csproj'
$buildRoot = Join-Path $project 'artifacts/update-harness'
$runRoot = Join-Path $buildRoot ([Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null

function Invoke-Dotnet([string[]]$Arguments) {
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw 'Update harness build failed.' }
}
function Invoke-Hidden([string]$Executable, [string]$Arguments = '') {
    $options = @{ FilePath = $Executable; WindowStyle = 'Hidden'; PassThru = $true; Wait = $true }
    if ($Arguments) { $options.ArgumentList = $Arguments }
    $process = Start-Process @options
    if ($process.ExitCode -ne 0) { throw "Update fixture failed: $($process.ExitCode)" }
}
function Wait-Started([string]$Target, [string]$Version) {
    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    do {
        if (Test-Path -LiteralPath "$Target.started") {
            $actual = Get-Content -LiteralPath "$Target.started" -Raw
            if ($actual -eq $Version) { return }
            if ($actual) { throw "Expected $Version but started $actual" }
        }
        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $deadline)
    throw 'Updated fixture did not restart.'
}

Invoke-Dotnet @('restore', $harness, '--locked-mode')
foreach ($version in @('0.4.6', '0.4.7')) {
    Invoke-Dotnet @('publish', $harness, '-c', 'Release', '--no-restore', "-p:Version=$version", '-o', (Join-Path $buildRoot $version))
}
$old = Join-Path $buildRoot '0.4.6/DPSMeter.UpdateHarness.exe'
$new = Join-Path $buildRoot '0.4.7/DPSMeter.UpdateHarness.exe'
$targets = @()
try {
    foreach ($scenario in @('replace', 'locked')) {
        # Include spaces to exercise real process argument quoting.
        $folder = Join-Path $runRoot "$scenario with spaces"
        New-Item -ItemType Directory -Path $folder | Out-Null
        $target = Join-Path $folder 'Meter fixture.exe'
        $targets += $target
        Copy-Item -LiteralPath $old -Destination $target
        Invoke-Hidden $old "--stage `"$target`" `"$new`""
        $held = $null
        try {
            if ($scenario -eq 'locked') {
                $held = [IO.File]::Open($target, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
            }
            Invoke-Hidden $target
            if ($scenario -eq 'replace') {
                Wait-Started $target '0.4.7.0'
                if ((Get-FileHash -LiteralPath "$target.previous").Hash -ne (Get-FileHash -LiteralPath $old).Hash) {
                    throw 'Original binary was not preserved.'
                }
                Write-Output 'PASS: real helper waits for exit, replaces EXE, keeps backup and launches newer version (paths with spaces).'
            } else {
                Wait-Started $target '0.4.6.0'
                Write-Output 'PASS: locked target restarts old version without a restart loop or forced termination.'
            }
        } finally { if ($held) { $held.Dispose() } }
        if ($scenario -eq 'locked') {
            Remove-Item -LiteralPath "$target.started"
            Invoke-Hidden $target
            Wait-Started $target '0.4.7.0'
            Write-Output 'PASS: a later launch installs the pending update after the file lock is released.'
        }
    }
} finally {
    foreach ($target in $targets) {
        if (!(Test-Path -LiteralPath "$target.cache")) { continue }
        $cache = [IO.Path]::GetFullPath((Get-Content -LiteralPath "$target.cache" -Raw))
        $allowed = [IO.Path]::GetFullPath((Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'DPSMeter/updates'))
        if ([IO.Path]::GetDirectoryName($cache) -ne $allowed -or [IO.Path]::GetFileName($cache) -notmatch '^[A-F0-9]{64}$') {
            throw 'Refusing cleanup outside the fixture update cache.'
        }
        for ($attempt = 0; $attempt -lt 30; $attempt++) {
            try { Remove-Item -LiteralPath $cache -Recurse -Force; break }
            catch { if ($attempt -eq 29) { throw }; Start-Sleep -Milliseconds 100 }
        }
    }
}
