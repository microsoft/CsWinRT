[CmdletBinding()]
param(
    # The 'win-<plat>' output folder of ObjectLifetimeTests.Lifted.
    [Parameter(Mandatory = $true)] [string] $LayoutDir,

    # Optional folder for MTP results and the TRX report.
    [string] $OutputDir,

    [ValidateSet('None', 'MSIX')] [string] $WindowsPackageType = 'None',

    # Run the Native AOT executable from the layout's publish folder.
    [switch] $NativeAot,

    # Preserve startup logs and probe standalone startup if the MTP invocation fails.
    [switch] $DiagnoseStartup,

    [ValidateRange(1, 3600)] [int] $DiagnosticTimeoutSeconds = 120
)

$ErrorActionPreference = 'Stop'

$layout = Get-Item -LiteralPath $LayoutDir
if ($layout.Name -notmatch '^win-(x86|x64|arm64)$') {
    throw "Expected a win-<architecture> output directory, got $($layout.FullName)"
}
$platform = $Matches[1]
$targetFramework = $layout.Parent.Name
$configuration = $layout.Parent.Parent.Name
$executableDirectory = if ($NativeAot) { Join-Path $layout.FullName 'publish' } else { $layout.FullName }
$executable = Join-Path $executableDirectory 'ObjectLifetimeTests.Lifted.exe'
if (-not (Test-Path -LiteralPath $executable)) {
    throw "Object Lifetime test executable not found at $executable"
}

if (-not $OutputDir) {
    $OutputDir = Join-Path $executableDirectory 'TestResults'
}
$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)
New-Item -ItemType Directory -Force $OutputDir | Out-Null
$project = Join-Path $PSScriptRoot '..\..\src\Tests\ObjectLifetimeTests\ObjectLifetimeTests.Lifted.csproj'
$targetPathProperty = if ($WindowsPackageType -eq 'None') {
    "/p:TestingPlatformExecutablePath=$executable"
} else {
    "/p:TestingPlatformPackagedAppTargetPath=$executable"
}

$diagnosticDirectory = Join-Path $OutputDir 'diagnostics'
$savedEnvironment = @{}
if ($DiagnoseStartup) {
    New-Item -ItemType Directory -Force $diagnosticDirectory | Out-Null
    $diagnosticEnvironment = @{
        TESTINGPLATFORM_DIAGNOSTIC = '1'
        TESTINGPLATFORM_DIAGNOSTIC_VERBOSITY = 'Trace'
        TESTINGPLATFORM_DIAGNOSTIC_SYNCHRONOUS_WRITE = '1'
        TESTINGPLATFORM_DIAGNOSTIC_OUTPUT_DIRECTORY = $diagnosticDirectory
    }
    foreach ($name in $diagnosticEnvironment.Keys) {
        $savedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name)
        [Environment]::SetEnvironmentVariable($name, $diagnosticEnvironment[$name])
    }
}

function Invoke-StartupProbe {
    param([string] $FilePath, [string[]] $Arguments, [string] $Name)

    $stdout = Join-Path $diagnosticDirectory "$Name.stdout.txt"
    $stderr = Join-Path $diagnosticDirectory "$Name.stderr.txt"
    $process = Start-Process -FilePath $FilePath -ArgumentList $Arguments `
        -WorkingDirectory $executableDirectory -RedirectStandardOutput $stdout `
        -RedirectStandardError $stderr -PassThru
    try {
        # Windows PowerShell needs the handle cached to retain the exit code after WaitForExit.
        $null = $process.Handle
        if (-not $process.WaitForExit($DiagnosticTimeoutSeconds * 1000)) {
            if ($Name -eq 'procdump') {
                Get-CimInstance Win32_Process -Filter "ParentProcessId = $($process.Id)" | ForEach-Object {
                    Stop-Process -Id $_.ProcessId -Force -ErrorAction Continue
                }
            }
            if (-not $process.HasExited) {
                Stop-Process -Id $process.Id -Force
            }
            $process.WaitForExit()
            $message = "$Name timed out after $DiagnosticTimeoutSeconds seconds."
            Set-Content -LiteralPath (Join-Path $diagnosticDirectory "$Name.exit.txt") -Value $message
            Write-Warning $message
            return 124
        }
        $process.WaitForExit()
        $message = '{0} exit code: {1} (0x{1:X8})' -f $Name, $process.ExitCode
        Set-Content -LiteralPath (Join-Path $diagnosticDirectory "$Name.exit.txt") -Value $message
        Write-Host $message
        return $process.ExitCode
    }
    finally {
        $process.Dispose()
        foreach ($path in @($stdout, $stderr)) {
            Write-Host "===== $path ====="
            # ProcDump mixes UTF-16 output with its child's output; keep the raw file for inspection.
            Get-Content -LiteralPath $path | ForEach-Object { Write-Host $_.Replace("`0", '') }
        }
    }
}

