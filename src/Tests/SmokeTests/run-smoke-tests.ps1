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

      * MixedConsumption: a .NET app combines Windows SDK projections with an authored component
        implementing an SDK interface and taking an SDK type in a constructor. This validates
        component projection generation against SDK forwarders without duplicate type definitions
        or missing SDK assembly identities.

      * ExclusiveToConsumption: a .NET app consumes packed standalone exclusive interfaces whose
        runtime classes stay in the SDK projections. Native JSON objects exercise allowed and denied
        dynamic casts, ordinary interfaces, and managed CCWs without requiring a desktop. Producer
        metadata, include/exclude precedence, and filter-only incremental rebuilds are also checked.
        Per-interface 'CsWinRT.IdicExclusiveTo.v1' metadata must survive in refs, not in forwarders.

      * Authoring: a Windows Runtime component library is built, validating WinMD
        generation, the reference projection, and the forwarder assembly.

      * Projection: a class library generates a reference projection for a third-party
        component's '.winmd' (reusing the one emitted by the authoring test), validating the
        reference projection generator and the forwarder generator, exactly as a NuGet
        projection author would. The forwarder is also checked to ship embedded symbols.

      * WindowsSdkProjection: a class library generates the base Windows SDK reference projection
        from the 'Microsoft.Windows.SDK.Contracts' '.winmd' files, exactly as the
        'Microsoft.Windows.SDK.NET.Ref' projection package is produced. This validates that the full
        Windows SDK surface generates and compiles against the packaged 'WinRT.Runtime' reference
        assembly, catching reference-projection codegen regressions before they break that package.

      * WindowsSdkXamlProjection: as above, but for the 'Windows.UI.Xaml' surface, which references the
        base Windows SDK reference projection (mirroring how the UWP XAML projection package depends on
        the base Windows SDK projection package).

    The smoke tests reference the package via 'RestoreSources' (see the '.csproj' files), so
    no global NuGet configuration changes are required. The exclusive-interface matrix uses an
    isolated package cache and unique projection package versions, never replacing a cached package.
    Its intermediate/output files and per-command binlogs are retained under 'obj\ex-<id>'.
    Its four-boolean-combination and extended incremental checks run on CoreCLR only; Native AOT
    runs the both-enabled, excluded, and public-only runtime cases.

.PARAMETER PackageSource
    Folder containing the built 'Microsoft.Windows.CsWinRT' NuGet package.

.PARAMETER PackageVersion
    Version of the 'Microsoft.Windows.CsWinRT' package to consume.

.PARAMETER Test
    Which smoke test(s) to run: 'Consumption', 'MixedConsumption', 'ExclusiveToConsumption', 'Authoring', 'Projection',
    'WindowsSdkProjection', 'WindowsSdkXamlProjection', or 'All' (the default). The CI runs each test as
    its own step (passing a single value), so an individual failure is reported in isolation; local
    builds use the default 'All'.

