# Authoring Components

## Overview
C#/WinRT provides support for authoring Windows Runtime components. You can write a library in C#, and use C#/WinRT's source generator to get a winmd that any WinRT compatible language can use. For example, a library written in C# can be used by a C++ program, via C#/WinRT and C++/WinRT, with just a few tweaks to the C++ project.


## Authoring the C# Component
To create a library, select the Class Library (.NET Core) template in Visual Studio. 
Make sure the `<TargetFramework>` for the project is one of `net5.0-windows10.0.19041.0`, `net5.0-windows10.0.18362.0` or `net5.0-windows10.0.17763.0`. 
And add the following to your library's project file:
```
  <PropertyGroup>
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

2. Not all C# types have been mapped to runtime types, but we are working on completing this coverage. 

3. Composable type support is still in progress

## Using the authored component in C++
As part of the native (C++) support, the WinRT Hosting dlls (WinRT.Host and WinRT.Host.Shim) need to be in the same folder as the native executable. 
For now, users need a special target of their own so MSBuild can place the hosting dlls in the correct place. But soon we will implement this so that a package reference to C#/WinRT in the authored component is all that is needed.   

Modifications needed: 
  1. [for native consumption] add a manifest file and runtimeconfig file 
  2. update the consuming app's project file (e.g. `YourApp.vcxproj`)
  3. add a target for copying the required DLLs.


### Manifest and RuntimeConfig
You'll need to author some files to assist the hosting process by the native app: `YourNativeApp.exe.manifest` and `WinRT.Host.runtimeconfig.json`. 
For information on writing these, see the [hosting docs](https://github.com/microsoft/CsWinRT/blob/master/docs/hosting.md).

### Project File
When updating the app's project file, you'll need to add a `<Reference Include="YourAuthoredLibrary">` node that contains (1) the path to your component's generated WinMD file, (2) the `<IsWinMDFile>` attribute with value `true`. 

For consumption by native components, you'll also need to add `Include` statements for your hosting `runtimeconfig.json` file and the consuming app's `exe.manifest` file. More information on these files can be found in the hosting docs.   

### Copying DLLs Target

In your C++ app, add a `Directory.Build.targets` file that copies over the necessary DLLs: 
```
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

  <PropertyGroup>
    <PrepareForRunDependsOn>CopyHostAssets;$(PrepareForRunDependsOn)</PrepareForRunDependsOn>
  </PropertyGroup>
  
  <PropertyGroup>
    <CsWinRTVersion>1.1.0</CsWinRTVersion>
  </PropertyGroup>
  
  <Target Name="CopyHostAssets">
    <Copy SourceFiles="Path\To\Nuget\Packages\microsoft.windows.cswinrt\$(CsWinRTVersion)\native\$(Platform)\WinRT.Host.dll"
          DestinationFolder="$(OutDir)" 
          UseHardlinksIfPossible="false" SkipUnchangedFiles="true" />
    
    <Copy SourceFiles="Path\To\Nuget\Packages\microsoft.windows.cswinrt\$(CsWinRTVersion)\lib\net5.0\WinRT.Host.Shim.dll"
          DestinationFolder="$(OutDir)" 
          UseHardlinksIfPossible="false" SkipUnchangedFiles="true" />

    <Copy SourceFiles="..\PosnLibrary\bin\$(Platform)\$(Configuration)\net5.0-windows10.0.19041.0\WinRT.Runtime.dll" 
          DestinationFolder="$(OutDir)" 
          UseHardlinksIfPossible="false" SkipUnchangedFiles="true" />

    <Copy SourceFiles="..\PosnLibrary\bin\$(Platform)\$(Configuration)\net5.0-windows10.0.19041.0\Microsoft.Windows.SDK.NET.dll"
          DestinationFolder="$(OutDir)"
          UseHardlinksIfPossible="false" SkipUnchangedFiles="true" />
    
    <Copy SourceFiles="..\PosnLibrary\bin\$(Platform)\$(Configuration)\net5.0-windows10.0.19041.0\PosnLibrary.dll"
          DestinationFolder="$(OutDir)"
          UseHardlinksIfPossible="false" SkipUnchangedFiles="true" />
  </Target>  
  
</Project>

```

## References
Here are some resources that demonstrate authoring C#/WinRT components and the changes discussed above.
1. https://github.com/microsoft/CsWinRT/tree/master/Authoring/AuthoringConsumptionTest
2. https://github.com/AdamBraden/MyRandom
