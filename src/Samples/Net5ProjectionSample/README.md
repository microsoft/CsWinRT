# C#/WinRT Projection Sample

This sample demonstrates how to do the following:

- Use the C#/WinRT package to generate a C# projection interop assembly from a C++/WinRT component
- Distribute the component along with the interop assembly as a NuGet package
- Consume the component from a .NET 5+ console application

## Requirements

- Visual Studio 16.8
- .NET 5.0 SDK
- nuget.exe 5.8.0-preview.2 (for command line MSBuild)

## Build and run the sample

For building in Visual Studio 2019:

1. Build the *CppWinRTComponentProjectionSample* solution first. This builds the WinMD for SimpleMathComponent, generates a projection interop assembly for the component using C#/WinRT, and generates the SimpleMathComponent NuGet package for the component which .NET 5+ apps can reference. 

2. Build and run the *ConsoleAppSample* solution which references and restores the SimpleMathComponent NuGet package to consume the projection.

    If you run into errors restoring NuGet packages, look at the docs on [NuGet restore options](https://docs.microsoft.com/nuget/consume-packages/package-restore). You may need to  configure your NuGet package manager settings to allow for package restores on build.

For building with the command line, execute the following:

```cmd
nuget restore CppWinRTComponentProjectionSample.sln
msbuild /p:platform=x64;configuration=debug CppWinRTComponentProjectionSample.sln
nuget restore ConsoleAppSample.sln
msbuild /p:platform=x64;configuration=debug ConsoleAppSample.sln
```
