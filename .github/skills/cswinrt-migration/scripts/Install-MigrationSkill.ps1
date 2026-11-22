<#
.SYNOPSIS
Installs the self-contained CsWinRT migration skill in a consuming repository.
#>
[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string] $RepositoryPath,

    [switch] $Force
)

$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path -LiteralPath $RepositoryPath).Path
if (-not (Test-Path -LiteralPath $repository -PathType Container)) {
    throw "RepositoryPath must be an existing directory."
}

$skillRoot = Split-Path -Parent $PSScriptRoot
$guide = Join-Path $skillRoot '..\..\..\docs\migration-2-to-3.md'
$target = Join-Path $repository '.github\skills\cswinrt-migration'
if ((Test-Path -LiteralPath $target) -and -not $Force) {
    throw "The migration skill already exists at '$target'. Use -Force to update its installed files."
}

if (-not (Test-Path -LiteralPath $guide -PathType Leaf)) {
    throw "Run the installer from the CsWinRT source checkout; its migration guide is missing."
}

New-Item -ItemType Directory -Path (Join-Path $target 'references') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $skillRoot 'SKILL.md') -Destination (Join-Path $target 'SKILL.md') -Force
Copy-Item -LiteralPath $guide -Destination (Join-Path $target 'references\migration-guide.md') -Force
Write-Output "Installed cswinrt-migration at '$target'."
