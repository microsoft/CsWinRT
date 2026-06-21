#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Builds and runs the C#/WinRT end-to-end smoke tests against a real
    'Microsoft.Windows.CsWinRT' NuGet package.

.DESCRIPTION
    These smoke tests verify that the real NuGet package works for the two main consumer
    scenarios, in isolation from the CsWinRT repository build infrastructure:

      * Consumption: a .NET app that uses a Windows SDK projection ('Windows.Data.Json') is
        built and run, validating that the generated projection and interop assemblies, and
        the 'WinRT.Runtime' ref/impl assemblies, are wired up correctly.

      * Authoring: a Windows Runtime component library is built, validating WinMD
        generation, the reference projection, and the forwarder assembly.

    The smoke tests reference the package via 'RestoreSources' (see the '.csproj' files), so
    no global NuGet configuration changes are required.

.PARAMETER PackageSource
    Folder containing the built 'Microsoft.Windows.CsWinRT' NuGet package.

.PARAMETER PackageVersion
    Version of the 'Microsoft.Windows.CsWinRT' package to consume.

.PARAMETER Test
    Which smoke test(s) to run: 'Consumption', 'Authoring', or 'All' (the default). The CI runs
    each test as its own step (passing a single value), so an individual failure is reported in
    isolation; local builds use the default 'All'.

.PARAMETER Runtime
    Which runtime to target: 'CoreCLR' (the default) builds and runs on the managed runtime;
    'NativeAot' publishes the project with Native AOT ('PublishAot=true', win-x64), exercising the
    full publish pipeline (projection and interop generators, then ILC). The CI runs both as
    separate steps so a failure points at the exact runtime.

.PARAMETER Configuration
    Build configuration to use (defaults to 'Release').

.EXAMPLE
    ./run-smoke-tests.ps1 -PackageSource ../../_build/x64/Release/cswinrt/bin -PackageVersion 0.0.0-private.0

.EXAMPLE
    ./run-smoke-tests.ps1 -PackageSource ./packages -PackageVersion 3.0.0-preview.1 -Test Consumption -Runtime NativeAot
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $PackageSource,

    [Parameter(Mandatory = $true)]
    [string] $PackageVersion,

    [ValidateSet('All', 'Consumption', 'Authoring')]
    [string] $Test = 'All',

    [ValidateSet('CoreCLR', 'NativeAot')]
    [string] $Runtime = 'CoreCLR',

    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

# Native AOT publishes are always x64: the NuGet publish job that runs the smoke tests only runs
# on an x64 host.
$nativeAotRid = 'win-x64'

$smokeTestsRoot = $PSScriptRoot
$consumptionProject = [IO.Path]::Combine($smokeTestsRoot, 'Consumption', 'Consumption.csproj')
$authoringProject = [IO.Path]::Combine($smokeTestsRoot, 'Authoring', 'Authoring.csproj')

# Resolve the package source to an absolute path (NuGet rejects relative '--source' values).
$resolvedPackageSource = (Resolve-Path -Path $PackageSource).Path

Write-Host "Smoke tests: consuming CsWinRT package '$PackageVersion' from '$resolvedPackageSource'" -ForegroundColor Cyan

$commonBuildArgs = @(
    '--configuration', $Configuration
    "-p:CsWinRTPackageSource=$resolvedPackageSource"
    "-p:CsWinRTPackageVersion=$PackageVersion"
)

function Invoke-Dotnet {
    param ([string[]] $Arguments)

    Write-Host "> dotnet $($Arguments -join ' ')" -ForegroundColor DarkGray
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command 'dotnet $($Arguments -join ' ')' failed with exit code $LASTEXITCODE."
    }
}

function Assert-WinMDDefinesType {
    param (
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Namespace,
        [Parameter(Mandatory = $true)] [string] $TypeName
    )

    # Deliberately lightweight inspection (no managed-metadata dependencies, works in any
    # PowerShell host): a Windows Runtime metadata file carries the 'WindowsRuntime 1.4'
    # metadata version, and a type's namespace and name are stored as separate, null-terminated
    # entries in the metadata strings heap.
    $text = [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($Path))

    if (-not $text.Contains('WindowsRuntime 1.4')) {
        throw "'$Path' is not a Windows Runtime metadata (.winmd) file."
    }

    foreach ($name in @($Namespace, $TypeName)) {
        if (-not $text.Contains("$name`0")) {
            throw "'$Path' does not define '$Namespace.$TypeName' (missing '$name')."
        }
    }

    Write-Host "Verified '$([IO.Path]::GetFileName($Path))' defines '$Namespace.$TypeName'." -ForegroundColor DarkGray
}

# Consumption: build (CoreCLR) or Native AOT publish, then run (must not crash).
function Invoke-ConsumptionSmokeTest {
    Write-Host "`n=== Consumption smoke test ($Runtime) ===" -ForegroundColor Green

    if ($Runtime -eq 'NativeAot') {
        # Publish the whole app with Native AOT (self-contained, no managed host).
        Invoke-Dotnet (@('publish', $consumptionProject, '--runtime', $nativeAotRid, '-p:PublishAot=true') + $commonBuildArgs)
    }
    else {
        Invoke-Dotnet (@('build', $consumptionProject) + $commonBuildArgs)
    }

    # Locate the freshly built app, asserting a clean (zero) exit code when run. A Native AOT
    # publish drops a self-contained '.exe' under a 'publish' folder, so filter to it; a CoreCLR
    # build leaves the '.exe' directly under the target framework folder.
    $consumptionExe = Get-ChildItem -Path ([IO.Path]::Combine($smokeTestsRoot, 'Consumption', 'bin')) -Filter 'Consumption.exe' -Recurse |
        Where-Object { $Runtime -ne 'NativeAot' -or $_.FullName -match '\\publish\\' } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $consumptionExe) {
        throw "Could not find the built 'Consumption.exe'."
    }

    Write-Host "Running '$($consumptionExe.FullName)'" -ForegroundColor DarkGray
    & $consumptionExe.FullName
    if ($LASTEXITCODE -ne 0) {
        throw "Consumption smoke test crashed or failed with exit code $LASTEXITCODE."
    }
}

# Authoring: build and verify the generated Windows Runtime metadata (CoreCLR), or just verify the
# component publishes cleanly with Native AOT (we don't load the published output).
function Invoke-AuthoringSmokeTest {
    Write-Host "`n=== Authoring smoke test ($Runtime) ===" -ForegroundColor Green

    if ($Runtime -eq 'NativeAot') {
        Invoke-Dotnet (@('publish', $authoringProject, '--runtime', $nativeAotRid, '-p:PublishAot=true') + $commonBuildArgs)
        return
    }

    Invoke-Dotnet (@('build', $authoringProject) + $commonBuildArgs)

    # The authoring build emits a '.winmd' next to the component assembly. Verify it was produced
    # and that it defines the expected Windows Runtime type.
    $authoringWinMD = Get-ChildItem -Path ([IO.Path]::Combine($smokeTestsRoot, 'Authoring', 'bin')) -Filter 'Authoring.winmd' -Recurse -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $authoringWinMD) {
        throw "The authoring build did not produce 'Authoring.winmd'."
    }

    Assert-WinMDDefinesType -Path $authoringWinMD.FullName -Namespace 'Authoring' -TypeName 'Greeter'
}

if ($Test -in @('All', 'Consumption')) {
    Invoke-ConsumptionSmokeTest
}

if ($Test -in @('All', 'Authoring')) {
    Invoke-AuthoringSmokeTest
}

Write-Host "`nSmoke tests passed." -ForegroundColor Green