.PARAMETER Runtime
    Which runtime to target: 'CoreCLR' (the default) builds and runs on the managed runtime;
    'NativeAot' publishes the project with Native AOT ('PublishAot=true', win-x64), exercising the
    full publish pipeline (projection and interop generators, then ILC). The CI runs both as
    separate steps so a failure points at the exact runtime. The 'Projection', 'WindowsSdkProjection',
    and 'WindowsSdkXamlProjection' tests are build-only and therefore CoreCLR-only; they are skipped for
    'NativeAot'. Native AOT requires the MSVC x64 toolchain; make the installed 'vswhere.exe'
    discoverable on PATH when running from a non-developer shell.

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

    [ValidateSet('All', 'Consumption', 'MixedConsumption', 'ExclusiveToConsumption', 'Authoring', 'Projection', 'WindowsSdkProjection', 'WindowsSdkXamlProjection')]
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
$mixedConsumptionProject = [IO.Path]::Combine($smokeTestsRoot, 'MixedConsumption', 'MixedConsumption.csproj')
$exclusiveToProjectionProject = [IO.Path]::Combine($smokeTestsRoot, 'ExclusiveToProjection', 'ExclusiveToProjection.csproj')
$exclusiveToConsumptionProject = [IO.Path]::Combine($smokeTestsRoot, 'ExclusiveToConsumption', 'ExclusiveToConsumption.csproj')
$authoringProject = [IO.Path]::Combine($smokeTestsRoot, 'Authoring', 'Authoring.csproj')
$projectionProject = [IO.Path]::Combine($smokeTestsRoot, 'Projection', 'Projection.csproj')
$windowsSdkProjectionProject = [IO.Path]::Combine($smokeTestsRoot, 'WindowsSdkProjection', 'WindowsSdkProjection.csproj')
$windowsSdkXamlProjectionProject = [IO.Path]::Combine($smokeTestsRoot, 'WindowsSdkXamlProjection', 'WindowsSdkXamlProjection.csproj')

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
    param (
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [string] $Project
    )

    Write-Host "`n=== $Name smoke test ($Runtime) ===" -ForegroundColor Green

    if ($Runtime -eq 'NativeAot') {
        # Publish the whole app with Native AOT (self-contained, no managed host).
        Invoke-Dotnet (@('publish', $Project, '--runtime', $nativeAotRid, '-p:PublishAot=true') + $commonBuildArgs)
    }
    else {
        Invoke-Dotnet (@('build', $Project) + $commonBuildArgs)
    }

    # Locate the freshly built app, asserting a clean (zero) exit code when run. A Native AOT
    # publish drops a self-contained '.exe' under a 'publish' folder, so filter to it; a CoreCLR
    # build leaves the '.exe' directly under the target framework folder.
    $consumptionExe = Get-ChildItem -Path ([IO.Path]::Combine([IO.Path]::GetDirectoryName($Project), 'bin')) -Filter "$Name.exe" -Recurse |
        Where-Object { $Runtime -ne 'NativeAot' -or $_.FullName -match '\\publish\\' } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $consumptionExe) {
        throw "Could not find the built '$Name.exe'."
    }

    Write-Host "Running '$($consumptionExe.FullName)'" -ForegroundColor DarkGray
    & $consumptionExe.FullName
    if ($LASTEXITCODE -ne 0) {
        throw "$Name smoke test crashed or failed with exit code $LASTEXITCODE."
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

    # The forwarder is what lands in 'lib/<tfm>' of a projection package, so it has to ship symbols. It
    # is emitted as metadata rather than compiled, so its debug information is synthesized by
    # 'cswinrtimplgen'; without that, the whole package reports as having no symbols.
    Assert-HasEmbeddedSymbols -Path $forwarder.FullName

    Write-Host "Verified the $Name projection produced both a forwarder and a reference assembly." -ForegroundColor DarkGray
}

# Verifies that an assembly carries an embedded portable PDB and is marked as reproducible.
function Assert-HasEmbeddedSymbols {
    param ([Parameter(Mandatory = $true)] [string] $Path)

    $stream = [IO.File]::OpenRead($Path)
    try {
        $peReader = [Reflection.PortableExecutable.PEReader]::new($stream)
        try {
            $entryTypes = $peReader.ReadDebugDirectory() | ForEach-Object { $_.Type }

            foreach ($required in @('EmbeddedPortablePdb', 'Reproducible', 'CodeView', 'PdbChecksum')) {
                if ($entryTypes -notcontains $required) {
                    throw "'$([IO.Path]::GetFileName($Path))' is missing the '$required' debug directory entry (has: $($entryTypes -join ', '))."
                }
            }
        }
        finally { $peReader.Dispose() }
    }
    finally { $stream.Dispose() }

    Write-Host "Verified '$([IO.Path]::GetFileName($Path))' ships embedded symbols." -ForegroundColor DarkGray
}

function Read-ExclusiveToProjectionMetadata {
    param ([Parameter(Mandatory = $true)] [string] $Path)

    $stream = [IO.File]::OpenRead($Path)
    try { return [CsWinRT.SmokeTests.ExclusiveToProjectionMetadata]::Read($stream) }
    finally { $stream.Dispose() }
}

