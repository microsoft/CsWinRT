# C#/WinRT End to End Sample

This sample demonstrates how to invoke C#/WinRT to build a .NET5 projection for a C++/WinRT Runtime Component, create a NuGet package for the component, and reference the NuGet package from a .NET5 console app.

**Note**: This sample is accurate as of RC2, and there are expected tooling improvements to be made for the developer experience in our future GA release.

## Requirements

- Visual Studio 16.8 Preview 3
- .NET 5 SDK RC2
- Nuget.exe 5.8.0-preview.2 (for command line MSBuild)

## Build and run the sample

Using Visual Studio:

1. Build the *CppWinRTComponentProjectionSample* solution first. This generates the projection and interop assembly using cswinrt, and creates *SimpleMathComponent.nupkg* which can be referenced in consuming apps. If you run into errors restoring NuGet packages, look at the docs on [NuGet restore options](https://docs.microsoft.com/nuget/consume-packages/package-restore). You may need to configure your NuGet package manager settings to allow for package restores on build.

2. Build the *ConsoleAppSample* solution which references and restores  *SimpleMathComponent.nupkg* to consume the projection.

For building with the command line, execute the following:

```cmd
nuget restore CppWinRTComponentProjectionSample.sln
msbuild /p:platform=x64;configuration=debug CppWinRTComponentProjectionSample.sln
nuget restore ConsoleAppSample.sln
msbuild /p:platform=x64;configuration=debug ConsoleAppSample.sln
```
