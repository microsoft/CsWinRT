# Authoring Components

## Overview

**Note: Authoring Support is still in preview**

C#/WinRT provides support for authoring Windows Runtime components. You can write a library in C#, and specify that it is a `CsWinRTComponent` for C#/WinRT to produce a WinMD that any WinRT compatible language can use. For example, a library written in C# can be used by a C++ program, via C#/WinRT and C++/WinRT.
Managed apps only need a project or package reference to the authored component, and native apps will need some extra steps that we cover in this documentation.

## Authoring a C#/WinRT Component

To author your component, first create a project using the **Class Library (.NET Core)** template in Visual Studio. You will need to make the following modifications to the project.

1. In the library project file, update the `TargetFramework` property.

      ```xml
      <PropertyGroup>
            <!-- Choose your TargetFramework for the desired Windows SDK projection -->
            <TargetFramework>net5.0-windows10.0.19041.0</TargetFramework>
      </PropertyGroup>
      ```

      C#/WinRT projects require Windows API version specific .NET frameworks. The following versions are supported:

      - **net5.0-windows10.0.17763.0**
      - **net5.0-windows10.0.18362.0**
      - **net5.0-windows10.0.19041.0**

2. Add the following C#/WinRT specific properties to the project file. The `CsWinRTComponent` property specifies that your project is a Windows Runtime component, so that a WinMD file is generated for the component. The `CsWinRTWindowsMetadata` property provides a source for Windows Metadata and is required as of the latest C#/WinRT version.

      ```xml
      <PropertyGroup>
            <CsWinRTComponent>true</CsWinRTComponent>
            <CsWinRTWindowsMetadata>10.0.19041.0</CsWinRTWindowsMetadata>
      </PropertyGroup>
      ```

3. Make sure to install the latest version of the [Microsoft.Windows.CsWinRT](https://www.nuget.org/packages/Microsoft.Windows.CsWinRT) NuGet package.

### Packaging your component

To generate a NuGet package for the component, you can choose one of the following methods:

* If you want to generate a NuGet package every time you build the project, add the following property to the project file.

    ```xml
    <PropertyGroup>
        <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    </PropertyGroup>
    ```

* Alternatively, you can generate a NuGet package by right clicking the project in **Solution Explorer** and selecting **Pack**.

To make your component available as a NuGet package, it is important to include the DLLs necessary for C#/WinRT hosting. 
When you pack your C#/WinRT component the DLLs/WinMD are automatically added to your nupkg, based on a nuspec generated from your project file.

**If you are going to write your own nuspec**, then you should make sure your nuspec contains the following ```file``` entries for your component ```MyAuthoredComponent``` (note: your TargetFramework may vary). This is so our targets that supply the DLLs for any consumers of your package work.  
Similarly, any other dependencies, e.g. `Microsoft.WinUI`, will need to be included in your nuspec as well.

``` xml
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

Your component can then be used in consuming apps by a `PackageReference`.

### Authoring an out of process component

For an example of authoring an out-of-process C#/WinRT component, see the [background task component sample](https://github.com/microsoft/CsWinRT/tree/master/src/Samples/BgTaskComponent).

## Consuming C#/WinRT Components

This section describes the steps needed to consume a C#/WinRT component from the following kinds of applications:

- [C++/WinRT desktop applications](#Consuming-from-C++/WinRT)
- [C# .NET 5+ desktop applications](#Consuming-from-C#-applications)
- [Packaged applications](#Consuming-from-packaged-applications)

### Consuming from C++/WinRT

You'll need to create a manifest file to consume a C#/WinRT component from native app named `YourNativeApp.exe.manifest`. If your app is packaged with MSIX, then you don't need to include the manifest file, otherwise you need to include your activatable class registrations in the manifest file.

You can find an example of the manifest file in this [sample](https://github.com/microsoft/CsWinRT/tree/master/src/Samples/AuthoringDemo/CppConsoleApp). For more information on managed component hosting, refer to the [hosting docs](https://github.com/microsoft/CsWinRT/blob/master/docs/hosting.md).

1. To add this manifest file in Visual Studio, right click on the project node under **Solution Explorer** and click **Add -> New Item**. Search for the **Text File** template and name your file `YourNativeApp.exe.manifest`.

2. Modify the project to include the manifest file in the output when deploying the project. Right-click on the file in **Solution Explorer**, select **Properties**, and set the **Content** property to **True** using the drop-down arrow on the right.

3. Add a reference to the C#/WinRT component either as a NuGet package reference or project reference.

      If you choose to consume your component as a **project reference** in a native app, you will also need to add a reference to the component's generated WinMD. The WinMD can be found in the output directory of the authored component's project. To add references, right-click on the native project node, and click **Add** -> **Reference**. Select the C#/WinRT component project under the **Projects** node and the generated WinMD file from the **Browse** node.

### Consuming from C# applications

Both NuGet package references and project references to C#/WinRT coponents are supported for managed apps written in C#/.NET 5.

### Consuming from packaged applications

Consuming C#/WinRT components from MSIX-packaged applications is supported for some scenarios. The latest Visual Studio Preview version is recommended for packaging scenarios.

- Consuming a C#/WinRT component from packaged C# apps is supported.
- Consuming a C#/WinRT component from packaged C++ apps works as a package reference, but not project reference.

## Known Authoring Issues

You can follow along [here](https://github.com/microsoft/CsWinRT/issues/663) as we develop authoring support.

## References

Here are some resources that demonstrate authoring C#/WinRT components and the details discussed in this document.

1. [Simple C#/WinRT component sample](https://github.com/microsoft/CsWinRT/tree/master/src/Samples/AuthoringDemo) and associated [walkthrough](https://docs.microsoft.com/en-us/windows/uwp/csharp-winrt/create-windows-runtime-component-cswinrt) on creating a C#/WinRT component and consuming it from C++/WinRT

2. [Background Task component sample](https://github.com/microsoft/CsWinRT/tree/master/src/Samples/BgTaskComponent) demonstrating consuming an out-of-process C#/WinRT component from a packaged .NET app

3. https://github.com/microsoft/CsWinRT/tree/master/src/Tests/AuthoringTest

4. https://github.com/microsoft/CsWinRT/tree/master/src/Tests/AuthoringConsumptionTest