function Read-ExclusiveToPackedMetadata {
    param ([Parameter(Mandatory = $true)] [IO.Compression.ZipArchiveEntry] $Entry)

    $stream = $Entry.Open()
    $memory = [IO.MemoryStream]::new()
    try {
        $stream.CopyTo($memory)
        $memory.Position = 0
        return [CsWinRT.SmokeTests.ExclusiveToProjectionMetadata]::Read($memory)
    }
    finally {
        $memory.Dispose()
        $stream.Dispose()
    }
}

function Assert-ExclusiveToReferenceMetadata {
    param (
        [Parameter(Mandatory = $true)] $Metadata,
        [Parameter(Mandatory = $true)] [string[]] $Interfaces,
        [Parameter(Mandatory = $true)] [bool] $PublicInterfaces,
        [string[]] $Expected = @()
    )

    $sortedExpected = [string[]] $Expected.Clone()
    [Array]::Sort($sortedExpected, [StringComparer]::Ordinal)

    if (-not $Metadata.HasReferenceAttribute -or
        ($Metadata.SelectedInterfaces -join ';') -cne ($sortedExpected -join ';')) {
        throw "Packed reference policy mismatch: expected '$($sortedExpected -join ';')', got '$($Metadata.SelectedInterfaces -join ';')'."
    }

    if (-not $Metadata.UnknownMetadataKeys.Contains('CsWinRT.SmokeTests.FutureMetadata')) {
        throw 'The unknown-key fixture was not preserved in the reference assembly.'
    }

    foreach ($name in $Interfaces) {
        if ($PublicInterfaces) {
            if (-not $Metadata.Interfaces.ContainsKey($name) -or -not $Metadata.Interfaces[$name]) {
                throw "The public-exclusive option did not preserve the public '$name' declaration."
            }
        }
        elseif ($Metadata.Interfaces.ContainsKey($name) -and $Metadata.Interfaces[$name]) {
            throw "The internal-exclusive option unexpectedly exposed '$name' publicly."
        }
    }
}

function Assert-ExclusiveToImplementationMetadata {
    param (
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string[]] $Interfaces,
        [Parameter(Mandatory = $true)] [bool] $PublicInterfaces,
        [string[]] $Expected = @()
    )

    $metadata = Read-ExclusiveToProjectionMetadata -Path $Path

    if ($metadata.MetadataAttributeCount -ne 0) {
        throw 'Reference-only metadata leaked into the merged implementation assembly.'
    }

    foreach ($name in $Interfaces) {
        $selected = $Expected -ccontains $name

        if ($PublicInterfaces -or $selected) {
            if (-not $metadata.Interfaces.ContainsKey($name) -or $metadata.Interfaces[$name] -ne $PublicInterfaces) {
                throw "The merged projection did not regenerate '$name' with the producer's visibility ($PublicInterfaces)."
            }
        }

        if ($metadata.DynamicInterfaces.Contains($name) -ne $selected) {
            throw "The merged projection has a missing or stale IDIC implementation for '$name' (expected $selected)."
        }
    }
}

