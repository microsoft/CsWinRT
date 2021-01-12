# Authoring Components

## Overview
**Authoring Support is still in preview**
C#/WinRT is working to provide support for authoring Windows Runtime components. You can write a library in C#, and use C#/WinRT's source generator to get a winmd that any WinRT compatible language can use. For example, a library written in C# can be used by a C++ program, via C#/WinRT and C++/WinRT, with just a few tweaks to the C++ project.


## References
Here are some resources that demonstrate authoring C#/WinRT components and the details discussed in this document.
1. https://github.com/microsoft/CsWinRT/tree/master/src/Tests/AuthoringTest
2. https://github.com/AdamBraden/MyRandom


## Authoring the C# Component
To create a library, select the Class Library (.NET Core) template in Visual Studio. C#/WinRT projects require Windows API version specific .NET frameworks.

Accepted `<TargetFramework>` properties |
--- |
`net5.0-windows10.0.19041.0` |
`net5.0-windows10.0.18362.0` |
`net5.0-windows10.0.17763.0` |

The library you are authoring should specify the following properties in its project file: 
```
  <PropertyGroup>
    <!-- update the Windows API version to reflect your TargetFramework -->
    <CsWinRTWindowsMetadata>10.0.19041.0</CsWinRTWindowsMetadata>
    <CsWinRTComponent>true</CsWinRTComponent>
    <CsWinRTEnableLogging>true</CsWinRTEnableLogging>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <GeneratedFilesDir Condition="'$(GeneratedFilesDir)'==''">$([MSBuild]::NormalizeDirectory('$(MSBuildProjectDirectory)', '$(IntermediateOutputPath)', 'Generated Files'))</GeneratedFilesDir>
  </PropertyGroup>
```
And don't forget to include a `PackageReference` to `Microsoft.Windows.CsWinRT`!


## Known Authoring Issues
1. There are some programs you could write as/in your component that aren't available in the Windows Runtime. 
We are working on implementing diagnostics in our tool that will catch these errors before a winmd is generated for your component.

2. Not all C# types have been mapped to WinRT types, but we are working on completing this coverage. 

3. Composable type support is still in progress

## Using the authored component

To use the component in a C# app, the authored component just needs to be added as a project/package reference.

For native (C++) apps, more steps are needed.

Modifications needed: 
  1. Reference to your authored component in the consuming app
  2. Add a manifest file and runtimeconfig file [for native consumption only] 
  3. Use a MSBuild targets for copying necessary DLLs


### Add the reference to your authored component
**In Visual Studio** Under the Project node, right click on "References", click "Add Reference", then "Browse" and add the `.winmd` file generated in your authored component. 

### For native app (C++) consumption
As part of the native (C++) support, the WinRT Hosting dlls (WinRT.Host and WinRT.Host.Shim) need to be in the same folder as the native executable. 
For now, users need a special target of their own so MSBuild can place the hosting dlls in the correct place. But soon we will implement this so that a package reference to C#/WinRT in the authored component is all that is needed.

You'll need to author some files to assist the hosting process by the native app: `YourNativeApp.exe.manifest` and `WinRT.Host.runtimeconfig.json`. 

If your app is packaged with MSIX, then you don't need to include the manifest file, otherwise you need to include your activatable class registrations in the manifest file.

To do this, **in Visual Studio**, right click on the project node on the "Solution Explorer" window, click "Add", then "New Item". Search for the "Text File" template and name your file "YourNativeApp.exe.manifest".
Repeat this for the "WinRT.Host.runtimeconfig.json" file. 

This process adds the nodes `<Manifest Include=... >` and `<None Include=... >` to your native app's project file -- **you need to update these to have `<DeploymentContent>true</DeploymentContent>` for them to be placed in the output directory with your executable**.  

