#!/usr/bin/env pwsh
#Requires -Version 7.2

<#
.SYNOPSIS
    Rejects native static constructors for FixedAddressValueType owners in a smoke-test MSTAT.
.PARAMETER MstatPath
    The Preinitialization.mstat produced by ILC, alongside its .map.xml and .ilc.rsp.
.PARAMETER AssemblyDirectory
    The matching pre-publish bin directory containing the actual WinRT implementation assemblies.
#>
[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)] [string] $MstatPath,
    [Parameter(Mandatory = $true)] [string] $AssemblyDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not ('PreinitializationValidation.MstatReader' -as [type])) {
    Add-Type -Path ([IO.Path]::Combine($PSScriptRoot, 'MetadataNames.cs')), ([IO.Path]::Combine($PSScriptRoot, 'MstatReader.cs'))
}

$assemblyNames = @('WinRT.Runtime', 'WinRT.Sdk.Projection', 'WinRT.Sdk.Xaml.Projection', 'WinRT.Projection', 'WinRT.Interop')
$assemblyPaths = @($assemblyNames | ForEach-Object {
    (Get-Item -LiteralPath ([IO.Path]::Combine($AssemblyDirectory, "$_.dll"))).FullName
})

$eligibleTypes = [PreinitializationValidation.MetadataNames]::ReadFixedAddressTypes($assemblyPaths)
$mstatFile = Get-Item -LiteralPath $MstatPath
$ilcArgs = Get-Content -LiteralPath ([IO.Path]::ChangeExtension($mstatFile.FullName, 'ilc.rsp'))
$foldingArgs = @($ilcArgs | Where-Object { $_ -cmatch '^--methodbodyfolding:' })
if ($foldingArgs.Count -ne 1 -or $foldingArgs[0] -cne '--methodbodyfolding:none') {
    throw 'This combined MSTAT/map check requires --methodbodyfolding:none: .NET 10 can omit folded-method records with multiple dumpers.'
}
$mstat = [PreinitializationValidation.MstatReader]::Read($mstatFile.FullName)
$staticData = [PreinitializationValidation.MstatReader]::ReadStaticData([IO.Path]::ChangeExtension($mstatFile.FullName, 'map.xml'))

if (-not $mstat.Constructors.ContainsKey('[Preinitialization]Preinitialization.RuntimeInitializedControl')) {
    throw 'MSTAT does not contain the runtime-initialized control .cctor. The report is incomplete or belongs to a different app.'
}

