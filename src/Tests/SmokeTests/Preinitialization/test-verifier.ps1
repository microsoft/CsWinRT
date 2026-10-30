#!/usr/bin/env pwsh
#Requires -Version 7.2

<#
.SYNOPSIS
    Exercises fail-closed verifier guards using copies of a successful smoke publish's reports.
#>
[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)] [string] $MstatPath,
    [Parameter(Mandatory = $true)] [string] $AssemblyDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not ('PreinitializationValidation.MetadataNames' -as [type])) {
    Add-Type -Path ([IO.Path]::Combine($PSScriptRoot, 'MetadataNames.cs')), ([IO.Path]::Combine($PSScriptRoot, 'MstatReader.cs'))
}
$verify = [IO.Path]::Combine($PSScriptRoot, 'verify-preinitialization.ps1')
$original = (Get-Item -LiteralPath $MstatPath).FullName
$directory = [IO.Path]::Combine($PSScriptRoot, 'obj', "verifier-$([Guid]::NewGuid().ToString('N'))")
$null = New-Item -ItemType Directory -Path $directory
$fixture = [IO.Path]::Combine($directory, 'Preinitialization.mstat')
$map = [IO.Path]::ChangeExtension($fixture, 'map.xml')
$rsp = [IO.Path]::ChangeExtension($fixture, 'ilc.rsp')
$collisionAssembly = [IO.Path]::Combine($directory, 'Collision.dll')
$collisionSource = @'
using System.Runtime.CompilerServices;

namespace PreinitializationVerifierFixture
{
    public class Outer_Inner { }
    public class Outer_Inner_0 { }
    public class Outer
    {
        public static class Inner
        {
            [FixedAddressValueType]
            public static readonly int Value = 42;
        }
    }
}
'@

function Assert-Rejected {
    param ([string] $Name, [scriptblock] $Mutate, [string] $Expected)

    Copy-Item -LiteralPath $original -Destination $fixture
    Copy-Item -LiteralPath ([IO.Path]::ChangeExtension($original, 'map.xml')) -Destination $map
    Copy-Item -LiteralPath ([IO.Path]::ChangeExtension($original, 'ilc.rsp')) -Destination $rsp
    & $Mutate

    $message = $null
    try {
        & $verify -MstatPath $fixture -AssemblyDirectory $AssemblyDirectory *> $null
    }
    catch {
        $message = $_.Exception.ToString()
    }

    if ($null -eq $message -or $message -notmatch $Expected) {
        throw "Verifier guard '$Name' failed. Expected '$Expected'; got '$message'."
    }
    Write-Host "Verified rejection: $Name." -ForegroundColor DarkGray
}

try {
    Add-Type -OutputAssembly $collisionAssembly -TypeDefinition $collisionSource
    $inventory = [PreinitializationValidation.MetadataNames]::ReadFixedAddressTypes([string[]]@($collisionAssembly))
    if ($inventory.Count -ne 1 -or
        -not @($inventory.Keys)[0].EndsWith('PreinitializationVerifierFixture.Outer+Inner', [StringComparison]::Ordinal) -or
        -not @($inventory.Values)[0].EndsWith('_PreinitializationVerifierFixture_Outer_Inner_1@@', [StringComparison]::Ordinal)) {
        throw 'Native-name collision handling did not reserve the unmarked types and their natural suffix.'
    }
    Write-Host 'Verified native-name collisions with unmarked types.' -ForegroundColor DarkGray

    Assert-Rejected 'missing MSTAT' { Remove-Item -LiteralPath $fixture } 'does not exist'
    Assert-Rejected 'non-MSTAT assembly' {
        Copy-Item -LiteralPath ([IO.Path]::Combine($AssemblyDirectory, 'Preinitialization.dll')) -Destination $fixture
    } 'Unsupported MSTAT format'
    Assert-Rejected 'truncated MSTAT' {
        $bytes = [IO.File]::ReadAllBytes($fixture)
        [IO.File]::WriteAllBytes($fixture, $bytes[0..31])
    } 'BadImageFormatException|Image is too small'
    Assert-Rejected 'missing control constructor' {
        # A reversible byte encoding preserves the PE while changing the shared metadata method name.
        $bytes = [IO.File]::ReadAllBytes($fixture)
        $text = [Text.Encoding]::Latin1.GetString($bytes)
        if (-not $text.Contains(".cctor`0")) {
            throw 'The fixture has no .cctor metadata name to replace.'
        }
        [IO.File]::WriteAllBytes($fixture, [Text.Encoding]::Latin1.GetBytes($text.Replace(".cctor`0", ".xctor`0")))
    } 'runtime-initialized control'
    Assert-Rejected 'unsafe method folding' {
        $text = [IO.File]::ReadAllText($rsp)
        [IO.File]::WriteAllText($rsp, $text.Replace('--methodbodyfolding:none', '--methodbodyfolding:all'))
    } 'requires --methodbodyfolding:none'
    Assert-Rejected 'conflicting method folding' {
        [IO.File]::AppendAllText($rsp, "`n--methodbodyfolding:all`n")
    } 'requires --methodbodyfolding:none'
    Assert-Rejected 'missing static storage' {
        [IO.File]::WriteAllText($map, '<ObjectNodes />')
    } 'no NonGCStatics records'
    Assert-Rejected 'missing representative table' {
        $document = [Xml.Linq.XDocument]::Load($map)
        $nodes = @($document.Root.Elements('NonGCStatics') | Where-Object {
            $_.Attribute('Name').Value -eq '?__NONGCSTATICS@WinRT_Runtime_WindowsRuntime_InteropServices_IUnknownImpl@@'
        })
        if ($nodes.Count -ne 1) {
            throw 'The successful fixture must contain exactly one IUnknownImpl static block.'
        }
        $nodes[0].Remove()
        $document.Save($map)
    } 'coverage is missing.*IUnknownImpl'
}
finally {
    foreach ($path in @($fixture, $map, $rsp, $collisionAssembly)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path
        }
    }
    Remove-Item -LiteralPath $directory
}
