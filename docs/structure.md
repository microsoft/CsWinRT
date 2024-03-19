# Repository Structure

This document describes the CsWinRT repository organization. Documentation and specs are located in the [`/docs`](.) folder. All source code for CsWinRT is located in the [`/src`](../src) folder, and files for generating the NuGet package are located in [`/nuget`](../nuget).

## [`build`](../build)

Contains source files for Azure DevOps pipeline that handles official builds and testing for C#/WinRT. Uses Maestro to publish builds conveniently for dependent projects; Maestro is a dependency manager 
developed by dotnet as part of the [Arcade Build System](https://github.com/dotnet/arcade).

## [`eng`](../eng)

Contains files that assist with publishing to Maestro.

## [`nuget`](../nuget)

Contains source files for producing a C#/WinRT NuGet package, which is regularly built, signed, and published to nuget.org by Microsoft.  The C#/WinRT NuGet package contains the **cswinrt.exe** compiler and the runtime assembly, `WinRT.Runtime.dll`.

## [`src/Authoring`](../src/Authoring)

Contains projects for implementing authoring and hosting support.

## [`src/Benchmarks`](../src/Benchmarks)

Contains benchmarks written using BenchmarkDotNet to track the performance of scenarios in the generated projection.  To run the benchmarks using the CsWinRT projection, run `benchmark.cmd`.  To run the same benchmarks using the built-in WinMD support in NET Core 3.1 to compare against as a baseline, run `benchmark_winmd.cmd`.

## [`src/cswinrt`](../src/cswinrt) 

Contains the sources and `cswinrt.vcxproj` project file for building the C#/WinRT compiler, **cswinrt.exe**.

The compiler uses the [WinMD NuGet package](http://aka.ms/winmd/nuget) for parsing [ECMA-335 metadata](http://www.ecma-international.org/publications/standards/Ecma-335.htm) files.  The WinMD github repo includes a [winmd.natvis](https://github.com/microsoft/winmd/blob/master/vs/winmd.natvis) script for debugging metadata parsing.  A symlink can be used to install the script:
  > for /f "tokens=2*" %i in ('reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders" /v Personal ^| findstr Personal') do @for /f "tokens=2" %k in ('"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere" -latest ^| findstr catalog_productLineVersion') do @echo %j\Visual Studio %k\Visualizers| for /f "delims=" %l in ('more') do @md "%l" 2>nul & mklink "%l\winmd.natvis" "c:\git\winmd\vs\winmd.natvis" 
  
The C#/WinRT project also contains a cswinrt.natvis script for debugging the C# projection writing, which can also be installed with a symlink:
> for /f "tokens=2*" %i in ('reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders" /v Personal ^| findstr Personal') do @for /f "tokens=2" %k in ('"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere" -latest ^| findstr catalog_productLineVersion') do @echo %j\Visual Studio %k\Visualizers| for /f "delims=" %l in ('more') do @md "%l" 2>nul & mklink "%l\cswinrt.natvis" "c:\git\cswinrt\cswinrt\cswinrt.natvis"

See also [Deploying .natvis files](https://docs.microsoft.com/en-us/visualstudio/debugger/create-custom-views-of-native-objects?view=vs-2015#BKMK_natvis_location).

## [`src/Projections`](../src/Projections) 

Contains several projects for generating and building projections from the Windows SDK, WinUI, Benchmark (produced by the BenchmarkComponent project), and Test metadata (produced by the TestWinRT and TestComponentCSharp projects).

## [`src/Samples`](../src/Samples) 

- [`Net5ProjectionSample`](../src/Samples/Net5ProjectionSample): Contains an end-to-end sample for component authors, showing how to generate a projection from a C++/WinRT component and consume it using a NuGet package.

- [`WinUIDesktopSample`](../src/Samples/WinUIDesktopSample): Contains an end-to-end sample app that uses the Windows SDK and WinUI projections generated above.

## [`src/Tests`](../src/Tests)

Contains various testing-related projects:

- [`TestComponentCSharp`](../src/Tests/TestComponentCSharp): This is an implementation of a WinRT test component, defined in `class.idl` and used by the UnitTest project.  To complement the general TestComponent above, the TestComponentCSharp  tests scenarios specific to the C#/WinRT language projection.

- [`UnitTest`](../src/Tests/UnitTest): Unit tests for validating the Windows SDK, WinUI, and Test projections generated above.  All pull requests should ensure that this project executes without errors.

- [`HostTest`](../src/Tests/HostTest): Unit tests for WinRT.Host.dll, which provides hosting for runtime components written in C#.

## [`src/TestWinRT`](https://github.com/microsoft/TestWinRT/)

C#/WinRT makes use of the standalone [TestWinRT](https://github.com/microsoft/TestWinRT/) repository for general language projection test coverage.  This repo is cloned into the root of the C#/WinRT repo, via `get_testwinrt.cmd`, so that `cswinrt.sln` can resolve its reference to `TestComponent.vcxproj`.  The resulting `TestComponent` and `BenchmarkComponent` files are consumed by the UnitTest and Benchmarks projects above.

## [`src/WinRT.Runtime`](../src/WinRT.Runtime) 

Contains the WinRT.Runtime project for building the C#/WinRT runtime assembly, `WinRT.Runtime.dll`. The runtime assembly provides an abstraction layer over the .NET 5 runtime, and also provides Xaml reference tracking support which is necessary for WinUI 3 applications to manage memory correctly. The runtime assembly implements the following features for all projected C#/WinRT types:

- WinRT activation and marshaling logic
- Custom type mappings, primarily for WinUI
- [COM Wrapper](https://docs.microsoft.com/dotnet/api/system.runtime.interopservices.comwrappers?view=net-5.0) management
- IDynamicInterfaceCastable and ComImport casting support
- Extension methods common to projected types

