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

      * Projection: a class library generates a reference projection for a third-party
        component's '.winmd' (reusing the one emitted by the authoring test), validating the
        reference projection generator and the forwarder generator, exactly as a NuGet
        projection author would.

      * WindowsSdkProjection: a class library generates the base Windows SDK reference projection
        from the 'Microsoft.Windows.SDK.Contracts' '.winmd' files, exactly as the
        'Microsoft.Windows.SDK.NET.Ref' projection package is produced. This validates that the full
        Windows SDK surface generates and compiles against the packaged 'WinRT.Runtime' reference
        assembly, catching reference-projection codegen regressions before they break that package.

      * WindowsSdkXamlProjection: as above, but for the 'Windows.UI.Xaml' surface, which references the
        base Windows SDK reference projection (mirroring how the UWP XAML projection package depends on
        the base Windows SDK projection package).

      * ComponentUsingProjection: a Windows Runtime component consumes a *packaged* projection (the one
        packed from the projection test above) and exposes one of its types. A package reference resolves
        to the reference assembly, unlike the forwarder a project reference resolves to, so this is the
        only test that covers a projected type being declared both by that reference assembly and by the
        projection generated for the component itself.

    The smoke tests reference the package via 'RestoreSources' (see the '.csproj' files), so
    no global NuGet configuration changes are required.

.PARAMETER PackageSource
    Folder containing the built 'Microsoft.Windows.CsWinRT' NuGet package.

.PARAMETER PackageVersion
    Version of the 'Microsoft.Windows.CsWinRT' package to consume.

.PARAMETER Test
    Which smoke test(s) to run: 'Consumption', 'Authoring', 'Projection', 'WindowsSdkProjection',
    'WindowsSdkXamlProjection', 'ComponentUsingProjection', or 'All' (the default). The CI runs each test
    as its own step (passing a single value), so an individual failure is reported in isolation; local
    builds use the default 'All'.

.PARAMETER Runtime
    Which runtime to target: 'CoreCLR' (the default) builds and runs on the managed runtime;
    'NativeAot' publishes the project with Native AOT ('PublishAot=true', win-x64), exercising the
    full publish pipeline (projection and interop generators, then ILC). The CI runs both as
    separate steps so a failure points at the exact runtime. The 'Projection', 'WindowsSdkProjection',
    'WindowsSdkXamlProjection', and 'ComponentUsingProjection' tests are build-only and therefore
    CoreCLR-only; they are skipped for 'NativeAot'.

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

    [ValidateSet('All', 'Consumption', 'Authoring', 'Projection', 'WindowsSdkProjection', 'WindowsSdkXamlProjection', 'ComponentUsingProjection')]
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
$projectionProject = [IO.Path]::Combine($smokeTestsRoot, 'Projection', 'Projection.csproj')
$windowsSdkProjectionProject = [IO.Path]::Combine($smokeTestsRoot, 'WindowsSdkProjection', 'WindowsSdkProjection.csproj')
$windowsSdkXamlProjectionProject = [IO.Path]::Combine($smokeTestsRoot, 'WindowsSdkXamlProjection', 'WindowsSdkXamlProjection.csproj')
$componentUsingProjectionProject = [IO.Path]::Combine($smokeTestsRoot, 'ComponentUsingProjection', 'ComponentUsingProjection.csproj')

# Version the 'Projection' smoke test is packed with, and that 'ComponentUsingProjection' consumes.
$projectionPackageVersion = '1.0.0'

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

# Projection: build a reference projection for a third-party component's '.winmd' (CoreCLR only). This
# is a build-time artifact, so there is nothing to publish with Native AOT.
function Invoke-ProjectionSmokeTest {
    Write-Host "`n=== Projection smoke test ($Runtime) ===" -ForegroundColor Green

    Invoke-ReferenceProjectionSmokeTest -Name 'Projection' -Project $projectionProject
}

# Windows SDK projection: build the base Windows SDK reference projection, exactly as the
# 'Microsoft.Windows.SDK.NET.Ref' projection package is produced (CoreCLR only; build-time artifact).
function Invoke-WindowsSdkProjectionSmokeTest {
    Write-Host "`n=== Windows SDK projection smoke test ($Runtime) ===" -ForegroundColor Green

    Invoke-ReferenceProjectionSmokeTest -Name 'WindowsSdkProjection' -Project $windowsSdkProjectionProject
}

# Windows SDK XAML projection: build the 'Windows.UI.Xaml' reference projection (which references the
# base Windows SDK reference projection above), exactly as the UWP XAML projection package is produced
# (CoreCLR only; build-time artifact).
function Invoke-WindowsSdkXamlProjectionSmokeTest {
    Write-Host "`n=== Windows SDK XAML projection smoke test ($Runtime) ===" -ForegroundColor Green

    Invoke-ReferenceProjectionSmokeTest -Name 'WindowsSdkXamlProjection' -Project $windowsSdkXamlProjectionProject
}

