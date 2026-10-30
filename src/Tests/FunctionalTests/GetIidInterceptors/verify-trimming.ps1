#Requires -Version 7.2
[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)] [string] $MstatPath,
    [Parameter(Mandatory = $true)] [string] $AssemblyPath,
    [switch] $ExpectFallback
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$readerDirectory = [IO.Path]::GetFullPath([IO.Path]::Combine($PSScriptRoot, '..', '..', 'SmokeTests', 'Preinitialization'))
if (-not ('PreinitializationValidation.MstatReader' -as [type])) {
    Add-Type -Path ([IO.Path]::Combine($readerDirectory, 'MetadataNames.cs')), ([IO.Path]::Combine($readerDirectory, 'MstatReader.cs'))
}

$mstatFile = Get-Item -LiteralPath $MstatPath
$ilcArguments = Get-Content -LiteralPath ([IO.Path]::ChangeExtension($mstatFile.FullName, 'ilc.rsp'))
if (@($ilcArguments | Where-Object { $_ -ceq '--methodbodyfolding:none' }).Count -ne 1) {
    throw 'The combined MSTAT/map check requires --methodbodyfolding:none.'
}

$stream = [IO.File]::OpenRead((Get-Item -LiteralPath $AssemblyPath).FullName)
$pe = [System.Reflection.PortableExecutable.PEReader]::new($stream)
try {
    $metadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
    $lookupReferences = @(
        foreach ($handle in $metadata.MemberReferences) {
            $member = $metadata.GetMemberReference($handle)
            if ($metadata.GetString($member.Name) -ceq 'GetIid' -and
                [PreinitializationValidation.MetadataNames]::GetTypeName($metadata, $member.Parent) -ceq
                    '[WinRT.Runtime]WindowsRuntime.InteropServices.WindowsRuntimeMarshal') {
                $handle
            }
        }
    )
}
finally {
    $pe.Dispose()
    $stream.Dispose()
}

$mstat = [PreinitializationValidation.MstatReader]::Read($mstatFile.FullName)
$nativeIidRecords = @(
    @($mstat.Methods) + @($mstat.Types) | Where-Object {
        $_ -cmatch 'WindowsRuntimeIid(?:Info|TypeName|TypeMapGroup)' -or
        $_ -cmatch 'WindowsRuntimeMarshal::GetIid$' -or
        $_ -cmatch 'IidProvider'
    }
)

# Native EEType/data nodes cover helpers whose methods were completely inlined
$settings = [Xml.XmlReaderSettings]::new()
$settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
$settings.XmlResolver = $null
$xmlReader = [Xml.XmlReader]::Create([IO.Path]::ChangeExtension($mstatFile.FullName, 'map.xml'), $settings)
$map = [Xml.XmlDocument]::new()
try {
    $map.Load($xmlReader)
}
finally {
    $xmlReader.Dispose()
}
$nativeIidNodes = @($map.ObjectNodes.ChildNodes | Where-Object {
    $_.NodeType -eq [Xml.XmlNodeType]::Element -and
    $_.GetAttribute('Name') -cmatch 'WindowsRuntimeIid(?:Info|TypeName|TypeMapGroup)|IidProvider'
})

if ($ExpectFallback) {
    if ($lookupReferences.Count -eq 0 -or
        @($mstat.Types | Where-Object { $_ -ceq '[WinRT.Runtime]WindowsRuntime.InteropServices.WindowsRuntimeIidInfo' }).Count -eq 0 -or
        $nativeIidNodes.Count -eq 0) {
        throw 'The negative control did not retain the real GetIid reference and native IID-map infrastructure.'
    }
}
elseif ($lookupReferences.Count -ne 0 -or $nativeIidRecords.Count -ne 0 -or $nativeIidNodes.Count -ne 0) {
    throw "IID lookup infrastructure survived interception: $($lookupReferences.Count) IL reference(s), $($nativeIidRecords.Count) native record(s), $($nativeIidNodes.Count) native node(s).`n$($nativeIidRecords -join "`n")"
}

Write-Host "Verified IID trimming (fallback=$ExpectFallback): $($lookupReferences.Count) original IL reference(s), $($nativeIidRecords.Count) native record(s), $($nativeIidNodes.Count) native node(s); $($mstat.MethodCount) native methods inspected."
