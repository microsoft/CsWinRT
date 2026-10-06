[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)] [string] $PackageSource,
    [Parameter(Mandatory = $true)] [string] $PackageVersion,
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$PackageSource = (Resolve-Path -LiteralPath $PackageSource).Path
$app = Join-Path $PSScriptRoot 'PreparedApp\PreparedApp.csproj'
$consumer = Join-Path $PSScriptRoot 'Consumer\Consumer.csproj'
$runRoot = Join-Path $PSScriptRoot "obj\graph-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
$null = New-Item -ItemType Directory -Path $runRoot
$script:commandNumber = 0

function Invoke-GraphMSBuild {
    param (
        [string] $Project,
        [string] $Name,
        [string[]] $Arguments,
        [switch] $Query,
        [string] $ExpectedError
    )

    $script:commandNumber++
    $log = Join-Path $profileRoot "$script:commandNumber-$Name.binlog"
    $args = @(
        'msbuild', $Project, '-nologo', '-v:quiet', '-nr:false', "-bl:$log"
        "-p:Configuration=$Configuration", '-p:Platform=AnyCPU'
        "-p:CsWinRTPackageSource=$PackageSource", "-p:CsWinRTPackageVersion=$PackageVersion"
        "-p:GraphBuildTestDirectory=$profileRoot\"
        "-p:GraphBuildMarkupPath=$profileRoot\View.xaml"
        "-p:GraphBuildExtraSource=$extraSource"
        "-p:GraphBuildExtraReference=$extraReference"
        '-p:FindInvalidProjectReferences=true', '-p:RestoreUseStaticGraphEvaluation=true'
    ) + $Arguments

    $output = (& dotnet @args 2>&1) -join [Environment]::NewLine
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0 -and (-not $ExpectedError -or -not $output.Contains($ExpectedError))) {
        throw "$Name failed (binlog: '$log'):`n$output"
    }
    if (-not (Test-Path -LiteralPath $log -PathType Leaf)) {
        throw "$Name did not produce a binlog."
    }
    if ($ExpectedError) {
        if ($exitCode -eq 0) { throw "$Name succeeded without the expected error '$ExpectedError'." }
        Write-Host $output
        return
    }
    if ($Query) {
        $jsonStart = [regex]::Match($output, '(?m)^\{')
        if (-not $jsonStart.Success) { throw "$Name returned no MSBuild query result: $output" }
        if ($jsonStart.Index -gt 0) { Write-Host $output.Substring(0, $jsonStart.Index).TrimEnd() }
        return $output.Substring($jsonStart.Index) | ConvertFrom-Json
    }
    if ($output) { Write-Host $output }
}

function Get-AssemblySnapshot {
    return @(Get-ChildItem -LiteralPath $profileRoot -Filter '*.dll' -Recurse |
        ForEach-Object { "$($_.FullName)|$($_.LastWriteTimeUtc.Ticks)" } |
        Sort-Object) -join ';'
}

function Assert-PreparedCompilation {
    $trace = @(Get-Content -LiteralPath (Join-Path $appObj 'preparation.txt'))
    if (($trace -join ',') -ne 'resources,compile-inputs,compiled') {
        throw "Compilation bypassed resource/compile preparation: $($trace -join ', ')."
    }
}

function Assert-GeneratedArtifacts {
    param ([string] $Directory)

    foreach ($name in 'WinRT.Interop.dll', 'WinRT.Sdk.Projection.dll') {
        if (-not (Test-Path -LiteralPath (Join-Path $Directory $name) -PathType Leaf)) {
            throw "Missing generated artifact '$name' in '$Directory'."
        }
    }
}

