param(
    [Parameter(Mandatory)]
    [ValidateSet('jit', 'aot')]
    [string] $Runtime,
    [Parameter(Mandatory)]
    [string] $PackageVersion
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$src = "$repoRoot\src"
$previousTestRuntime = $env:cswinrt_unit_test_runtime
try {
    $env:cswinrt_unit_test_runtime = $Runtime
    & $env:ComSpec /d /c "call `"$src\build.cmd`" x64 Release 3.0.0.0 $PackageVersion 3.0.0.0 product"
    if ($LASTEXITCODE -ne 0) {
        throw "UnitTest ($Runtime) build failed (exit code $LASTEXITCODE)."
    }
} finally {
    $env:cswinrt_unit_test_runtime = $previousTestRuntime
}
if ($Runtime -eq 'aot') {
    $testExecutable = "$src\Tests\UnitTest\bin\x64\Release\net10.0\win-x64\publish\UnitTest.exe"
} else {
    $testExecutable = "$src\Tests\UnitTest\bin\x64\Release\net10.0\win-x64\UnitTest.exe"
}

$results = "$repoRoot\artifacts\test-results\$Runtime"
New-Item -ItemType Directory -Force $results | Out-Null
$trxPath = "$results\UnitTest-$Runtime.trx"
# Require a fresh report so a previous successful run cannot mask missing results
if (Test-Path -LiteralPath $trxPath) {
    Remove-Item -LiteralPath $trxPath
}
Push-Location (Split-Path $testExecutable)
try {
    & $testExecutable --results-directory $results --report-trx --report-trx-filename "UnitTest-$Runtime.trx"
    if ($LASTEXITCODE -ne 0) {
        throw "UnitTest ($Runtime) failed (exit code $LASTEXITCODE)."
    }
} finally {
    Pop-Location
}

# A zero exit code also allows an entirely skipped run
[xml] $report = Get-Content -LiteralPath $trxPath -Raw
if ([int] $report.TestRun.ResultSummary.Counters.passed -le 0) {
    throw "UnitTest ($Runtime) did not pass any tests. An empty or entirely skipped run is not a successful validation."
}
