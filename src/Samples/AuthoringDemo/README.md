# C#/WinRT Authoring Sample

This sample demonstrates how to author a simple C#/WinRT component and consume the component as a project reference.

- **AuthoringDemo** is a C# .NET 6 class library project that uses the C#/WinRT NuGet package to generate a WinRT component. There are two runtime classes authored in *Example.cs* and *FolderEnumeration.cs*, which demonstrate the usage of basic types and WinRT types.

The following apps demonstrate how to consume the C#/WinRT component **AuthoringDemo**:

- [**CppConsoleApp**](CppConsoleApp) is a **Windows Console Application (C++/WinRT)** project that consumes the **AuthoringDemo** component with a project reference.

- [**WinUI3CppApp**](WinUI3CppApp) is a C++/WinRT desktop app using the Windows App SDK **Blank App, Packaged (WinUI 3 in Desktop)** template (see [create a WinUI3 C++ Packaged app](https://docs.microsoft.com/windows/apps/winui/winui3/create-your-first-winui3-app?pivots=winui3-packaged-cpp)).



## Prerequisites

* [.NET 8](https://dotnet.microsoft.com/download/dotnet/8.0)
* Visual Studio 2022

**Note**: This sample can be modified to target .NET 5 and Visual Studio 2019. This involves editing the `TargetFramework` properties to target `net5.0-windows10.0.19041.0`.

## Building and running the sample

1. Open **AuthoringDemo.sln** in Visual Studio 2022.

2. Ensure that the **CppConsoleApp** project is set as the startup project.

3. From Visual Studio, choose **Start Debugging** (F5). 

## Resources

- [Authoring C#/WinRT components](https://github.com/microsoft/CsWinRT/blob/master/docs/authoring.md)
