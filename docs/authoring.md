# Authoring Components

## Overview
**Authoring Support is still in preview**

C#/WinRT is working to provide support for authoring Windows Runtime components. You can write a library in C#, and specify that it is a `CsWinRTComponent` for C#/WinRT to produce a WinMD that any WinRT compatible language can use. For example, a library written in C# can be used by a C++ program, via C#/WinRT and C++/WinRT.


## References
Here are some resources that demonstrate authoring C#/WinRT components and the details discussed in this document.
1. https://github.com/microsoft/CsWinRT/tree/master/src/Tests/AuthoringTest

2. https://github.com/microsoft/CsWinRT/tree/master/src/Tests/AuthoringConsumptionTest

3. https://github.com/AdamBraden/MyRandom


## Authoring the C# Component
To create a library, select the Class Library (.NET Core) template in Visual Studio. C#/WinRT projects require Windows API version specific .NET frameworks.

Accepted `<TargetFramework>` properties |
--- |
`net5.0-windows10.0.19041.0` |
`net5.0-windows10.0.18362.0` |
`net5.0-windows10.0.17763.0` |

The library you are authoring should specify the following properties in its project file: 
``` csproj
<PropertyGroup>
  <!-- update the Windows API version to reflect your TargetFramework -->
  <CsWinRTWindowsMetadata>10.0.19041.0</CsWinRTWindowsMetadata>
  <CsWinRTComponent>true</CsWinRTComponent>
  <CsWinRTEnableLogging>true</CsWinRTEnableLogging>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  <GeneratedFilesDir Condition="'$(GeneratedFilesDir)'==''">$([MSBuild]::NormalizeDirectory('$(MSBuildProjectDirectory)', '$(IntermediateOutputPath)', 'Generated Files)</GeneratedFilesDir>
</PropertyGroup>
```
And don't forget to include a `PackageReference` to `Microsoft.Windows.CsWinRT`!


## Using your authored component
To use the component in a C# app, the authored component just needs to be added as a project/package reference.

For native (C++) apps, there are DLLs needed to host your authored component. When you use the automatic nuget packaging on-build support (in Visual Studio) to make a nupkg for your runtime component, the DLLs/WinMD are automatically added to your nupkg, before the ```GenerateNuspec``` MSBuild step.

**If you are going to write your own nuspec, i.e. not rely on automatic packaging** then the CsWinRT target that adds the hosting dlls to your package will not run, and you should make sure your nuspec contains the following ```file``` entries for ```MyAuthoredComponent``` (note: your TargetFramework may vary). If adding a file entry for ```Coords.targets``` does not work, then update the project file per the above example. 

``` nuspec
<files>
  <file src="$(TargetDir)MyAuthoredComponent.dll"        target="lib\native\MyAuthoredComponent.dll" />
  <file src="$(TargetDir)MyAuthoredComponent.winmd"      target="winmd\MyAuthoredComponent.winmd" />
  
  <file src="$(TargetDir)Microsoft.Windows.SDK.NET.dll"  target="lib\native\Microsoft.Windows.SDK.NET.dll" />
   
  <!-- Note: you must rename the CsWinRt.Authoring.Targets as follows -->
  <file src="C:\Path\To\CsWinRT\NugetDir\buildTransitive\Microsoft.Windows.CsWinRT.Authoring.targets"   
        target="buildTransitive\MyAuthoredComponent.targets" />
   
  <file src="C:\Path\To\CsWinRT\NugetDir\build\Microsoft.Windows.CsWinRT.Authoring.targets"       
        target="build\MyAuthoredComponent.targets" />
   
  <!-- Include the managed DLLs -->
  <file src="C:\Path\To\CsWinRT\NugetDir\lib\net5.0\WinRT.Host.Shim.dll"                                  
        target="lib\native\WinRT.Host.Shim.dll" />
    
  <file src="C:\Path\To\CsWinRT\NugetDir\lib\net5.0\WinRT.Runtime.dll"                                  
        target="lib\native\WinRT.Runtime.dll" />
    
  <!-- Include the native DLLs -->
  <file src="C:\Path\To\CsWinRT\NugetDir\runtimes\win-x64\native\WinRT.Host.dll"                                  
        target="runtimes\win-x64\native\WinRT.Host.dll" />
    
  <file src="C:\Path\To\CsWinRT\NugetDir\runtimes\win-x86\native\WinRT.Host.dll"                                  
        target="runtimes\win-x86\native\WinRT.Host.dll" />
</files>
```

### For native app (C++) consumption
Install your authored component's package -- this will come with a targets file that automatically adds a reference to the component's WinMD and copies the dlls necessary for native support.

You'll need to use [C++/WinRT](https://docs.microsoft.com/en-us/windows/uwp/cpp-and-winrt-apis/intro-to-using-cpp-with-winrt) to consume your API. So make sure you have C++/WinRT installed, and have added `#include <winrt/MyAuthoredComponent.h>` to the file `pch.h` of the native app.  

You'll need to author some files to assist the hosting process by the native app: `YourNativeApp.exe.manifest` and `WinRT.Host.runtimeconfig.json`. 

If your app is packaged with MSIX, then you don't need to include the manifest file, otherwise you need to include your activatable class registrations in the manifest file.

To do this, **in Visual Studio**, right click on the project node on the "Solution Explorer" window, click "Add", then "New Item". Search for the "Text File" template and name your file "YourNativeApp.exe.manifest".
Repeat this for the "WinRT.Host.runtimeconfig.json" file. 

This process adds the nodes `<Manifest Include=... >` and `<None Include=... >` to your native app's project file -- **you need to update these to have `<DeploymentContent>true</DeploymentContent>` for them to be placed in the output directory with your executable**.  

You should read the [hosting docs](https://github.com/microsoft/CsWinRT/blob/master/docs/hosting.md) as well, for more information on these files.

In summary, here is the fragment of additions made to the native app's project file:
``` vcxproj
<ItemGroup>
    <!-- the runtimeconfig.json -->
    <None Include="WinRT.Host.runtimeconfig.json">
      <DeploymentContent>true</DeploymentContent>
    </None>
    <!-- the manifest -->
    <Manifest Include="YourNativeApp.exe.manifest">
      <DeploymentContent>true</DeploymentContent>
    </Manifest>
</ItemGroup> 
```

## Known Authoring Issues
You can follow along [here](https://github.com/microsoft/CsWinRT/issues/663) as we develop authoring support. 
