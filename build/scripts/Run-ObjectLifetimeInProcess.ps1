[CmdletBinding()]
param(
    # The 'win-<plat>' output folder of ObjectLifetimeTests.Lifted.
    [Parameter(Mandatory = $true)] [string] $LayoutDir,

    # Optional folder for MTP results and the TRX report.
    [string] $OutputDir,

    [ValidateSet('None', 'MSIX')] [string] $WindowsPackageType = 'None',

    # Run the Native AOT executable from the layout's publish folder.
    [switch] $NativeAot
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

# The SDK owns test startup, results, and exit codes, including sidecar activation when MSIX is requested.
# MSBuild's %22 escaping preserves quotes around result paths containing spaces.
& dotnet msbuild $project /t:InvokeTestingPlatform /nologo /v:minimal `
    "/p:Platform=$platform" "/p:Configuration=$configuration" "/p:TargetFramework=$targetFramework" `
    "/p:WindowsPackageType=$WindowsPackageType" `
    "/p:PublishAot=$($NativeAot.IsPresent)" /p:TestingPlatformCaptureOutput=false `
    $targetPathProperty `
    "/p:TestingPlatformCommandLineArguments=--output Detailed --report-trx --results-directory %22$OutputDir%22"
exit $LASTEXITCODE