$failures = @($mstat.Constructors.Keys | Where-Object { $eligibleTypes.ContainsKey($_) } | Sort-Object)
if ($failures.Count -ne 0) {
    $details = $failures | ForEach-Object { "  ${_}::.cctor ($($mstat.Constructors[$_]))" }
    throw "ILC did not preinitialize $($failures.Count) FixedAddressValueType owner(s):`n$($details -join "`n")"
}

$rootedTypes = @(foreach ($type in $eligibleTypes.Keys) {
    # Match whole NonGCStatics symbols, never an EEType or a metadata/string-heap occurrence.
    $symbol = $eligibleTypes[$type]
    if ($staticData.ContainsKey($symbol) -and $staticData[$symbol] -gt 0) {
        $type
    }
})
foreach ($assembly in $assemblyNames) {
    $count = @($rootedTypes | Where-Object { $_.StartsWith("[$assembly]", [StringComparison]::Ordinal) }).Count
    if ($count -eq 0) {
        throw "MSTAT contains no native data for FixedAddressValueType owners in '$assembly'."
    }
    Write-Host "${assembly}: $count rooted table owners, no native .cctor." -ForegroundColor DarkGray
}

function Assert-RootedFamily {
    param ([string] $Name, [string] $Pattern)

    if (@($rootedTypes | Where-Object { $_ -cmatch $Pattern }).Count -eq 0) {
        throw "Preinitialization coverage is missing '$Name' (expected native static data for an owner matching '$Pattern')."
    }
}

function Assert-RootedType {
    param ([string] $Assembly, [string] $Namespace, [string] $Name)

    # C# file-local helpers carry a compiler-generated prefix; their identities above remain exact.
    $pattern = '^\[' + [regex]::Escape($Assembly) + '\]' + [regex]::Escape($Namespace) +
        '\.(?:[^.]*__)?' + [regex]::Escape($Name) + '$'
    Assert-RootedFamily "$Assembly/$Name" $pattern
}

foreach ($name in @(
    'IUnknownImpl', 'IInspectableImpl', 'IStringableImpl', 'IWeakReferenceSourceImpl',
    'FreeThreadedMarshalImpl', 'RoBufferMarshalImpl', 'OtherTypePropertyValueImpl',
    'Int32ArrayPropertyValueImpl', 'StringArrayPropertyValueImpl', 'OtherTypeArrayPropertyValueImpl'
)) {
    Assert-RootedType 'WinRT.Runtime' 'WindowsRuntime.InteropServices' $name
}
foreach ($name in @('Int32ReferenceImpl', 'Int32PropertyValueImpl', 'Int32InterfaceEntriesImpl', 'EventHandlerImpl', 'EventHandlerReferenceImpl', 'EventHandlerInterfaceEntriesImpl')) {
    Assert-RootedType 'WinRT.Runtime' 'ABI.System' $name
}
Assert-RootedType 'WinRT.Runtime' 'ABI.WindowsRuntime.InteropServices' 'WindowsRuntimePinnedArrayBufferInterfaceEntriesImpl'
Assert-RootedType 'WinRT.Runtime' 'ABI.WindowsRuntime.InteropServices' 'WindowsRuntimePinnedArrayBufferByteAccessImpl'
Assert-RootedType 'WinRT.Sdk.Projection' 'ABI.Windows.ApplicationModel.Background' 'IBackgroundTaskImpl'
Assert-RootedType 'WinRT.Sdk.Xaml.Projection' 'ABI.Windows.UI.Xaml.Data' 'IValueConverterImpl'
Assert-RootedType 'WinRT.Projection' 'ABI.Authoring' 'IThermometerImpl'

foreach ($entry in @(
    @('WinRT.Sdk.Projection', 'ABI.Windows.System.Threading', 'WorkItemHandler'),
    @('WinRT.Projection', 'ABI.Authoring', 'TemperatureChangedHandler')
)) {
    foreach ($suffix in @('Impl', 'ReferenceImpl', 'InterfaceEntriesImpl')) {
        Assert-RootedType $entry[0] $entry[1] ($entry[2] + $suffix)
    }
}
foreach ($entry in @(
    @('WinRT.Sdk.Projection', 'ABI.Windows.Storage', 'FileAttributes'),
    @('WinRT.Sdk.Projection', 'ABI.Windows.UI', 'Color'),
    @('WinRT.Sdk.Projection', 'ABI.Windows.Web.Http', 'HttpProgress'),
    @('WinRT.Projection', 'ABI.Authoring', 'Season'),
    @('WinRT.Projection', 'ABI.Authoring', 'Measurement')
)) {
    foreach ($suffix in @('ReferenceImpl', 'InterfaceEntriesImpl')) {
        Assert-RootedType $entry[0] $entry[1] ($entry[2] + $suffix)
    }
}

foreach ($name in @('IEnumerable', 'IEnumerator', 'IList', 'IReadOnlyList', 'IDictionary', 'IReadOnlyDictionary')) {
    Assert-RootedFamily "generic $name vtable" ("^\[WinRT\.Interop\]ABI\.System\.Collections\.Generic\.<[^>]+>$name'[12]<.+>Impl$")
}
foreach ($name in @('IObservableVector', 'IObservableMap', 'IMapChangedEventArgs')) {
    Assert-RootedFamily "generic $name vtable" ("^\[WinRT\.Interop\]ABI\.Windows\.Foundation\.Collections\.<[^>]+>$name'[12]<.+>Impl$")
}
foreach ($name in @('IAsyncOperation', 'IAsyncActionWithProgress', 'IAsyncOperationWithProgress')) {
    Assert-RootedFamily "generic $name vtable" ("^\[WinRT\.Interop\]ABI\.Windows\.Foundation\.<[^>]+>$name'[12]<.+>Impl$")
}
foreach ($suffix in @('Impl', 'ReferenceImpl', 'InterfaceEntriesImpl')) {
    Assert-RootedFamily "generic delegate $suffix" ("^\[WinRT\.Interop\]ABI\.System\.<[^>]+>EventHandler'[12]<.+>$suffix$")
}
foreach ($suffix in @('Impl', 'InterfaceEntriesImpl')) {
    Assert-RootedFamily "key/value pair $suffix" ("^\[WinRT\.Interop\]ABI\.System\.Collections\.Generic\.<[^>]+>KeyValuePair'2<.+>$suffix$")
    Assert-RootedFamily "array $suffix" ("^\[WinRT\.Interop\]ABI\..+Array$suffix$")
}
Assert-RootedFamily 'user-defined CCW interface entries' '^\[WinRT\.Interop\]WindowsRuntime\.Interop\.UserDefinedTypes\..+InterfaceEntriesImpl$'

Write-Host "Verified preinitialization of $($rootedTypes.Count) rooted table owners ($($eligibleTypes.Count) eligible types inspected)." -ForegroundColor Green