You should read the [hosting docs](https://github.com/microsoft/CsWinRT/blob/master/docs/hosting.md) as well, for more information on these files.

You'll need to use [C++/WinRT](https://docs.microsoft.com/en-us/windows/uwp/cpp-and-winrt-apis/intro-to-using-cpp-with-winrt) to consume the `winmd` too, so make sure you have it installed, and have added `#include <winrt/YourAuthoredLibrary.h>` to the file `pch.h` of the native app.  

In summary, here is the fragment of additions made to the native app's project file:
```
<ItemGroup>
    <!-- targets to copy the needed DLLs -->
    <None Include="Directory.Build.targets" />
    <!-- the runtimeconfig.json -->
    <None Include="WinRT.Host.runtimeconfig.json">
      <DeploymentContent>true</DeploymentContent>
    </None>
    <!-- the winmd -->
    <Reference Include="YourAuthoredLibrary">
      <HintPath>..\Path\To\Generated\Files\YourAuthoredLibrary.winmd</HintPath>
      <IsWinMDFile>true</IsWinMDFile>
    </Reference>
    <!-- the manifest -->
    <Manifest Include="PosnConsole.exe.manifest">
      <DeploymentContent>true</DeploymentContent>
    </Manifest>
  </ItemGroup> 
```

### Copying DLLs Target

1. In the project folder for the component you are authoring, you will need to add a `PropertyGroup` so MSBuild places the proper WinRT Dlls in the output directory. This is needed by the targets file we need to add to C++ apps to copy the WinRT Dlls to the native app's output directory.

This is generally done via a `Directory.Build.targets` file like so:
```
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
 
  <PropertyGroup>
    <CopyLocalLockFileAssemblies Condition="$(CsWinRTComponent)==true">true</CopyLocalLockFileAssemblies>
  </PropertyGroup>

</Project>
```


2. In your C++ app, add a `Directory.Build.targets` file that copies over the necessary DLLs: 
```
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

  <PropertyGroup>
    <PrepareForRunDependsOn>CopyHostAssets;$(PrepareForRunDependsOn)</PrepareForRunDependsOn>
  </PropertyGroup>
  
  <PropertyGroup>
    <CsWinRTVersion>1.1.0</CsWinRTVersion>
  </PropertyGroup>
  
  <Target Name="CopyHostAssets">
    <Copy SourceFiles="$(NuGetPackageRoot)microsoft.windows.cswinrt\$(CsWinRTVersion)\native\$(Platform)\WinRT.Host.dll"
          DestinationFolder="$(OutDir)" 
          UseHardlinksIfPossible="false" SkipUnchangedFiles="true" />
    
    <Copy SourceFiles="$(NuGetPackageRoot)microsoft.windows.cswinrt\$(CsWinRTVersion)\lib\net5.0\WinRT.Host.Shim.dll"
          DestinationFolder="$(OutDir)" 
          UseHardlinksIfPossible="false" SkipUnchangedFiles="true" />
    
    <!-- Note: the following three source paths may not need the $(Platform) entry for your app -->
    <Copy SourceFiles="..\YourC#Library\bin\$(Platform)\$(Configuration)\net5.0-windows10.0.19041.0\WinRT.Runtime.dll" 
          DestinationFolder="$(OutDir)" 
          UseHardlinksIfPossible="false" SkipUnchangedFiles="true" />

    <Copy SourceFiles="..\YourC#Library\bin\$(Platform)\$(Configuration)\net5.0-windows10.0.19041.0\Microsoft.Windows.SDK.NET.dll"
          DestinationFolder="$(OutDir)"
          UseHardlinksIfPossible="false" SkipUnchangedFiles="true" />
    
    <Copy SourceFiles="..\YourC#Library\bin\$(Platform)\$(Configuration)\net5.0-windows10.0.19041.0\YourC#Library.dll"
          DestinationFolder="$(OutDir)"
          UseHardlinksIfPossible="false" SkipUnchangedFiles="true" />
  </Target>  
  
</Project>

```

