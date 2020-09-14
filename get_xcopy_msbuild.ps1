function RedirectCppTasks($filePath) {
    $config = New-Object -TypeName "System.XML.XMLDocument"
    $config.Load($filePath)
    $namespaces = New-Object -TypeName "Xml.XmlNamespaceManager" -ArgumentList $config.NameTable
    $namespaces.AddNamespace("ns", "urn:schemas-microsoft-com:asm.v1")
    $redirect = $config.SelectSingleNode("//ns:assemblyIdentity[@name='Microsoft.Build.CPPTasks.Common']/../ns:bindingRedirect", $namespaces)
    $redirect.newVersion = "16.7.0.0"
    $config.Save($filePath)
}

$packageName = 'RoslynTools.MSBuild'
$packageVersion = "16.8.0-preview3"
$packageDir = ".\.msbuild"
$packagePath = Join-Path $packageDir "$packageName.$packageVersion.nupkg"
New-Item -Path $packageDir -Force -ItemType 'Directory' | Out-Null
Invoke-WebRequest "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/flat2/$packageName/$packageVersion/$packageName.$packageVersion.nupkg" -OutFile $packagePath
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::ExtractToDirectory($packagePath, $packageDir)

# Modify msbuild.exe.config files to use Microsoft.Build.CPPTasks.Common.dll version 16.7
RedirectCppTasks(".msbuild\tools\MSBuild\Current\Bin\MSBuild.exe.config")
RedirectCppTasks(".msbuild\tools\MSBuild\Current\Bin\amd64\MSBuild.exe.config")
