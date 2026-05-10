# Golden-output validation harness for WinRT.Projection.Writer.
#
# Modes:
#   .\validate-writer-output.ps1 -Mode capture              # capture baseline manifests for the configured scenarios
#   .\validate-writer-output.ps1 -Mode validate             # run every scenario, compare hashes, exit non-zero on drift
#   .\validate-writer-output.ps1 -Mode capture-and-validate # capture if missing; otherwise validate, overwrite on drift
#
# Per-scenario manifests are stored under: $PSScriptRoot\baselines\<scenario>.sha256
# Each scenario is described by an .rsp response file (the same format the TestRunner accepts);
# the .rsp files live under $RspRoot and select the input metadata + output directory for one
# regen scenario. The harness loads all .rsp files under $RspRoot whose name (without extension)
# matches one of the configured -Scenarios.
#
# This script is environment-portable: -RepoRoot defaults to the repo root inferred from the
# script's own location, and -RspRoot defaults to a sibling 'rsp' directory under -RepoRoot.
# Override either one when calling the script if your local layout differs.

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('capture','validate','capture-and-validate')]
    [string]$Mode,
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path,
    [string]$RspRoot = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path 'eng\rsp'),
    [string[]]$Scenarios,
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$baselineDir = Join-Path $PSScriptRoot 'baselines'
if (-not (Test-Path $baselineDir)) { New-Item -ItemType Directory -Path $baselineDir | Out-Null }

# Auto-discover scenarios from the .rsp directory if none were explicitly listed.
if (-not $Scenarios) {
    if (-not (Test-Path $RspRoot)) {
        throw "RspRoot '$RspRoot' does not exist. Pass -RspRoot <dir> pointing at a folder of .rsp files, or -Scenarios <names>."
    }
    $Scenarios = Get-ChildItem $RspRoot -Filter '*.rsp' | Sort-Object Name | ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_.Name) }
    if (-not $Scenarios) {
        throw "No .rsp files found under '$RspRoot'."
    }
}

# Locate the TestRunner exe.
$runner = "$RepoRoot\src\WinRT.Projection.Writer.TestRunner\bin\$Configuration\net10.0\WinRT.Projection.Writer.TestRunner.exe"
if (-not (Test-Path $runner)) {
    Write-Host 'TestRunner exe not found; building...' -ForegroundColor Yellow
    $proj = "$RepoRoot\src\WinRT.Projection.Writer.TestRunner\WinRT.Projection.Writer.TestRunner.csproj"
    if (-not (Test-Path $proj)) { throw "TestRunner csproj not found at '$proj'." }
    $buildLog = & dotnet build $proj -c $Configuration --nologo 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'TestRunner build failed:' -ForegroundColor Red
        $buildLog | Select-Object -Last 25 | ForEach-Object { Write-Host $_ }
        throw 'TestRunner build failed.'
    }
    if (-not (Test-Path $runner)) { throw "TestRunner exe still not found at '$runner' after build." }
}

Write-Host "TestRunner: $runner" -ForegroundColor Cyan

function Get-ManifestForScenario {
    param([string]$ScenarioName)
    $rsp = Join-Path $RspRoot "$ScenarioName.rsp"
    if (-not (Test-Path $rsp)) { return $null }
    $outDir = (((Get-Content $rsp -Raw) -split "`r?`n") | Where-Object { $_ -match '^--output-directory' } | Select-Object -First 1) -replace '^--output-directory ', ''
    $outDir = $outDir.Trim()
    if (-not (Test-Path $outDir)) { return $null }
    $sb = [System.Text.StringBuilder]::new()
    Get-ChildItem "$outDir\*.cs" | Sort-Object Name | ForEach-Object {
        $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash
        [void]$sb.Append($hash).Append(' ').AppendLine($_.Name)
    }
    return $sb.ToString()
}

function Run-Scenario {
    param([string]$ScenarioName)
    $rsp = Join-Path $RspRoot "$ScenarioName.rsp"
    if (-not (Test-Path $rsp)) { Write-Host "SKIP: $ScenarioName ($rsp not found)" -ForegroundColor Yellow; return $false }
    $output = & $runner 'rsp' $rsp 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "$ScenarioName : EXEC FAILED (exit=$LASTEXITCODE)" -ForegroundColor Red
        $output | Select-Object -Last 5 | ForEach-Object { Write-Host "  $_" }
        return $false
    }
    return $true
}

