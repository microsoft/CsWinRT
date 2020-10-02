# C#/WinRT End to End Sample

This sample demonstrates how to invoke C#/WinRT to build a .NET5 projection for a C++/WinRT Runtime Component, create a NuGet package for the component, and reference the NuGet package from a .NET5 console app.

Requirements:

- Visual Studio 16.8 Preview 3
- .NET 5 SDK RC2

To run this sample, you will need to do the following:

1. Build the *CppWinRTProjectionSample* solution first. This generates the projection and interop assembly using cswinrt, and creates *SimpleMathComponent.nupkg* which can be referenced in consuming apps. If you run into errors restoring NuGet packages, go to **Tools** -> **NuGet Package Manager** -> **Package Manager Settings** and select *"Automatically check for missing packages during build in Visual Studio”*. Alternatively, you can run `dotnet restore` using the command line.

2. Build the *ConsoleAppSample* solution which references and restores  *SimpleMathComponent.nupkg* to consume the projection.

**Note**: This sample is accurate as of RC2, and there are expected tooling improvements to be made for the developer experience in our future GA release.