# Shared implementation for the reference-projection smoke tests. Building a reference projection produces
# a forwarder assembly (from 'cswinrtimplgen') next to a 'ref' reference assembly (compiled from the
# 'cswinrtprojectionrefgen' sources). Verifying both were produced confirms the package wired up and ran
# both generators correctly, and that the generated reference projection compiled against the packaged
# 'WinRT.Runtime' reference assembly. A reference projection is a build-time artifact, so there is nothing
# to publish with Native AOT and these tests run on CoreCLR only.
function Invoke-ReferenceProjectionSmokeTest {
    param (
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [string] $Project
    )

    if ($Runtime -eq 'NativeAot') {
        Write-Host "Skipping the $Name smoke test for Native AOT (a reference projection is a build-time artifact)." -ForegroundColor DarkGray
        return
    }

    Invoke-Dotnet (@('build', $Project) + $commonBuildArgs)

    $projectDirectory = [IO.Path]::GetDirectoryName($Project)
    $assemblies = Get-ChildItem -Path ([IO.Path]::Combine($projectDirectory, 'bin')) -Filter "$Name.dll" -Recurse -ErrorAction SilentlyContinue

    $forwarder = $assemblies | Where-Object { $_.FullName -notmatch '\\ref\\' } | Select-Object -First 1
    $referenceAssembly = $assemblies | Where-Object { $_.FullName -match '\\ref\\' } | Select-Object -First 1

    if ($null -eq $forwarder) {
        throw "The $Name build did not produce the '$Name.dll' forwarder assembly."
    }

    if ($null -eq $referenceAssembly) {
        throw "The $Name build did not produce the 'ref\$Name.dll' reference assembly."
    }

    Write-Host "Verified the $Name projection produced both a forwarder and a reference assembly." -ForegroundColor DarkGray
}

# Component consuming a packaged projection: pack the 'Projection' smoke test, then build a Windows
# Runtime component that consumes it as a package and exposes one of its types. A package reference
# resolves to the reference assembly under 'ref', which declares the projected types for real, unlike
# the forwarder a project reference resolves to, so only this shape covers the ambiguity between those
# declarations and the ones generated into this component's own projection. Building the component is
# the assertion: the component projection fails to compile if they are ambiguous.
function Invoke-ComponentUsingProjectionSmokeTest {
    Write-Host "`n=== Component using a packaged projection smoke test ($Runtime) ===" -ForegroundColor Green

    if ($Runtime -eq 'NativeAot') {
        Write-Host 'Skipping the ComponentUsingProjection smoke test for Native AOT (it is covered by the CoreCLR build).' -ForegroundColor DarkGray
        return
    }

    $projectionDirectory = [IO.Path]::GetDirectoryName($projectionProject)
    $projectionPackageSource = [IO.Path]::Combine($projectionDirectory, 'bin', $Configuration)

    Invoke-Dotnet (@('pack', $projectionProject, "-p:PackageVersion=$projectionPackageVersion") + $commonBuildArgs)

    $package = [IO.Path]::Combine($projectionPackageSource, "Projection.$projectionPackageVersion.nupkg")

    if (-not (Test-Path -Path $package)) {
        throw "Packing the projection did not produce '$package'."
    }

    # The whole point of this test is that the projection is consumed through its reference assembly,
    # so fail loudly (rather than silently covering nothing) if the package is not laid out that way.
    Assert-PackageHasReferenceAssemblyLayout -Path $package -AssemblyName 'Projection'

    Invoke-Dotnet (@('build', $componentUsingProjectionProject, "-p:ProjectionPackageVersion=$projectionPackageVersion") + $commonBuildArgs)

    Write-Host 'Verified a component consuming a packaged projection builds its component projection.' -ForegroundColor DarkGray
}

# Verifies a projection package ships both the reference assembly consumers compile against ('ref')
# and the forwarder they bind to at runtime ('lib'), plus the metadata the consumer regenerates the
# implementation from. All three are preconditions for the test above covering anything.
function Assert-PackageHasReferenceAssemblyLayout {
    param (
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $AssemblyName
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $archive = [IO.Compression.ZipFile]::OpenRead($Path)

    try {
        $entries = $archive.Entries | ForEach-Object { $_.FullName }
    }
    finally {
        $archive.Dispose()
    }

    $referenceAssembly = $entries | Where-Object { $_ -like "ref/*/$AssemblyName.dll" } | Select-Object -First 1
    $forwarder = $entries | Where-Object { $_ -like "lib/*/$AssemblyName.dll" } | Select-Object -First 1
    $metadata = $entries | Where-Object { $_ -like 'metadata/*.winmd' } | Select-Object -First 1

    if ($null -eq $referenceAssembly) {
        throw "The projection package does not contain 'ref/<tfm>/$AssemblyName.dll', so it is not consumed through a reference assembly and this test would cover nothing."
    }

    if ($null -eq $forwarder) {
        throw "The projection package does not contain 'lib/<tfm>/$AssemblyName.dll', so there is no forwarder for the build to prefer over the reference assembly."
    }

    if ($null -eq $metadata) {
        throw "The projection package does not contain 'metadata/<name>.winmd', so the consumer generates no projection for its types and nothing would collide with the reference assembly."
    }

    Write-Host "Verified the projection package ships '$referenceAssembly', '$forwarder' and '$metadata'." -ForegroundColor DarkGray
}

if ($Test -in @('All', 'Consumption')) {
    Invoke-ConsumptionSmokeTest
}

if ($Test -in @('All', 'Authoring')) {
    Invoke-AuthoringSmokeTest
}

if ($Test -in @('All', 'Projection')) {
    Invoke-ProjectionSmokeTest
}

if ($Test -in @('All', 'WindowsSdkProjection')) {
    Invoke-WindowsSdkProjectionSmokeTest
}

if ($Test -in @('All', 'WindowsSdkXamlProjection')) {
    Invoke-WindowsSdkXamlProjectionSmokeTest
}

if ($Test -in @('All', 'ComponentUsingProjection')) {
    Invoke-ComponentUsingProjectionSmokeTest
}

Write-Host "`nSmoke tests passed." -ForegroundColor Green
