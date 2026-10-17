[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('MSTest', 'Functional', 'Merge')]
    [string] $Mode,

    [Parameter(Mandatory = $true)]
    [string] $OutputDir,

    [ValidatePattern('^[A-Za-z][A-Za-z0-9]*$')]
    [string] $Name,

    [switch] $RcwFirst
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)
$rawDirectory = Join-Path $OutputDir 'raw'
$settings = Join-Path $root 'build\CodeCoverage.config'
New-Item -ItemType Directory -Force $rawDirectory | Out-Null

if ($Mode -eq 'Merge') {
    if ($Name -or $RcwFirst) {
        throw 'Merge does not accept a test name or -RcwFirst.'
    }

    $files = @(Get-ChildItem -LiteralPath $rawDirectory -Filter '*.coverage' -File)
    if ($files.Count -eq 0) {
        throw "No coverage reports found in $rawDirectory"
    }

    $report = Join-Path $OutputDir 'merged.cobertura.xml'
    if (Test-Path -LiteralPath $report) {
        Remove-Item -LiteralPath $report
    }

    Push-Location $root
    try {
        & dotnet tool run dotnet-coverage merge @($files.FullName) --output $report --output-format cobertura | Out-Host
        if ($LASTEXITCODE -ne 0) {
            throw "Coverage merge failed with exit code $LASTEXITCODE"
        }
    } finally {
        Pop-Location
    }

    [xml] $coverage = Get-Content -LiteralPath $report -Raw
    if ([long] $coverage.coverage.'lines-valid' -le 0 -or [long] $coverage.coverage.'lines-covered' -le 0) {
        throw "Merged report contains no covered product source lines: $report"
    }

    Write-Host "Merged $($files.Count) reports: $($coverage.coverage.'lines-covered')/$($coverage.coverage.'lines-valid') product source lines covered."
    exit 0
}

if (-not $Name) {
    throw "$Mode requires a test name."
}
if ($RcwFirst -and ($Mode -ne 'Functional' -or $Name -ne 'NativeExposedType')) {
    throw '-RcwFirst is only supported for the NativeExposedType functional test.'
}

if ($Mode -eq 'MSTest') {
    $framework = if ($Name -eq 'ObjectLifetimeTests') { 'net10.0-windows10.0.26100.1' } else { 'net10.0' }
    $assembly = if ($Name -eq 'ObjectLifetimeTests') { 'ObjectLifetimeTests.Lifted' } else { $Name }
    $layout = Join-Path $root "src\Tests\$Name\bin\x64\Release\$framework\win-x64"
} else {
    $assembly = $Name
    $layout = Join-Path $root "src\Tests\FunctionalTests\$Name\bin\Release\net10.0\win-x64\coverage"
}

$executable = Join-Path $layout "$assembly.exe"
$managedAssembly = Join-Path $layout "$assembly.dll"
foreach ($file in @($executable, $managedAssembly)) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        throw "JIT test output not found: $file"
    }
}

$suffix = if ($RcwFirst) { '-rcw-first' } else { '' }
$report = Join-Path $rawDirectory "$Mode-$Name$suffix.coverage"
if (Test-Path -LiteralPath $report) {
    throw "Coverage output already exists; use a fresh output directory: $report"
}

Push-Location $layout
try {
    if ($Mode -eq 'MSTest') {
        $results = Join-Path $OutputDir "trx\$Name"
        & $executable --coverage --coverage-settings $settings --coverage-output $report --coverage-output-format coverage `
            --report-trx --results-directory $results | Out-Host
        $expectedExitCode = 0
    } else {
        # The command-string form preserves target arguments through the collector's parser.
        $command = "`"$executable`""
        if ($RcwFirst) {
            $command += ' --rcw-first'
        }
        & dotnet tool run dotnet-coverage collect --settings $settings --output $report --output-format coverage `
            $command | Out-Host
        $expectedExitCode = 100
    }

    if ($LASTEXITCODE -ne $expectedExitCode) {
        throw "$Mode $Name$suffix failed with exit code $LASTEXITCODE (expected $expectedExitCode)."
    }
    if (-not (Test-Path -LiteralPath $report -PathType Leaf) -or (Get-Item -LiteralPath $report).Length -eq 0) {
        throw "Coverage collection did not produce a report: $report"
    }
} finally {
    Pop-Location
}

exit 0