foreach ($nodes in 1, 4) {
    $profileRoot = Join-Path $runRoot "m$nodes"
    $null = New-Item -ItemType Directory -Path $profileRoot
    $extraSource = ''
    $extraReference = 'false'
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'PreparedApp\View.xaml') -Destination $profileRoot
    $tfm = 'net10.0-windows10.0.26100.1'
    $appObj = Join-Path $profileRoot "PreparedApp\obj\$Configuration\$tfm"
    $appBin = Join-Path $profileRoot "PreparedApp\bin\$Configuration\$tfm"
    $buildArgs = @('-graphBuild', "-m:$nodes", '-t:Build')

    Invoke-GraphMSBuild $consumer 'restore' @('-t:Restore')

    foreach ($target in 'GetTargetPath', 'GetTargetPathWithTargetPlatformMoniker', 'GetCopyToOutputDirectoryItems', 'GetCopyToPublishDirectoryItems') {
        $result = Invoke-GraphMSBuild $app "cold-$target" @(
            "-t:$target", "-getTargetResult:$target", '-p:BuildingProject=false'
        ) -Query
        if ($result.TargetResults.$target.Result -ne 'Success' -or
            ($target.StartsWith('GetTargetPath') -and @($result.TargetResults.$target.Items).Count -ne 1)) {
            throw "'$target' did not return the expected project metadata."
        }
        if ((Get-AssemblySnapshot) -ne '' -or (Test-Path -LiteralPath (Join-Path $appObj 'preparation.txt'))) {
            throw "'$target' compiled or prepared an unbuilt project."
        }
    }

    $queryTrace = Join-Path $appObj 'queries.txt'
    Remove-Item -LiteralPath $queryTrace
    Invoke-GraphMSBuild $consumer 'consumer-graph' $buildArgs
    Assert-PreparedCompilation
    if (@(Get-Content -LiteralPath $queryTrace)[0] -ne 'target-path') {
        throw 'The consumer graph did not request target-path metadata before compilation.'
    }
    Assert-GeneratedArtifacts $appObj
    Assert-GeneratedArtifacts $appBin
    & (Join-Path $appBin 'PreparedApp.exe')
    if ($LASTEXITCODE -ne 0) { throw 'The graph-built application failed.' }

    $snapshot = Get-AssemblySnapshot
    Invoke-GraphMSBuild $consumer 'incremental-graph' $buildArgs
    if ((Get-AssemblySnapshot) -ne $snapshot) {
        throw 'A no-op graph build rewrote assembly outputs.'
    }
    foreach ($target in 'GetTargetPath', 'GetTargetPathWithTargetPlatformMoniker') {
        Invoke-GraphMSBuild $app "warm-$target" @("-t:$target")
    }
    if ((Get-AssemblySnapshot) -ne $snapshot) { throw 'Warm output-path queries rewrote assembly outputs.' }

    $markup = Join-Path $profileRoot 'View.xaml'
    Set-Content -LiteralPath $markup -Value '<View Value="43" />'
    Invoke-GraphMSBuild $consumer 'changed-markup' $buildArgs
    & (Join-Path $appBin 'PreparedApp.exe') 43
    if ($LASTEXITCODE -ne 0) { throw 'Changed markup was not compiled.' }

    $code = Join-Path $profileRoot 'Changed.cs'
    Set-Content -LiteralPath $code -Value 'public class Changed { public const int Value = 1; }'
    $extraSource = $code
    Invoke-GraphMSBuild $consumer 'added-code' $buildArgs
    $interop = Join-Path $appObj 'WinRT.Interop.dll'
    $before = (Get-Item -LiteralPath $interop).LastWriteTimeUtc
    Set-Content -LiteralPath $code -Value 'public class Changed { public const int Value = 2; }'
    Invoke-GraphMSBuild $consumer 'changed-code' $buildArgs
    if ((Get-Item -LiteralPath $interop).LastWriteTimeUtc -le $before) {
        throw 'Changed code did not regenerate interop.'
    }

    $extraReference = 'true'
    $before = (Get-Item -LiteralPath $interop).LastWriteTimeUtc
    Invoke-GraphMSBuild $consumer 'changed-references' ($buildArgs + '-restore')
    if ((Get-Item -LiteralPath $interop).LastWriteTimeUtc -le $before) {
        throw 'Changed project references did not regenerate interop.'
    }
    Remove-Item -LiteralPath $interop
    Remove-Item -LiteralPath (Join-Path $appObj 'WinRT.Sdk.Projection.dll')
    Remove-Item -LiteralPath (Join-Path $appBin 'WinRT.Interop.dll')
    Remove-Item -LiteralPath (Join-Path $appBin 'WinRT.Sdk.Projection.dll')
    Invoke-GraphMSBuild $consumer 'missing-outputs' $buildArgs
    Assert-GeneratedArtifacts $appObj
    Assert-GeneratedArtifacts $appBin

    $compiledAssembly = Join-Path $appObj 'PreparedApp.dll'
    $compiledTimestamp = (Get-Item -LiteralPath $compiledAssembly).LastWriteTimeUtc
    Remove-Item -LiteralPath $interop
    Invoke-GraphMSBuild $app 'publish-no-build' @('-t:Publish', '-p:NoBuild=true')
    if ((Get-Item -LiteralPath $compiledAssembly).LastWriteTimeUtc -ne $compiledTimestamp) {
        throw 'NoBuild publish recompiled the application.'
    }
    Assert-GeneratedArtifacts $appObj
    Assert-GeneratedArtifacts (Join-Path $appBin 'publish')
    $deps = Get-Content -LiteralPath (Join-Path $appBin 'publish\PreparedApp.deps.json') -Raw
    if (-not $deps.Contains('WinRT.Interop') -or -not $deps.Contains('WinRT.Sdk.Projection')) {
        throw 'Publish dependency metadata omitted the generated assemblies.'
    }

    Invoke-GraphMSBuild $app 'clean' @('-t:Clean')
    foreach ($name in 'WinRT.Interop.dll', 'WinRT.Sdk.Projection.dll', 'Markup.g.cs', 'CompileInputs.g.cs') {
        if (Test-Path -LiteralPath (Join-Path $appObj $name)) { throw "Clean left '$name' behind." }
    }

    # Cold copy-item protocols are metadata-only, like target-path protocols
    Invoke-GraphMSBuild $app 'copy-only' @('-t:GetCopyToOutputDirectoryItems')
    if (Test-Path -LiteralPath $compiledAssembly) { throw 'A cold copy-item query compiled the application.' }

    Invoke-GraphMSBuild $app 'design-time-compile' @('-t:Compile', '-p:DesignTimeBuild=true', '-p:SkipCompilerExecution=true')
    if ((Test-Path -LiteralPath $interop) -or (Test-Path -LiteralPath $compiledAssembly)) {
        throw 'Design-time compilation produced runtime artifacts.'
    }

    Invoke-GraphMSBuild $app 'missing-compiled-input' @('-t:Publish', '-p:NoBuild=true') -ExpectedError 'CsWinRT artifact generation requires an existing compiled assembly.'
    Remove-Item -LiteralPath (Join-Path $appObj 'preparation.txt')
    Invoke-GraphMSBuild $app 'explicit-generation' @('-t:CsWinRTGenerateArtifacts')
    Assert-PreparedCompilation
    Assert-GeneratedArtifacts $appObj
    Assert-GeneratedArtifacts $appBin
    $snapshot = Get-AssemblySnapshot
    Invoke-GraphMSBuild $app 'explicit-incremental' @('-t:CsWinRTGenerateArtifacts')
    if ((Get-AssemblySnapshot) -ne $snapshot) { throw 'Warm explicit generation rewrote assembly outputs.' }
}

Write-Host "Graph-build smoke tests passed. Binlogs: '$runRoot'." -ForegroundColor Green
