# C#/WinRT Authoring Sample

This sample demonstrates how to author a simple C#/WinRT component, create a NuGet package for the component, and consume the component as a package reference from a C++/WinRT console app. This sample currently uses [CsWinRT version 1.1.1](https://www.nuget.org/packages/Microsoft.Windows.CsWinRT/1.1.1). Refer to the [authoring docs](https://github.com/microsoft/CsWinRT/blob/master/docs/authoring.md) for more details.

- **AuthoringDemo** is is a C# .NET 5 authored component using a **Class Library (.NET Core)** project with some project file modifications. There are two runtime classes authored in *Example.cs* and *FolderEnumeration.cs*, which demonstrate the usage of basic types and WinRT types.

- **CppConsoleApp** is a **Windows Console Application (C++/WinRT)** project that demonstrates consuming the **AuthoringDemo** component with a NuGet package reference.

Before building the solution, right click on **AuthoringDemo** solution node and select **Restore NuGet packages**.
