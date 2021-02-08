# Authoring Components

## Overview

**Note: Authoring Support is still in preview**

C#/WinRT provides support for authoring Windows Runtime components. You can write a library in C#, and specify that it is a `CsWinRTComponent` for C#/WinRT to produce a WinMD that any WinRT compatible language can use. For example, a library written in C# can be used by a C++ program, via C#/WinRT and C++/WinRT.
Managed apps only need a project or package reference to the authored component, and native apps will need some extra steps that we cover in this documentation.

## References
Here are some resources that demonstrate authoring C#/WinRT components and the details discussed in this document.
1. [Simple C#/WinRT component sample](https://github.com/microsoft/CsWinRT/tree/master/src/Samples/AuthoringDemo) and associated [walkthrough](https://docs.microsoft.com/en-us/windows/uwp/csharp-winrt/create-windows-runtime-component-cswinrt) on creating a C#/WinRT component and consuming it from C++/WinRT

2. https://github.com/microsoft/CsWinRT/tree/master/src/Tests/AuthoringTest

3. https://github.com/microsoft/CsWinRT/tree/master/src/Tests/AuthoringConsumptionTest

## Authoring the C# Component
To create a library, select the Class Library (.NET Core) template in Visual Studio. C#/WinRT projects require Windows API version specific .NET frameworks.

Accepted `<TargetFramework>` versions |
--- |
`net5.0-windows10.0.19041.0` |
`net5.0-windows10.0.18362.0` |
`net5.0-windows10.0.17763.0` |

The library you are authoring should specify the following properties in its project file: 

``` csproj
<PropertyGroup>
  <!-- Choose your TargetFramework for the desired Windows SDK projection -->
  <TargetFramework>net5.0-windows10.0.19041.0</TargetFramework>
  <CsWinRTComponent>true</CsWinRTComponent>
  <CsWinRTWindowsMetadata>10.0.19041.0</CsWinRTWindowsMetadata>
  <CsWinRTEnableLogging>true</CsWinRTEnableLogging>
</PropertyGroup>
```
And don't forget to include a `PackageReference` to `Microsoft.Windows.CsWinRT`!

## Using an Authored Component in a Native App

You'll need to author some files to assist the hosting process of a consuming native app: `YourNativeApp.exe.manifest` and `WinRT.Host.runtimeconfig.json`. 
If your app is packaged with MSIX, then you don't need to include the manifest file, otherwise you need to include your activatable class registrations in the manifest file.

To add these files, **in Visual Studio**, right click on the project node on the "Solution Explorer" window, click "Add", then "New Item". 
Search for the "Text File" template and name your file `YourNativeApp.exe.manifest`.
Repeat this for the `WinRT.Host.runtimeconfig.json` file. 
For each item, right-click on it in the "Solution Explorer" window of Visual Studio; then select "Properties" and change the "Content" property to "Yes" using the drop-down arrow on the right -- this ensures it will be added to the output directory of your solution.

We have some [hosting docs](https://github.com/microsoft/CsWinRT/blob/master/docs/hosting.md) as well, that provide more information on these files.

For consuming by "PackageReference", this is all that is required. C++ apps will need to use [C++/WinRT](https://docs.microsoft.com/en-us/windows/uwp/cpp-and-winrt-apis/intro-to-using-cpp-with-winrt) to consume the authored component. So make sure you have C++/WinRT installed, and have added `#include <winrt/MyAuthoredComponent.h>` to the file `pch.h` of the native app.  

## Native Consumption by Project Reference

If you choose to consume your component through a project reference in a native app, then some modifications to the native app's `.vcxproj` file are needed.
Because dotnet will assume a `TargetFramework` for your app that conflicts with `net5`, we need to specify the `TargetFramwork`, `TargetFrameworkVersion` and `TargetRuntime`. 
Examples of this are seen in the code snippet below. This is needed for this preview version, as we continue working on proper support.

You will need to add a reference to both the C#/WinRT component's `csproj` file, and the WinMD produced for the component. 
The WinMD can be found in the output directory of the authored component's project.

Here are the additions made to the native app's project file:
``` vcxproj
<!-- Note: this property group is only required if you are using a project reference, 
           and is a part of the preview while we work on proper support -->
<PropertyGroup>
  <TargetFrameworkVersion>net5.0</TargetFrameworkVersion>
  <TargetFramework>native</TargetFramework>
  <TargetRuntime>Native</TargetRuntime>
</PropertyGroup>
```

Project references for managed apps only need the reference to the authored component's project file.

## Packaging
To generate a NuGet package for the component, you can simply right click on the project and select **Pack**. Alternatively, you can add the following property to the library project file to automatically generate a NuGet package on build: `GeneratePackageOnBuild`. 

To make your component available as a NuGet package, it is important to include the DLLs necessary for C#/WinRT hosting. 
When you pack your C#/WinRT component the DLLs/WinMD are automatically added to your nupkg, based on a nuspec generated from your project file. 

**If you are going to write your own nuspec**, then you should make sure your nuspec contains the following ```file``` entries for your component ```MyAuthoredComponent``` (note: your TargetFramework may vary). This is so our targets that supply the DLLs for any consumers of your package work.  
Similarly, any other dependencies, e.g. `Microsoft.WinUI`, will need to be included in your nuspec as well.

``` nuspec
<files>
  <file src="$(TargetDir)MyAuthoredComponent.dll"        target="lib\$(TargetFramework)\MyAuthoredComponent.dll" />
  <file src="$(TargetDir)MyAuthoredComponent.winmd"      target="lib\$(TargetFramework)\winmd\MyAuthoredComponent.winmd" />
  
  <file src="$(TargetDir)Microsoft.Windows.SDK.NET.dll"  target="lib\$(TargetFramework)\Microsoft.Windows.SDK.NET.dll" />
   
  <!-- Note: you must rename the CsWinRt.Authoring.Targets as follows -->
  <file src="C:\Path\To\CsWinRT\NugetDir\buildTransitive\Microsoft.Windows.CsWinRT.Authoring.targets"   
        target="buildTransitive\MyAuthoredComponent.targets" />
        
  <!-- buildTransitive is for consumers using packagereference, build is for consumers using packages.config --> 
  <file src="C:\Path\To\CsWinRT\NugetDir\build\Microsoft.Windows.CsWinRT.Authoring.targets"       
        target="build\MyAuthoredComponent.targets" />
   
  <!-- Include the managed DLLs -->
  <file src="C:\Path\To\CsWinRT\NugetDir\lib\net5.0\WinRT.Host.Shim.dll"                                  
        target="lib\$(TargetFramework)\WinRT.Host.Shim.dll" />
    
  <file src="C:\Path\To\CsWinRT\NugetDir\lib\net5.0\WinRT.Runtime.dll"                                  
        target="lib\$(TargetFramework)\WinRT.Runtime.dll" />
    
  <!-- Include the native DLLs -->
  <file src="C:\Path\To\CsWinRT\NugetDir\runtimes\win-x64\native\WinRT.Host.dll"                                  
        target="runtimes\win-x64\native\WinRT.Host.dll" />
    
  <file src="C:\Path\To\CsWinRT\NugetDir\runtimes\win-x86\native\WinRT.Host.dll"                                  
        target="runtimes\win-x86\native\WinRT.Host.dll" />
</files>
```

Your component can then be added as a PackageReference to any consumer. 



## Known Authoring Issues
You can follow along [here](https://github.com/microsoft/CsWinRT/issues/663) as we develop authoring support. 
