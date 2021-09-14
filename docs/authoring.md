# Authoring Components

## Overview


**Note**: C#/WinRT authoring and hosting project reference support is available with [.NET 6 RC1](https://dotnet.microsoft.com/download/dotnet/6.0) and later. This means the following scenario is **not supported in .NET 5**: C# app with a project reference to a C#/WinRT Component.

C#/WinRT provides support for authoring Windows Runtime components. You can write a library in C#, and specify that it is a `CsWinRTComponent` for C#/WinRT to produce a WinMD that any WinRT compatible language can use. For example, a library written in C# can be used by a C++ program, via C#/WinRT and C++/WinRT.
Managed apps only need a project or package reference to the authored component, and native apps will need some extra steps that we cover in this documentation.

## Authoring a C#/WinRT Component

To author your component, first create a project using the C# **Class Library (.NET Core)** template in Visual Studio. You will need to make the following modifications to the project.

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
      - **net5.0-windows10.0.20348.0**
      - **net6.0-windows10.0.17763.0**
      - **net6.0-windows10.0.18362.0**
      - **net6.0-windows10.0.19041.0**
      - **net6.0-windows10.0.20348.0**

2. Install the latest version of the [Microsoft.Windows.CsWinRT](https://www.nuget.org/packages/Microsoft.Windows.CsWinRT) NuGet package.

3. Add the following C#/WinRT specific properties to the project file. The `CsWinRTComponent` property specifies that your project is a Windows Runtime component, so that a WinMD file is generated for the component. 

      ```xml
      <PropertyGroup>
            <CsWinRTComponent>true</CsWinRTComponent>
      </PropertyGroup>
      ```

4. Implement your runtime classes in the class files in your library project, following the necessary [WinRT guidelines and type restrictions](https://docs.microsoft.com/windows/uwp/winrt-components/creating-windows-runtime-components-in-csharp-and-visual-basic#declaring-types-in-windows-runtime-components).


### Packaging your component

To generate a NuGet package for the component, you can choose one of the following methods:

- If you want to generate a NuGet package every time you build the project, add the following property to the project file.

    ```xml
    <PropertyGroup>
        <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    </PropertyGroup>
    ```

- Alternatively, you can generate a NuGet package by right clicking the project in **Solution Explorer** and selecting **Pack**.

To make your component available as a NuGet package, it is important to include the DLLs necessary for C#/WinRT hosting. When you pack your C#/WinRT component the DLLs/WinMD are automatically added to your nupkg, based on a nuspec generated from your project file.

**If you are going to write your own nuspec**, then you should make sure your nuspec contains the following ```file``` entries for your component ```MyAuthoredComponent``` (note: your TargetFramework may vary). This is so our targets that supply the DLLs for any consumers of your package work.  
Similarly, any other dependencies, e.g. `Microsoft.WinUI`, will need to be included in your nuspec as well.

``` xml
<files>
  <file src="$(TargetDir)MyAuthoredComponent.dll"        target="lib\$(TargetFramework)\MyAuthoredComponent.dll" />
  <file src="$(TargetDir)MyAuthoredComponent.winmd"      target="lib\$(TargetFramework)\winmd\MyAuthoredComponent.winmd" />
  
  <file src="$(TargetDir)Microsoft.Windows.SDK.NET.dll"  target="lib\$(TargetFramework)\Microsoft.Windows.SDK.NET.dll" />
   
  <!-- Note: you must rename the CsWinRT.Authoring.Targets as follows -->
  <file src="C:\Path\To\CsWinRT\NugetDir\build\Microsoft.Windows.CsWinRT.Authoring.targets"   
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

## Consuming C#/WinRT Components

This section describes the steps needed to consume a C#/WinRT component from the following kinds of applications:

- [C++/WinRT desktop applications](#Consuming-from-C++/WinRT)
- [C# .NET 5+ desktop applications](#Consuming-from-C#-applications)
- [Out of process components](#Consuming-an-out-of-process-component)
- [Packaged applications](#Consuming-from-packaged-applications)

### Consuming from C++/WinRT

Consuming a C#/WinRT component from a C++/WinRT desktop application is supported by both package reference or project reference.

- For package references, simply right-click on the native project node and click **Manage NuGet packages** to find and install the component package.

- For project references, if your authored component is built with .NET 5, you also need a WinMD reference along with the typical project reference. The WinMD can be found in the output directory of the authored component's project. To add both the project and WinMD references, right-click on the native project node, and click **Add** -> **Reference**. Select the C#/WinRT component project under the **Projects** node and the generated WinMD file from the **Browse** node.

For native consumption of C#/WinRT components, you also need to create a manifest file named `YourNativeApp.exe.manifest`. If your app is packaged with MSIX, then you don't need to include the manifest file. In the case that you do make a manifest, you need to add activatable class registrations for the public types in your component. We provide an [authoring sample](https://github.com/microsoft/CsWinRT/tree/master/src/Samples/AuthoringDemo/CppConsoleApp) with an example manifest file. To create the manifest file:

1. In Visual Studio, right click on the project node under **Solution Explorer** and click **Add -> New Item**. Search for the **Text File** template and name your file `YourNativeApp.exe.manifest`.

2. Add your activatable class registrations to the manifest file. You can use the manifest file from [this sample](https://github.com/microsoft/CsWinRT/tree/master/src/Samples/AuthoringDemo/CppConsoleApp) as an example.

3. Modify the project to include the manifest file in the output when deploying the project. Right-click on the file in **Solution Explorer**, select **Properties**, and set the **Content** property to **True** using the drop-down arrow on the right.

### Consuming from C# applications

**Note** Starting with C#/WinRT 1.3.5, this requires .NET 6.

Consuming a C#/WinRT component from C#/.NET apps is supported by both package reference or project reference. This scenario is equivalent to consuming any ordinary C# class library and does not involve WinRT activation in most cases.

### Consuming an out of process component

C#/WinRT supports authoring out-of-process components that can be consumed by other languages. Currently, consuming an out-of-process component is supported for managed C# apps with the use of a packaging project. A manual WinRT.Host.runtimeconfig.json file is currently required for this scenario, which is demonstrated in the sample below. Native consumption of out-of-process components is not fully supported yet.

For an example of authoring and consuming an out-of-process C#/WinRT component, see the [background task component sample](https://github.com/microsoft/CsWinRT/tree/master/src/Samples/BgTaskComponent).

### Consuming from packaged applications

Consuming C#/WinRT components from MSIX-packaged applications is supported for some scenarios. The latest Visual Studio Preview version is recommended for packaging scenarios.

- Consuming a C#/WinRT component from packaged C# apps is supported.
- Consuming a C#/WinRT component from packaged C++ apps is supported as a package reference, and requires the `exe.manifest` file.

## Known Authoring Issues

You can follow along [here](https://github.com/microsoft/CsWinRT/issues/663) as we develop authoring support.

Authoring issues are tagged under the *authoring* label under this repo. Feel free to [file an issue](https://github.com/microsoft/CsWinRT/issues/new/choose) tagged with the *authoring* label if you encounter any new issues!

## Resources

Here are some resources that demonstrate authoring C#/WinRT components and the details discussed in this document.

1. [Simple C#/WinRT component sample](https://github.com/microsoft/CsWinRT/tree/master/src/Samples/AuthoringDemo) and associated [walkthrough](https://docs.microsoft.com/en-us/windows/uwp/csharp-winrt/create-windows-runtime-component-cswinrt) on creating a C#/WinRT component and consuming it from C++/WinRT

2. [Background Task component sample](https://github.com/microsoft/CsWinRT/tree/master/src/Samples/BgTaskComponent) demonstrating consuming an out-of-process C#/WinRT component from a packaged .NET 5 app

3. Testing projects used for authoring:
      - [Test Component](https://github.com/microsoft/CsWinRT/tree/master/src/Tests/AuthoringTest)
      - [Test Application](https://github.com/microsoft/CsWinRT/tree/master/src/Tests/AuthoringConsumptionTest)

4. [Managed component hosting](https://github.com/microsoft/CsWinRT/blob/master/docs/hosting.md) 

5. [Diagnose component errors](https://docs.microsoft.com/en-us/windows/uwp/csharp-winrt/authoring-diagnostics)