# Quote lists for MSBuild's command-line parser. Escaping ';' as '%3B' would preserve it as a
# literal character and prevent the generator task's string[] parameter from splitting the list.
function ConvertTo-MSBuildPropertyValue {
    param ([AllowEmptyString()] [string] $Value)

    return "`"$Value`""
}

function Invoke-ExclusiveToConsumptionSmokeTest {
    Write-Host "`n=== Exclusive-interface policy smoke test ($Runtime) ===" -ForegroundColor Green

    if (-not ('CsWinRT.SmokeTests.ExclusiveToProjectionMetadata' -as [type])) {
        Add-Type -Path ([IO.Path]::Combine($smokeTestsRoot, 'ExclusiveToProjectionMetadata.cs'))
    }

    $arrayInterface = 'Windows.Data.Json.IJsonArray'
    $objectInterface = 'Windows.Data.Json.IJsonObjectWithDefaultValues'
    $xamlInterface = 'Windows.UI.Xaml.IFrameworkElementProtected7'
    $interfaces = @($arrayInterface, $objectInterface, $xamlInterface)
    $includes = " $objectInterface ; ; $arrayInterface ; $xamlInterface ; $objectInterface ; windows.data.json "
    $excludes = ' Windows.Data.Json.IJsonObject ; ; Windows.Data.Json.IJsonObject ; windows.ui.xaml '

    # ToolTask starts native apphosts, whose executable paths still have a MAX_PATH limit.
    do {
        $runId = [Guid]::NewGuid().ToString('N')
        $runRoot = [IO.Path]::Combine($smokeTestsRoot, 'obj', "ex-$($runId.Substring(0, 8))")
    } while ([IO.Directory]::Exists($runRoot))

    $feed = [IO.Path]::Combine($runRoot, 'feed')
    $packages = [IO.Path]::Combine($runRoot, 'p')
    $logs = [IO.Path]::Combine($runRoot, 'logs')
    $projectionIntermediate = [IO.Path]::Combine($runRoot, 'projection', 'obj') + '\'
    $consumptionIntermediate = [IO.Path]::Combine($runRoot, 'consumption', 'obj') + '\'
    $projectionOutput = [IO.Path]::Combine($runRoot, 'projection', 'bin') + '\'
    $consumptionOutput = [IO.Path]::Combine($runRoot, 'consumption', 'bin') + '\'
    $referencePath = [IO.Path]::Combine($projectionOutput, 'ref', 'ExclusiveToProjection.dll')
    $projectionArgs = $commonBuildArgs + @(
        "-p:RestorePackagesPath=$packages", "-p:OutputPath=$projectionOutput"
        "-p:BaseIntermediateOutputPath=$projectionIntermediate"
        "-bl:$([IO.Path]::Combine($logs, 'projection-{}.binlog'))"
    )
    $consumptionArgs = $commonBuildArgs + @(
        "-p:RestorePackagesPath=$packages", "-p:OutputPath=$consumptionOutput"
        "-p:BaseIntermediateOutputPath=$consumptionIntermediate"
        "-bl:$([IO.Path]::Combine($logs, 'consumption-{}.binlog'))"
    )

    New-Item -ItemType Directory -Path $feed, $logs -Force | Out-Null
    Write-Host "Exclusive-interface artifacts: '$runRoot'." -ForegroundColor DarkGray

    # All profiles deliberately reuse the same intermediate and output paths. Do not clean between
    # them: changes to only Includes/Excludes must invalidate both producer and consumer generation.
    function Invoke-ExclusiveToProfile {
        param (
            [Parameter(Mandatory = $true)] [string] $Name,
            [Parameter(Mandatory = $true)] [bool] $PublicInterfaces,
            [Parameter(Mandatory = $true)] [bool] $DynamicallyCastable,
            [string] $Includes = '',
            [string] $Excludes = '',
            [string[]] $Expected = @(),
            [switch] $Consume
        )

        Write-Host "`n--- Exclusive interfaces: $Name ---" -ForegroundColor Cyan

        $policyArgs = @(
            "-p:CsWinRTPublicExclusiveToInterfaces=$PublicInterfaces"
            "-p:CsWinRTDynamicallyInterfaceCastableExclusiveTo=$DynamicallyCastable"
            "-p:CsWinRTDynamicallyInterfaceCastableExclusiveToIncludes=$(ConvertTo-MSBuildPropertyValue $Includes)"
            "-p:CsWinRTDynamicallyInterfaceCastableExclusiveToExcludes=$(ConvertTo-MSBuildPropertyValue $Excludes)"
        )

        Invoke-Dotnet (@('build', $exclusiveToProjectionProject) + $projectionArgs + $policyArgs) | Out-Host
        $metadata = Read-ExclusiveToProjectionMetadata -Path $referencePath
        Assert-ExclusiveToReferenceMetadata -Metadata $metadata -Interfaces $interfaces -PublicInterfaces $PublicInterfaces -Expected $Expected
        $referenceHash = (Get-FileHash -Path $referencePath -Algorithm SHA256).Hash

        if ($Consume) {
            $forwarderPath = [IO.Path]::Combine($projectionOutput, 'ExclusiveToProjection.dll')
            Assert-HasEmbeddedSymbols -Path $forwarderPath

            if ((Read-ExclusiveToProjectionMetadata -Path $forwarderPath).MetadataAttributeCount -ne 0) {
                throw 'Reference-only metadata was copied to the forwarder.'
            }

            # Pack without rebuilding, so changing a package identity cannot mask a missing filter
            # cache key. Each consumer restore gets a new identity, without overwriting any cache.
            $version = "1.0.0-smoke.run$runId.$Name"
            Invoke-Dotnet (@('pack', $exclusiveToProjectionProject, '--no-build', '--no-restore', '--output', $feed, "-p:PackageVersion=$version") + $projectionArgs + $policyArgs) | Out-Host

            if ((Get-FileHash -Path $referencePath -Algorithm SHA256).Hash -ne $referenceHash) {
                throw 'Packing unexpectedly rebuilt the reference assembly, masking the incremental-generation check.'
            }

            $packagePath = [IO.Path]::Combine($feed, "CsWinRT.SmokeTests.ExclusiveToProjection.$version.nupkg")
            $package = [IO.Compression.ZipFile]::OpenRead($packagePath)
            try {
                $references = @($package.Entries | Where-Object { $_.FullName -match '^ref/[^/]+/ExclusiveToProjection\.dll$' })
                $forwarders = @($package.Entries | Where-Object { $_.FullName -match '^lib/[^/]+/ExclusiveToProjection\.dll$' })

                if ($references.Count -ne 1 -or $forwarders.Count -ne 1) {
                    throw "The projection package must contain exactly one ref assembly and one lib forwarder: '$packagePath'."
                }

                $packedMetadata = Read-ExclusiveToPackedMetadata -Entry $references[0]
                Assert-ExclusiveToReferenceMetadata -Metadata $packedMetadata -Interfaces $interfaces -PublicInterfaces $PublicInterfaces -Expected $Expected

                if ((Read-ExclusiveToPackedMetadata -Entry $forwarders[0]).MetadataAttributeCount -ne 0) {
                    throw 'The packed lib forwarder contains reference-only metadata.'
                }
            }
            finally { $package.Dispose() }

            # No producer policy property is passed to the consumer. Its only policy input is the
            # durable metadata in the restored projection package, not a ProjectReference/global flag.
            $appArgs = $consumptionArgs + @(
                "-p:ExclusiveToProjectionPackageSource=$feed"
                "-p:ExclusiveToProjectionPackageVersion=$version"
                "-p:ExclusiveToTestPublicInterfaces=$PublicInterfaces"
            )
            $exeDirectory = $consumptionOutput

            if ($Runtime -eq 'NativeAot') {
                $exeDirectory = [IO.Path]::Combine($consumptionOutput, 'publish')
                Invoke-Dotnet (@('publish', $exclusiveToConsumptionProject, '--runtime', $nativeAotRid, '-p:PublishAot=true', "-p:PublishDir=$exeDirectory") + $appArgs) | Out-Host
            }
            else {
                Invoke-Dotnet (@('build', $exclusiveToConsumptionProject) + $appArgs) | Out-Host
                Assert-ExclusiveToImplementationMetadata -Path ([IO.Path]::Combine($consumptionOutput, 'WinRT.Projection.dll')) -Interfaces $interfaces -PublicInterfaces $PublicInterfaces -Expected $Expected
            }

            if ($PublicInterfaces) {
                $exe = [IO.Path]::Combine($exeDirectory, 'ExclusiveToConsumption.exe')
                $expectations = @(
                    ($Expected -ccontains $arrayInterface).ToString()
                    ($Expected -ccontains $objectInterface).ToString()
                    ($Expected -ccontains $xamlInterface).ToString()
                )

                Write-Host "> $exe --expect-idic $($expectations -join ' ')" -ForegroundColor DarkGray
                & $exe --expect-idic @expectations | Out-Host

                if ($LASTEXITCODE -ne 0) {
                    throw "Exclusive-interface profile '$Name' failed with exit code $LASTEXITCODE."
                }
            }
        }

        return $referenceHash
    }

    if ($Runtime -eq 'CoreCLR') {
        $null = Invoke-ExclusiveToProfile -Name 'private-disabled' -PublicInterfaces $false -DynamicallyCastable $false -Includes $includes

        # There are no public declarations for these interfaces. Whether the compiler retains their
        # internal definitions or prunes them, the string policy must regenerate only the selected set.
        $null = Invoke-ExclusiveToProfile -Name 'private-enabled' -PublicInterfaces $false -DynamicallyCastable $true -Includes $includes -Excludes $excludes -Expected @($arrayInterface, $xamlInterface) -Consume
    }

    # Filters cannot turn IDIC on, but disabling IDIC must not suppress public declarations or CCWs.
    $null = Invoke-ExclusiveToProfile -Name 'public-only' -PublicInterfaces $true -DynamicallyCastable $false -Includes $includes -Excludes $excludes -Consume

    # Empty includes select every eligible exclusive interface.
    $null = Invoke-ExclusiveToProfile -Name 'both-enabled' -PublicInterfaces $true -DynamicallyCastable $true -Expected $interfaces -Consume

    # Includes exercise whitespace, empty entries, duplicate names, and case-sensitive prefixes.
    $includedHash = Invoke-ExclusiveToProfile -Name 'filtered-inputs' -PublicInterfaces $true -DynamicallyCastable $true -Includes $includes -Expected $interfaces

    # Change ONLY Excludes. Its namespace/type prefix beats the more-specific, exact type include.
    $excludedHash = Invoke-ExclusiveToProfile -Name 'excluded' -PublicInterfaces $true -DynamicallyCastable $true -Includes $includes -Excludes $excludes -Expected @($arrayInterface, $xamlInterface) -Consume

    if ($excludedHash -eq $includedHash) {
        throw 'Changing only the exclusion filter did not regenerate the reference policy.'
    }

    if ($Runtime -eq 'CoreCLR') {
        # Re-enable the removed implementation without cleaning either project or changing sources.
        $restoredHash = Invoke-ExclusiveToProfile -Name 'restored' -PublicInterfaces $true -DynamicallyCastable $true -Includes $includes -Expected $interfaces -Consume

        if ($restoredHash -eq $excludedHash) {
            throw 'Removing only the exclusion filter left a stale reference policy.'
        }

        # Change ONLY Includes. A partial type-name prefix is valid, but matching is case-sensitive.
        $narrowIncludes = " Windows.Data.Json.IJsonObjectWithDefault ; ; $xamlInterface ; windows.data.json.IJsonArray "
        $narrowHash = Invoke-ExclusiveToProfile -Name 'narrowed' -PublicInterfaces $true -DynamicallyCastable $true -Includes $narrowIncludes -Expected @($objectInterface, $xamlInterface) -Consume

        if ($narrowHash -eq $restoredHash) {
            throw 'Changing only the inclusion filter did not regenerate the reference policy.'
        }
    }
}

if ($Test -in @('All', 'Consumption')) {
    Invoke-ConsumptionSmokeTest -Name 'Consumption' -Project $consumptionProject
}

if ($Test -in @('All', 'MixedConsumption')) {
    Invoke-ConsumptionSmokeTest -Name 'MixedConsumption' -Project $mixedConsumptionProject
}

if ($Test -in @('All', 'ExclusiveToConsumption')) {
    Invoke-ExclusiveToConsumptionSmokeTest
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

Write-Host "`nSmoke tests passed." -ForegroundColor Green