$anyFailure = $false

foreach ($scenario in $Scenarios) {
    $rsp = Join-Path $RspRoot "$scenario.rsp"
    if (-not (Test-Path $rsp)) { Write-Host "SKIP: $scenario (rsp missing)" -ForegroundColor Yellow; continue }

    $baselinePath = Join-Path $baselineDir "$scenario.sha256"

    if ($Mode -eq 'capture') {
        if (-not (Run-Scenario $scenario)) { $anyFailure = $true; continue }
        $manifest = Get-ManifestForScenario $scenario
        if ($manifest -eq $null) { Write-Host "$scenario : no output dir after run" -ForegroundColor Red; $anyFailure = $true; continue }
        Set-Content -Path $baselinePath -Value $manifest -NoNewline -Encoding utf8NoBOM
        $count = ($manifest.TrimEnd("`r`n").Split("`n") | Measure-Object).Count
        Write-Host ("{0,-40} CAPTURED  files={1}" -f $scenario, $count) -ForegroundColor Green
    }
    elseif ($Mode -eq 'validate') {
        if (-not (Test-Path $baselinePath)) { Write-Host "$scenario : NO BASELINE (run -Mode capture first)" -ForegroundColor Red; $anyFailure = $true; continue }
        if (-not (Run-Scenario $scenario)) { $anyFailure = $true; continue }
        $current = Get-ManifestForScenario $scenario
        $baseline = Get-Content -Path $baselinePath -Raw
        if ($current -eq $baseline) {
            $count = ($current.TrimEnd("`r`n").Split("`n") | Measure-Object).Count
            Write-Host ("{0,-40} OK         files={1}" -f $scenario, $count) -ForegroundColor Green
        } else {
            Write-Host ("{0,-40} DRIFT" -f $scenario) -ForegroundColor Red
            $baseLines = $baseline.Split("`n") | Where-Object { $_ }
            $curLines = $current.Split("`n") | Where-Object { $_ }
            $bMap = @{}; $cMap = @{}
            foreach ($l in $baseLines) { $i = $l.IndexOf(' '); $bMap[$l.Substring($i+1).Trim()] = $l.Substring(0,$i) }
            foreach ($l in $curLines) { $i = $l.IndexOf(' '); $cMap[$l.Substring($i+1).Trim()] = $l.Substring(0,$i) }
            $allFiles = ($bMap.Keys + $cMap.Keys) | Sort-Object -Unique
            $shown = 0
            foreach ($f in $allFiles) {
                if ($shown -ge 25) { Write-Host "  ... (more drifted files omitted)" -ForegroundColor DarkGray; break }
                $bh = $bMap[$f]; $ch = $cMap[$f]
                if (-not $bh) { Write-Host "  + $f (added)" -ForegroundColor Green; $shown++ }
                elseif (-not $ch) { Write-Host "  - $f (removed)" -ForegroundColor Red; $shown++ }
                elseif ($bh -ne $ch) { Write-Host "  ~ $f (changed)" -ForegroundColor Yellow; $shown++ }
            }
            $anyFailure = $true
        }
    }
    elseif ($Mode -eq 'capture-and-validate') {
        if (-not (Run-Scenario $scenario)) { $anyFailure = $true; continue }
        $manifest = Get-ManifestForScenario $scenario
        if (-not (Test-Path $baselinePath)) {
            Set-Content -Path $baselinePath -Value $manifest -NoNewline -Encoding utf8NoBOM
            Write-Host ("{0,-40} CAPTURED (no prior baseline)" -f $scenario) -ForegroundColor Green
        } else {
            $baseline = Get-Content -Path $baselinePath -Raw
            if ($manifest -eq $baseline) {
                Write-Host ("{0,-40} OK" -f $scenario) -ForegroundColor Green
            } else {
                Set-Content -Path $baselinePath -Value $manifest -NoNewline -Encoding utf8NoBOM
                Write-Host ("{0,-40} UPDATED (drift detected, baseline overwritten)" -f $scenario) -ForegroundColor Yellow
            }
        }
    }
}

if ($anyFailure) { exit 1 } else { exit 0 }
