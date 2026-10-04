param()
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
Push-Location $project
try {
    function Invoke-Dotnet([string[]]$Arguments) {
        & dotnet @Arguments
        if ($LASTEXITCODE -ne 0) { throw "dotnet failed: $($Arguments -join ' ')" }
    }
    Invoke-Dotnet @('restore', 'DPSMeter.sln', '--locked-mode')
    Invoke-Dotnet @('build', 'DPSMeter.sln', '-c', 'Release', '--no-restore', '-m:1')
    Invoke-Dotnet @('run', '--project', 'tests/DPSMeter.Checks', '-c', 'Release', '--no-build')
    Invoke-Dotnet @('run', '--project', 'tests/DPSMeter.EngineChecks', '-c', 'Release', '--no-build')
    & (Join-Path $PSScriptRoot 'Verify-Updates.ps1')
    Invoke-Dotnet @('publish', 'src/DPSMeter.Desktop', '-c', 'Release', '--no-restore', '-o', 'artifacts/windows', '-m:1')
    $previewPath = Join-Path $project 'artifacts/verification'
    $executable = Join-Path $project 'artifacts/windows/DPSMeter.exe'
    # Hidden verifier only: no capture, desktop input or existing process termination.
    $process = Start-Process -FilePath $executable -ArgumentList "--verify-views `"$previewPath`"" -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0) {
        $failure = Join-Path $previewPath 'failure.txt'
        if (Test-Path -LiteralPath $failure) { Get-Content -LiteralPath $failure }
        throw "UI verification failed with exit code $($process.ExitCode)."
    }
    Get-Content -LiteralPath (Join-Path $previewPath 'result.txt')
    Get-Content -LiteralPath (Join-Path $previewPath 'idle-fade-result.txt')
    Get-Content -LiteralPath (Join-Path $previewPath 'placement-result.txt')
} finally {
    Pop-Location
}