$testExitCode = 1
try {
    # The SDK owns test startup, results, and exit codes, including sidecar activation when MSIX is requested.
    # MSBuild's %22 escaping preserves quotes around result paths containing spaces.
    & dotnet msbuild $project /t:InvokeTestingPlatform /nologo /v:minimal `
        "/p:Platform=$platform" "/p:Configuration=$configuration" "/p:TargetFramework=$targetFramework" `
        "/p:WindowsPackageType=$WindowsPackageType" `
        "/p:PublishAot=$($NativeAot.IsPresent)" /p:TestingPlatformCaptureOutput=false `
        $targetPathProperty `
        "/p:TestingPlatformCommandLineArguments=--output Detailed --report-trx --results-directory %22$OutputDir%22"
    $testExitCode = $LASTEXITCODE

    if ($DiagnoseStartup -and $testExitCode -ne 0 -and $WindowsPackageType -eq 'None') {
        try {
            Write-Host "MTP/MSBuild exit code: $testExitCode. Probing standalone startup without the MSBuild pipe."
            # Listing tests exercises the same WinUI startup without rerunning the lifetime test bodies.
            $probeExitCode = Invoke-StartupProbe -FilePath $executable `
                -Arguments @('--list-tests', '--output', 'Detailed') -Name 'direct'
            if ($probeExitCode -ne 0) {
                $toolsDirectory = Join-Path $diagnosticDirectory 'procdump'
                New-Item -ItemType Directory -Force $toolsDirectory | Out-Null
                $archive = Join-Path $toolsDirectory 'Procdump.zip'
                Invoke-WebRequest -UseBasicParsing -TimeoutSec 60 `
                    -Uri 'https://download.sysinternals.com/files/Procdump.zip' -OutFile $archive
                Expand-Archive -LiteralPath $archive -DestinationPath $toolsDirectory -Force
                $procDump = Join-Path $toolsDirectory $(if ($platform -eq 'arm64') { 'procdump64a.exe' } elseif ($platform -eq 'x64') { 'procdump64.exe' } else { 'procdump.exe' })
                $dumpDirectory = Join-Path $diagnosticDirectory 'dumps'
                New-Item -ItemType Directory -Force $dumpDirectory | Out-Null
                # Launch under the debugger so even failures before Main can be captured.
                $null = Invoke-StartupProbe -FilePath $procDump -Name 'procdump' `
                    -Arguments @('-accepteula', '-ma', '-e', '-t', '-n', '1',
                        '-x', "`"$dumpDirectory`"", "`"$executable`"", '--list-tests', '--output', 'Detailed')
                if (-not (Get-ChildItem -LiteralPath $dumpDirectory -Filter '*.dmp')) {
                    Write-Warning "ProcDump did not produce a dump. See procdump stdout/stderr for details."
                }
                Get-ChildItem -LiteralPath $executableDirectory -Filter '*.pdb' |
                    Copy-Item -Destination $dumpDirectory
            }
        }
        catch {
            Write-Warning "Startup diagnostic collection failed: $_"
        }
    }
}
finally {
    if ($DiagnoseStartup) {
        Get-ChildItem -LiteralPath $diagnosticDirectory -Filter 'startup-*.log' | ForEach-Object {
            Write-Host "===== $($_.FullName) ====="
            Get-Content -LiteralPath $_.FullName | ForEach-Object { Write-Host $_ }
        }
        foreach ($name in $savedEnvironment.Keys) {
            [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name])
        }
        Write-Host "Startup diagnostics saved to $diagnosticDirectory"
    }
}
exit $testExitCode
