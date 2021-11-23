# C#/WinRT Projection Sample

This sample demonstrates how to do the following:

- Use the C#/WinRT package to generate a C# .NET projection interop assembly from a C++/WinRT component
- Distribute the component along with the interop assembly as a NuGet package
- Consume the component from a .NET 6 C# console application

## Requirements

* [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/) with the Universal Windows Platform development workload installed. In **Installation Details** > **Universal Windows Platform development**, check the **C++ (v14x) Universal Windows Platform tools** option.
* [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
* nuget.exe 5.8.0-preview.2 or later (for command line MSBuild)

**Note**: This sample uses .NET 6 and therefore requires Visual Studio 2022 to build and run. If you prefer, you can use Visual Studio 2019 and modify the sample to target [.NET 5](https://dotnet.microsoft.com/download/dotnet/5.0). To do this, you will need to modify the `TargetFramework` and the *nuspec* file in the `SimpleMathProjection` project to target `net5.0-windows10.0.19041.0`.

## Build and run the sample

For building in Visual Studio:

1. Open *CppWinRTComponentProjectionSample.sln* in Visual Studio. Ensure that *SimpleMathProjection* is set as the startup project, and set the Platform and Configuration to x64 and Release. Right click on the solution and build. This will do the following:
    - Build *SimpleMathComponent*: this will generate *SimpleMathComponent.winmd* and *SimpleMathComponent.dll* 
    - Generate the projection interop assembly for the component using C#/WinRT, *SimpleMathProjection.dll*
    - Generate a NuGet package for the component. To ensure the solution has built successfully, navigate to the *SimpleMathProjection/nuget* folder in your file explorer. You should see the generated NuGet package (*SimpleMathComponent.0.1.0-prerelease.nupkg*). which can be referenced by C# .NET app consumers.

2. Open *ConsoleAppSample.sln* in Visual Studio. Build and run the solution which references and restores the SimpleMathComponent NuGet package to consume the projection.

    - If you run into errors restoring NuGet packages, look at the docs on [NuGet restore options](https://docs.microsoft.com/nuget/consume-packages/package-restore). You may need to  configure your NuGet package manager settings to allow for package restores on build. You may also need to run `nuget.exe restore ConsoleAppSample.sln` from a command prompt.

For building with the command line, execute the following:

```cmd
nuget restore CppWinRTComponentProjectionSample.sln
msbuild /p:platform=x64;configuration=release CppWinRTComponentProjectionSample.sln
nuget restore ConsoleAppSample.sln
msbuild /p:platform=x64;configuration=release ConsoleAppSample.sln
```

## Resources

- [Walkthrough documentation](https://docs.microsoft.com/windows/uwp/csharp-winrt/net-projection-from-cppwinrt-component) for this sample
- [C#/WinRT NuGet properties](../../../nuget/README.md)